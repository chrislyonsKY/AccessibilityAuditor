using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Services.ColorAnalysis;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;

namespace AccessibilityAuditor.Services.Fixes
{
    /// <summary>
    /// Applies rule-based fixes that do not require an LLM API key.
    /// All fixes are deterministic and reversible via the standard Pro undo stack.
    /// </summary>
    public sealed class DeterministicFixStrategy : IFixStrategy
    {
        /// <summary>
        /// Rule IDs that this strategy can handle.
        /// </summary>
        internal static readonly HashSet<string> SupportedRuleIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "WCAG_1_4_3_CONTRAST",
            "WCAG_1_4_11_NON_TEXT",
            "WCAG_1_1_1_ALT_TEXT",
            "WCAG_1_4_1_USE_OF_COLOR"
        };

        /// <inheritdoc/>
        public bool RequiresApiKey => false;

        /// <inheritdoc/>
        public async Task<FixResult> ApplyFixAsync(Finding finding, CancellationToken ct)
        {
            try
            {
                return finding.RuleId switch
                {
                    "WCAG_1_4_3_CONTRAST" => await FixContrastAsync(finding, ct),
                    "WCAG_1_4_11_NON_TEXT" => await FixContrastAsync(finding, ct),
                    "WCAG_1_1_1_ALT_TEXT" => FixAltTextStub(finding),
                    "WCAG_1_4_1_USE_OF_COLOR" => FixColorblindPalette(finding),
                    _ => new FixResult(FixStatus.Failed, $"No deterministic fix for '{finding.RuleId}'.")
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Deterministic fix failed for rule '{finding.RuleId}': {ex}");
                return new FixResult(FixStatus.Failed, $"Fix error: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the nearest WCAG AA-passing color and attempts to apply it
        /// to the CIM element via QueuedTask.Run(). Falls back to Suggested if
        /// the element cannot be located in the active map or layout.
        /// </summary>
        private async Task<FixResult> FixContrastAsync(Finding finding, CancellationToken ct)
        {
            if (finding.ForegroundColor is null || finding.BackgroundColor is null)
                return new FixResult(FixStatus.Failed, "Missing color data for contrast fix.");

            var fg = finding.ForegroundColor;
            var bg = finding.BackgroundColor;

            // AA normal text requires 4.5:1; non-text / large text requires 3:1
            double targetRatio = finding.RuleId == "WCAG_1_4_11_NON_TEXT" ? 3.0 : 4.5;

            double currentRatio = ContrastCalculator.Calculate(fg, bg);
            if (currentRatio >= targetRatio)
                return new FixResult(FixStatus.Applied,
                    $"Already meets contrast target ({currentRatio:F1}:1 >= {targetRatio}:1).");

            var fixedColor = FindNearestPassingColor(fg, bg, targetRatio);
            if (fixedColor is null)
                return new FixResult(FixStatus.Failed,
                    "Could not find a passing color in the same hue family.");

            double newRatio = ContrastCalculator.Calculate(fixedColor, bg);
            string summary = $"Change foreground from {fg.Hex} to {fixedColor.Hex} " +
                $"(contrast {currentRatio:F1}:1 \u2192 {newRatio:F1}:1).";

            // Attempt CIM application — may fail if Pro runtime is unavailable
            bool applied = false;
            try
            {
                applied = await TryApplyColorToCimAsync(finding, fixedColor);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CIM application unavailable: {ex.Message}");
            }

            return applied
                ? new FixResult(FixStatus.Applied, summary, fixedColor.Hex)
                : new FixResult(FixStatus.Suggested, summary, fixedColor.Hex);
        }

        /// <summary>
        /// Inserts a "[Description required]" alt-text placeholder stub on the element.
        /// CIM layout elements don't have a direct alt-text field, so this remains a suggestion.
        /// </summary>
        private static FixResult FixAltTextStub(Finding finding)
        {
            const string stub = "[Description required]";
            return new FixResult(FixStatus.Suggested,
                $"Add alt text: \"{stub}\" to {finding.Element}.",
                stub);
        }

        /// <summary>
        /// Suggests switching to a colorblind-safe alternative for the element.
        /// </summary>
        private static FixResult FixColorblindPalette(Finding finding)
        {
            return new FixResult(FixStatus.Suggested,
                $"Consider adding a secondary visual channel (pattern, label, or shape) " +
                $"to {finding.Element} so colour is not the sole differentiator.",
                "Add pattern fill, symbol shape variation, or direct labels.");
        }

        #region CIM Application

        /// <summary>
        /// Attempts to apply a corrected foreground color to the CIM element.
        /// Tries layout text elements first, then map label classes.
        /// Returns false if the element cannot be located.
        /// All CIM access is inside QueuedTask.Run().
        /// </summary>
        private static async Task<bool> TryApplyColorToCimAsync(Finding finding, ColorInfo fixedColor)
        {
            try
            {
                return await QueuedTask.Run(() =>
                {
                    // Try layout text element
                    if (TryApplyToLayoutTextElement(finding, fixedColor))
                        return true;

                    // Try map label class
                    if (TryApplyToMapLabelClass(finding, fixedColor))
                        return true;

                    return false;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CIM application failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Applies corrected color to a layout text element's CIM text symbol.
        /// </summary>
        private static bool TryApplyToLayoutTextElement(Finding finding, ColorInfo fixedColor)
        {
            var layout = LayoutView.Active?.Layout;
            if (layout is null) return false;

            // Use NavigationTarget as element name, or extract from Element string
            string? elementName = finding.NavigationTarget ?? ExtractQuotedName(finding.Element);
            if (elementName is null) return false;

            var element = layout.FindElement(elementName);
            if (element is not TextElement textEl) return false;

            var cimElement = textEl.GetDefinition();
            if (cimElement is not CIMGraphicElement graphicElement) return false;
            if (graphicElement.Graphic is not CIMTextGraphic cimTextGraphic) return false;
            if (cimTextGraphic.Symbol?.Symbol is not CIMTextSymbol textSym) return false;

            if (textSym.Symbol is CIMPolygonSymbol polySym && polySym.SymbolLayers is not null)
            {
                bool modified = false;
                foreach (var symLayer in polySym.SymbolLayers)
                {
                    if (symLayer is CIMSolidFill fill)
                    {
                        fill.Color = CIMColor.CreateRGBColor(fixedColor.R, fixedColor.G, fixedColor.B);
                        modified = true;
                    }
                }

                if (modified)
                {
                    textEl.SetDefinition(cimElement);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Applies corrected color to a map layer's label class text symbol.
        /// </summary>
        private static bool TryApplyToMapLabelClass(Finding finding, ColorInfo fixedColor)
        {
            var map = MapView.Active?.Map;
            if (map is null || finding.LayerName is null) return false;

            var layer = map.GetLayersAsFlattenedList()
                .OfType<FeatureLayer>()
                .FirstOrDefault(l => l.Name == finding.LayerName);

            if (layer is null) return false;

            var cimDef = layer.GetDefinition() as CIMFeatureLayer;
            if (cimDef?.LabelClasses is null) return false;

            var labelClassName = ExtractQuotedName(finding.Element);
            bool modified = false;

            foreach (var lc in cimDef.LabelClasses)
            {
                if (labelClassName is not null && lc.Name != labelClassName)
                    continue;

                if (lc.TextSymbol?.Symbol is CIMTextSymbol textSym
                    && textSym.Symbol is CIMPolygonSymbol polySym
                    && polySym.SymbolLayers is not null)
                {
                    foreach (var symLayer in polySym.SymbolLayers)
                    {
                        if (symLayer is CIMSolidFill fill)
                        {
                            fill.Color = CIMColor.CreateRGBColor(fixedColor.R, fixedColor.G, fixedColor.B);
                            modified = true;
                        }
                    }
                }
            }

            if (modified)
            {
                layer.SetDefinition(cimDef);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Extracts the first single-quoted name from a string.
        /// E.g. "Label class 'Cities' on layer 'Transportation'" returns "Cities".
        /// </summary>
        internal static string? ExtractQuotedName(string text)
        {
            var match = Regex.Match(text, @"'([^']+)'");
            return match.Success ? match.Groups[1].Value : null;
        }

        #endregion

        #region HSL Color Helpers

        /// <summary>
        /// Finds the nearest WCAG-passing color by walking HSL lightness
        /// outward from the current value while preserving hue and saturation.
        /// </summary>
        internal static ColorInfo? FindNearestPassingColor(
            ColorInfo fg, ColorInfo bg, double targetRatio)
        {
            RgbToHsl(fg.R, fg.G, fg.B, out double h, out double s, out double currentL);

            ColorInfo? best = null;
            double bestDist = double.MaxValue;

            for (int step = 0; step <= 100; step++)
            {
                double candidateL = step / 100.0;
                HslToRgb(h, s, candidateL, out byte cr, out byte cg, out byte cb);
                var candidate = new ColorInfo(cr, cg, cb);

                double ratio = ContrastCalculator.Calculate(candidate, bg);
                if (ratio >= targetRatio)
                {
                    double dist = Math.Abs(candidateL - currentL);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = candidate;
                    }
                }
            }

            return best;
        }

        internal static void RgbToHsl(byte r, byte g, byte b,
            out double h, out double s, out double l)
        {
            double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            l = (max + min) / 2.0;

            if (delta == 0)
            {
                h = 0;
                s = 0;
            }
            else
            {
                s = l > 0.5 ? delta / (2.0 - max - min) : delta / (max + min);

                if (max == rd)
                    h = ((gd - bd) / delta + (gd < bd ? 6 : 0)) / 6.0;
                else if (max == gd)
                    h = ((bd - rd) / delta + 2) / 6.0;
                else
                    h = ((rd - gd) / delta + 4) / 6.0;
            }
        }

        internal static void HslToRgb(double h, double s, double l,
            out byte r, out byte g, out byte b)
        {
            double rd, gd, bd;

            if (s == 0)
            {
                rd = gd = bd = l;
            }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;
                rd = HueToRgb(p, q, h + 1.0 / 3.0);
                gd = HueToRgb(p, q, h);
                bd = HueToRgb(p, q, h - 1.0 / 3.0);
            }

            r = (byte)Math.Clamp((int)(rd * 255 + 0.5), 0, 255);
            g = (byte)Math.Clamp((int)(gd * 255 + 0.5), 0, 255);
            b = (byte)Math.Clamp((int)(bd * 255 + 0.5), 0, 255);
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
            return p;
        }

        #endregion
    }
}
