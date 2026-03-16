using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.Services.CimInspector
{
    /// <summary>
    /// Traverses the CIM object graph of a map and extracts symbology and label data.
    /// All methods in this class MUST be called inside <c>QueuedTask.Run()</c>.
    /// </summary>
    public sealed class CimWalker
    {
        private readonly SymbologyAnalyzer _symbologyAnalyzer = new();
        private readonly LabelAnalyzer _labelAnalyzer = new();

        /// <summary>
        /// Walks the CIM for the specified map and populates the audit context.
        /// Must be called on the MCT via <c>QueuedTask.Run()</c>.
        /// </summary>
        /// <param name="map">The map to inspect.</param>
        /// <param name="context">The audit context to populate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="map"/> or <paramref name="context"/> is <c>null</c>.</exception>
        public void WalkMap(Map map, AuditContext context)
        {
            if (map is null) throw new ArgumentNullException(nameof(map));
            if (context is null) throw new ArgumentNullException(nameof(context));

            var cimMap = map.GetDefinition();
            if (cimMap is null) return;

            context.MapTitle = cimMap.Name;
            context.MapDescription = cimMap.Description;

            var layers = map.GetLayersAsFlattenedList();
            foreach (var layer in layers)
            {
                if (layer is FeatureLayer featureLayer)
                {
                    WalkFeatureLayer(featureLayer, context);
                }
            }
        }

        private void WalkFeatureLayer(FeatureLayer featureLayer, AuditContext context)
        {
            CIMFeatureLayer? cimFL = null;
            try
            {
                cimFL = featureLayer.GetDefinition() as CIMFeatureLayer;
            }
            catch
            {
                // Layer may be in an invalid state; skip it
                return;
            }

            if (cimFL is null) return;

            string layerName = featureLayer.Name ?? "Unknown Layer";

            // Extract renderer info
            if (cimFL.Renderer is not null)
            {
                var rendererInfo = _symbologyAnalyzer.AnalyzeRenderer(cimFL.Renderer, layerName);
                if (rendererInfo is not null)
                {
                    context.Renderers.Add(rendererInfo);
                }
            }

            // Extract label class info
            if (cimFL.LabelClasses is not null)
            {
                foreach (var labelClass in cimFL.LabelClasses)
                {
                    var labelInfo = _labelAnalyzer.AnalyzeLabelClass(labelClass, layerName, context.DefaultBackgroundColor);
                    if (labelInfo is not null)
                    {
                        context.LabelClasses.Add(labelInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Extracts an RGB <see cref="ColorInfo"/> from a CIM color, handling different color models.
        /// Returns <c>null</c> if the color cannot be resolved.
        /// </summary>
        /// <param name="color">The CIM color to convert.</param>
        /// <returns>A <see cref="ColorInfo"/> or <c>null</c>.</returns>
        public static ColorInfo? ExtractColor(CIMColor? color)
        {
            if (color is null) return null;

            return color switch
            {
                CIMRGBColor rgb => new ColorInfo(
                    ClampToByte(rgb.R), ClampToByte(rgb.G), ClampToByte(rgb.B),
                    ClampToByte(rgb.Alpha * 2.55)), // CIM alpha is 0-100
                CIMHSVColor hsv => HsvToRgb(hsv.H, hsv.S, hsv.V, hsv.Alpha),
                CIMCMYKColor cmyk => CmykToRgb(cmyk.C, cmyk.M, cmyk.Y, cmyk.K, cmyk.Alpha),
                _ => null
            };
        }

        private static ColorInfo HsvToRgb(double h, double s, double v, double alpha)
        {
            // Normalize: H [0-360], S [0-100], V [0-100]
            double sn = s / 100.0;
            double vn = v / 100.0;
            double c = vn * sn;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = vn - c;

            double r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return new ColorInfo(
                ClampToByte((r + m) * 255),
                ClampToByte((g + m) * 255),
                ClampToByte((b + m) * 255),
                ClampToByte(alpha * 2.55));
        }

        private static ColorInfo CmykToRgb(double c, double m, double y, double k, double alpha)
        {
            // Simplified CMYK?RGB: R = 255 * (1-C/100) * (1-K/100)
            double cn = c / 100.0, mn = m / 100.0, yn = y / 100.0, kn = k / 100.0;
            return new ColorInfo(
                ClampToByte(255 * (1 - cn) * (1 - kn)),
                ClampToByte(255 * (1 - mn) * (1 - kn)),
                ClampToByte(255 * (1 - yn) * (1 - kn)),
                ClampToByte(alpha * 2.55));
        }

        private static byte ClampToByte(double value)
        {
            return (byte)Math.Clamp(value, 0, 255);
        }
    }
}
