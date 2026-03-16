using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.Services.CimInspector
{
    /// <summary>
    /// Analyzes CIM renderers to extract symbol color, size, and shape information.
    /// Must be called on the MCT via <c>QueuedTask.Run()</c>.
    /// </summary>
    public sealed class SymbologyAnalyzer
    {
        /// <summary>
        /// Analyzes a CIM renderer and returns structured renderer information.
        /// </summary>
        /// <param name="renderer">The CIM renderer to analyze.</param>
        /// <param name="layerName">The name of the owning layer.</param>
        /// <returns>A <see cref="RendererInfo"/> or <c>null</c> if the renderer cannot be analyzed.</returns>
        public RendererInfo? AnalyzeRenderer(CIMRenderer? renderer, string layerName)
        {
            if (renderer is null) return null;

            var info = new RendererInfo
            {
                LayerName = layerName,
                RendererType = renderer.GetType().Name
            };

            switch (renderer)
            {
                case CIMSimpleRenderer simple:
                    AnalyzeSimpleRenderer(simple, info);
                    break;
                case CIMUniqueValueRenderer uvr:
                    AnalyzeUniqueValueRenderer(uvr, info);
                    break;
                case CIMClassBreaksRenderer cbr:
                    AnalyzeClassBreaksRenderer(cbr, info);
                    break;
            }

            return info;
        }

        private void AnalyzeSimpleRenderer(CIMSimpleRenderer renderer, RendererInfo info)
        {
            if (renderer.Symbol?.Symbol is not null)
            {
                var symbolClass = ExtractSymbolClass(renderer.Symbol.Symbol, renderer.Label ?? "Default");
                info.SymbolClasses.Add(symbolClass);
            }
            info.UsesShapeOrSizeVariation = false;
        }

        private void AnalyzeUniqueValueRenderer(CIMUniqueValueRenderer renderer, RendererInfo info)
        {
            bool hasShapeVariation = false;
            string? firstShape = null;
            double? firstSize = null;

            if (renderer.Groups is null) return;

            foreach (var group in renderer.Groups)
            {
                if (group.Classes is null) continue;

                foreach (var cls in group.Classes)
                {
                    if (cls.Symbol?.Symbol is null) continue;

                    var symbolClass = ExtractSymbolClass(cls.Symbol.Symbol, cls.Label ?? "Unknown");
                    info.SymbolClasses.Add(symbolClass);

                    // Track shape/size variation
                    if (firstShape is null)
                    {
                        firstShape = symbolClass.ShapeName;
                        firstSize = symbolClass.SymbolSize;
                    }
                    else
                    {
                        if (symbolClass.ShapeName != firstShape || Math.Abs(symbolClass.SymbolSize - (firstSize ?? 0)) > 0.1)
                        {
                            hasShapeVariation = true;
                        }
                    }
                }
            }

            info.UsesShapeOrSizeVariation = hasShapeVariation;
        }

        private void AnalyzeClassBreaksRenderer(CIMClassBreaksRenderer renderer, RendererInfo info)
        {
            bool hasSizeVariation = false;
            double? firstSize = null;

            if (renderer.Breaks is null) return;

            foreach (var brk in renderer.Breaks)
            {
                if (brk.Symbol?.Symbol is null) continue;

                var symbolClass = ExtractSymbolClass(brk.Symbol.Symbol, brk.Label ?? "Unknown");
                info.SymbolClasses.Add(symbolClass);

                if (firstSize is null)
                {
                    firstSize = symbolClass.SymbolSize;
                }
                else if (Math.Abs(symbolClass.SymbolSize - firstSize.Value) > 0.1)
                {
                    hasSizeVariation = true;
                }
            }

            info.UsesShapeOrSizeVariation = hasSizeVariation;
        }

        private SymbolClassInfo ExtractSymbolClass(CIMSymbol symbol, string label)
        {
            var symbolClass = new SymbolClassInfo { Label = label };

            if (symbol is CIMPointSymbol pointSymbol)
            {
                ExtractSymbolLayers(pointSymbol.SymbolLayers, symbolClass);

                // Extract marker size from marker layers
                var marker = pointSymbol.SymbolLayers?
                    .OfType<CIMMarker>()
                    .FirstOrDefault();
                if (marker is not null)
                {
                    symbolClass.SymbolSize = marker.Size;
                }

                // Check for marker shape name
                var characterMarker = pointSymbol.SymbolLayers?
                    .OfType<CIMCharacterMarker>()
                    .FirstOrDefault();
                if (characterMarker is not null)
                {
                    symbolClass.ShapeName = characterMarker.FontFamilyName;
                }

                var vectorMarker = pointSymbol.SymbolLayers?
                    .OfType<CIMVectorMarker>()
                    .FirstOrDefault();
                if (vectorMarker is not null)
                {
                    symbolClass.ShapeName = "VectorMarker";
                }
            }
            else if (symbol is CIMLineSymbol lineSymbol)
            {
                ExtractSymbolLayers(lineSymbol.SymbolLayers, symbolClass);
            }
            else if (symbol is CIMPolygonSymbol polygonSymbol)
            {
                ExtractSymbolLayers(polygonSymbol.SymbolLayers, symbolClass);
            }

            return symbolClass;
        }

        private void ExtractSymbolLayers(CIMSymbolLayer[]? layers, SymbolClassInfo symbolClass)
        {
            if (layers is null) return;

            foreach (var layer in layers)
            {
                switch (layer)
                {
                    case CIMSolidFill solidFill:
                        symbolClass.FillColor ??= CimWalker.ExtractColor(solidFill.Color);
                        break;
                    case CIMSolidStroke solidStroke:
                        symbolClass.StrokeColor ??= CimWalker.ExtractColor(solidStroke.Color);
                        symbolClass.StrokeWidth = solidStroke.Width;
                        break;
                }
            }
        }
    }
}
