using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;
using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.Services.CimInspector
{
    /// <summary>
    /// Estimates background colors for labels and symbols by inspecting basemap layers
    /// and the map's reference background. Must be called on the MCT via <c>QueuedTask.Run()</c>.
    /// </summary>
    public sealed class BackgroundColorEstimator
    {
        /// <summary>
        /// Estimates the default background color for a map by examining the map's
        /// background color property and basemap type.
        /// Must be called on the MCT.
        /// </summary>
        /// <param name="map">The map to inspect.</param>
        /// <returns>The estimated background color, defaulting to white if undetermined.</returns>
        public ColorInfo EstimateMapBackground(Map map)
        {
            if (map is null) return new ColorInfo(255, 255, 255);

            var cimMap = map.GetDefinition();
            if (cimMap is null) return new ColorInfo(255, 255, 255);

            // Check the map's background color
            if (cimMap.BackgroundColor is not null)
            {
                var bg = CimWalker.ExtractColor(cimMap.BackgroundColor);
                if (bg is not null) return bg;
            }

            // Examine basemap layers to infer background
            return InferFromBasemap(map) ?? new ColorInfo(255, 255, 255);
        }

        /// <summary>
        /// Determines whether the map has a heterogeneous background (e.g., imagery basemap)
        /// that makes per-label contrast checking unreliable. When true, label contrast
        /// findings should be flagged for manual review rather than auto-pass/fail.
        /// Must be called on the MCT.
        /// </summary>
        /// <param name="map">The map to inspect.</param>
        /// <returns><c>true</c> if the background is imagery or otherwise variable.</returns>
        public bool IsHeterogeneousBackground(Map map)
        {
            if (map is null) return false;

            var layers = map.GetLayersAsFlattenedList();
            foreach (var layer in layers)
            {
                // Raster/imagery basemaps have variable backgrounds
                if (layer is BasicRasterLayer || layer is ImageServiceLayer)
                    return true;

                // Check layer name for common imagery basemap patterns
                string name = (layer.Name ?? string.Empty).ToUpperInvariant();
                if (name.Contains("IMAGERY") || name.Contains("SATELLITE") ||
                    name.Contains("AERIAL") || name.Contains("WORLD_IMAGERY") ||
                    name.Contains("ORTHO"))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Collects all unique colors used across the map's symbology and labels.
        /// Must be called on the MCT.
        /// </summary>
        /// <param name="map">The map to inspect.</param>
        /// <returns>A deduplicated list of all colors in use.</returns>
        public IReadOnlyList<ColorInfo> CollectMapPalette(Map map)
        {
            if (map is null) return Array.Empty<ColorInfo>();

            var colors = new HashSet<ColorInfo>();
            var layers = map.GetLayersAsFlattenedList();

            foreach (var layer in layers)
            {
                if (layer is FeatureLayer featureLayer)
                {
                    CollectLayerColors(featureLayer, colors);
                }
            }

            return colors.ToList();
        }

        /// <summary>
        /// Collects all colors used across the map's symbology and labels,
        /// preserving their layer name, category label, and color role.
        /// Must be called on the MCT.
        /// </summary>
        /// <param name="map">The map to inspect.</param>
        /// <returns>A list of labeled colors with full semantic context.</returns>
        public IReadOnlyList<LabeledColor> CollectLabeledPalette(Map map)
        {
            if (map is null) return Array.Empty<LabeledColor>();

            var result = new List<LabeledColor>();
            var seen = new HashSet<string>(); // deduplicate by hex+layer+label+role
            var layers = map.GetLayersAsFlattenedList();

            foreach (var layer in layers)
            {
                if (layer is FeatureLayer featureLayer)
                {
                    CollectLabeledLayerColors(featureLayer, result, seen);
                }
            }

            return result;
        }

        private static void CollectLayerColors(FeatureLayer featureLayer, HashSet<ColorInfo> colors)
        {
            CIMFeatureLayer? cimFL;
            try
            {
                cimFL = featureLayer.GetDefinition() as CIMFeatureLayer;
            }
            catch
            {
                return;
            }

            if (cimFL is null) return;

            // Collect renderer colors
            if (cimFL.Renderer is not null)
            {
                CollectRendererColors(cimFL.Renderer, colors);
            }

            // Collect label colors
            if (cimFL.LabelClasses is not null)
            {
                foreach (var lc in cimFL.LabelClasses)
                {
                    var textSymbol = lc?.TextSymbol?.Symbol as CIMTextSymbol;
                    if (textSymbol?.Symbol?.SymbolLayers is not null)
                    {
                        foreach (var sl in textSymbol.Symbol.SymbolLayers)
                        {
                            if (sl is CIMSolidFill fill)
                            {
                                var c = CimWalker.ExtractColor(fill.Color);
                                if (c is not null) colors.Add(c);
                            }
                        }
                    }
                }
            }
        }

        private static void CollectLabeledLayerColors(
            FeatureLayer featureLayer, List<LabeledColor> result, HashSet<string> seen)
        {
            CIMFeatureLayer? cimFL;
            try
            {
                cimFL = featureLayer.GetDefinition() as CIMFeatureLayer;
            }
            catch
            {
                return;
            }

            if (cimFL is null) return;
            string layerName = featureLayer.Name ?? "Unnamed Layer";

            // Renderer colors
            if (cimFL.Renderer is not null)
            {
                CollectLabeledRendererColors(cimFL.Renderer, layerName, result, seen);
            }

            // Label colors
            if (cimFL.LabelClasses is not null)
            {
                foreach (var lc in cimFL.LabelClasses)
                {
                    if (lc is null) continue;
                    var textSymbol = lc.TextSymbol?.Symbol as CIMTextSymbol;
                    if (textSymbol?.Symbol?.SymbolLayers is not null)
                    {
                        foreach (var sl in textSymbol.Symbol.SymbolLayers)
                        {
                            if (sl is CIMSolidFill fill)
                            {
                                var c = CimWalker.ExtractColor(fill.Color);
                                if (c is not null)
                                {
                                    AddLabeled(result, seen, c, layerName, lc.Name ?? "", "Label");
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void CollectRendererColors(CIMRenderer renderer, HashSet<ColorInfo> colors)
        {
            switch (renderer)
            {
                case CIMSimpleRenderer simple:
                    CollectSymbolColors(simple.Symbol?.Symbol, colors);
                    break;

                case CIMUniqueValueRenderer uvr:
                    if (uvr.Groups is not null)
                    {
                        foreach (var group in uvr.Groups)
                        {
                            if (group.Classes is null) continue;
                            foreach (var cls in group.Classes)
                            {
                                CollectSymbolColors(cls.Symbol?.Symbol, colors);
                            }
                        }
                    }
                    break;

                case CIMClassBreaksRenderer cbr:
                    if (cbr.Breaks is not null)
                    {
                        foreach (var brk in cbr.Breaks)
                        {
                            CollectSymbolColors(brk.Symbol?.Symbol, colors);
                        }
                    }
                    break;
            }
        }

        private static void CollectLabeledRendererColors(
            CIMRenderer renderer, string layerName, List<LabeledColor> result, HashSet<string> seen)
        {
            switch (renderer)
            {
                case CIMSimpleRenderer simple:
                    CollectLabeledSymbolColors(simple.Symbol?.Symbol, layerName, "", result, seen);
                    break;

                case CIMUniqueValueRenderer uvr:
                    if (uvr.Groups is not null)
                    {
                        foreach (var group in uvr.Groups)
                        {
                            if (group.Classes is null) continue;
                            foreach (var cls in group.Classes)
                            {
                                string label = cls.Label ?? cls.Values?.FirstOrDefault()?.FieldValues?.FirstOrDefault() ?? "";
                                CollectLabeledSymbolColors(cls.Symbol?.Symbol, layerName, label, result, seen);
                            }
                        }
                    }
                    break;

                case CIMClassBreaksRenderer cbr:
                    if (cbr.Breaks is not null)
                    {
                        foreach (var brk in cbr.Breaks)
                        {
                            CollectLabeledSymbolColors(brk.Symbol?.Symbol, layerName, brk.Label ?? "", result, seen);
                        }
                    }
                    break;
            }
        }

        private static void CollectSymbolColors(CIMSymbol? symbol, HashSet<ColorInfo> colors)
        {
            if (symbol is not CIMMultiLayerSymbol multiLayer) return;
            if (multiLayer.SymbolLayers is null) return;

            foreach (var layer in multiLayer.SymbolLayers)
            {
                switch (layer)
                {
                    case CIMSolidFill fill:
                        var fc = CimWalker.ExtractColor(fill.Color);
                        if (fc is not null) colors.Add(fc);
                        break;

                    case CIMSolidStroke stroke:
                        var sc = CimWalker.ExtractColor(stroke.Color);
                        if (sc is not null) colors.Add(sc);
                        break;

                    case CIMVectorMarker vm:
                        if (vm.MarkerGraphics is not null)
                        {
                            foreach (var mg in vm.MarkerGraphics)
                            {
                                CollectSymbolColors(mg.Symbol as CIMSymbol, colors);
                            }
                        }
                        break;
                }
            }
        }

        private static void CollectLabeledSymbolColors(
            CIMSymbol? symbol, string layerName, string categoryLabel,
            List<LabeledColor> result, HashSet<string> seen)
        {
            if (symbol is not CIMMultiLayerSymbol multiLayer) return;
            if (multiLayer.SymbolLayers is null) return;

            foreach (var layer in multiLayer.SymbolLayers)
            {
                switch (layer)
                {
                    case CIMSolidFill fill:
                        var fc = CimWalker.ExtractColor(fill.Color);
                        if (fc is not null)
                            AddLabeled(result, seen, fc, layerName, categoryLabel, "Fill");
                        break;

                    case CIMSolidStroke stroke:
                        var sc = CimWalker.ExtractColor(stroke.Color);
                        if (sc is not null)
                            AddLabeled(result, seen, sc, layerName, categoryLabel, "Stroke");
                        break;
                }
            }
        }

        private static void AddLabeled(
            List<LabeledColor> result, HashSet<string> seen,
            ColorInfo color, string layerName, string categoryLabel, string role)
        {
            string key = $"{color.Hex}|{layerName}|{categoryLabel}|{role}";
            if (seen.Add(key))
            {
                result.Add(new LabeledColor(color, layerName, categoryLabel, role));
            }
        }

        private static ColorInfo? InferFromBasemap(Map map)
        {
            var layers = map.GetLayersAsFlattenedList();
            if (layers is null || !layers.Any()) return null;

            // For vector tile basemaps, attempt to read the background fill color
            foreach (var layer in layers)
            {
                if (layer is FeatureLayer fl)
                {
                    try
                    {
                        var cimDef = fl.GetDefinition() as CIMFeatureLayer;
                        if (cimDef?.Renderer is CIMSimpleRenderer sr &&
                            sr.Symbol?.Symbol is CIMMultiLayerSymbol mls)
                        {
                            var fill = mls.SymbolLayers?
                                .OfType<CIMSolidFill>()
                                .FirstOrDefault();
                            if (fill is not null)
                            {
                                return CimWalker.ExtractColor(fill.Color);
                            }
                        }
                    }
                    catch
                    {
                        // Skip layers that can't be read
                    }
                }
            }

            return null;
        }
    }
}
