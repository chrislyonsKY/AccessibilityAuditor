using System;
using System.Linq;
using ArcGIS.Core.CIM;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.Services.CimInspector
{
    /// <summary>
    /// Analyzes CIM label classes to extract font, color, and halo properties.
    /// Must be called on the MCT via <c>QueuedTask.Run()</c>.
    /// </summary>
    public sealed class LabelAnalyzer
    {
        /// <summary>
        /// Analyzes a CIM label class and returns structured label information.
        /// </summary>
        /// <param name="labelClass">The CIM label class to analyze.</param>
        /// <param name="layerName">The name of the owning layer.</param>
        /// <param name="defaultBackground">The default background color for contrast estimation.</param>
        /// <returns>A <see cref="LabelClassInfo"/> or <c>null</c> if the label class cannot be analyzed.</returns>
        public LabelClassInfo? AnalyzeLabelClass(CIMLabelClass? labelClass, string layerName, ColorInfo defaultBackground)
        {
            if (labelClass is null) return null;

            var info = new LabelClassInfo
            {
                ClassName = labelClass.Name ?? "Unnamed",
                LayerName = layerName,
                IsVisible = labelClass.Visibility,
                EstimatedBackgroundColor = defaultBackground
            };

            var textSymbol = labelClass.TextSymbol?.Symbol as CIMTextSymbol;
            if (textSymbol is null) return info;

            info.FontSize = textSymbol.Height;
            info.FontFamily = textSymbol.FontFamilyName ?? "Unknown";
            info.IsBold = IsBoldStyle(textSymbol.FontStyleName);

            // Extract text color from the text symbol's symbol layers
            info.ForegroundColor = ExtractTextColor(textSymbol) ?? new ColorInfo(0, 0, 0);

            // Extract halo if present
            ExtractHalo(textSymbol, info);

            return info;
        }

        private static ColorInfo? ExtractTextColor(CIMTextSymbol textSymbol)
        {
            // The text color is in the Symbol property's symbol layers
            if (textSymbol.Symbol?.SymbolLayers is not null)
            {
                var solidFill = textSymbol.Symbol.SymbolLayers
                    .OfType<CIMSolidFill>()
                    .FirstOrDefault();

                if (solidFill is not null)
                {
                    return CimWalker.ExtractColor(solidFill.Color);
                }
            }

            return null;
        }

        private static void ExtractHalo(CIMTextSymbol textSymbol, LabelClassInfo info)
        {
            if (textSymbol.HaloSize > 0 && textSymbol.HaloSymbol is not null)
            {
                info.HaloSize = textSymbol.HaloSize;

                // Try to extract halo color from the halo symbol's layers
                if (textSymbol.HaloSymbol is CIMPolygonSymbol haloPolygon)
                {
                    var haloFill = haloPolygon.SymbolLayers?
                        .OfType<CIMSolidFill>()
                        .FirstOrDefault();

                    if (haloFill is not null)
                    {
                        info.HaloColor = CimWalker.ExtractColor(haloFill.Color);
                    }
                }
            }
        }

        private static bool IsBoldStyle(string? fontStyleName)
        {
            if (string.IsNullOrEmpty(fontStyleName)) return false;
            return fontStyleName.Contains("Bold", StringComparison.OrdinalIgnoreCase);
        }
    }
}
