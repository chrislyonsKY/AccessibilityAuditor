using System;
using AccessibilityAuditor.Core.Constants;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Services.ColorAnalysis;

namespace AccessibilityAuditor.Services.RuleEngine
{
    /// <summary>
    /// Generates actionable remediation suggestions for accessibility findings.
    /// </summary>
    public static class RemediationEngine
    {
        /// <summary>
        /// Suggests a contrast fix by recommending a replacement foreground color
        /// that meets the specified threshold against the given background.
        /// </summary>
        /// <param name="foreground">The current foreground color.</param>
        /// <param name="background">The background color.</param>
        /// <param name="requiredRatio">The minimum required contrast ratio.</param>
        /// <returns>A human-readable remediation string.</returns>
        public static string SuggestContrastFix(ColorInfo foreground, ColorInfo background, double requiredRatio)
        {
            if (foreground is null || background is null)
                return "Adjust the color to meet the required contrast ratio.";

            double bgLuminance = RelativeLuminance.Calculate(background.R, background.G, background.B);

            // Try white and black to determine which direction to suggest
            double whiteRatio = ContrastCalculator.ContrastRatio(1.0, bgLuminance);
            double blackRatio = ContrastCalculator.ContrastRatio(bgLuminance, 0.0);

            if (whiteRatio >= requiredRatio && blackRatio >= requiredRatio)
            {
                return $"Change text color to white ({whiteRatio:F1}:1) or black ({blackRatio:F1}:1). " +
                       "Both meet the required contrast ratio. Alternatively, add a halo effect with a contrasting color.";
            }

            if (whiteRatio >= requiredRatio)
            {
                return $"Change text color to white (#FFFFFF) for {whiteRatio:F1}:1 contrast. " +
                       "Alternatively, add a halo effect with a contrasting color.";
            }

            if (blackRatio >= requiredRatio)
            {
                return $"Change text color to black (#000000) for {blackRatio:F1}:1 contrast. " +
                       "Alternatively, add a halo effect with a contrasting color.";
            }

            return "The background color makes it difficult to achieve sufficient contrast. " +
                   "Consider changing the background color or adding a halo/banner behind the text.";
        }

        /// <summary>
        /// Suggests a fix for missing alt text / descriptions.
        /// </summary>
        /// <param name="elementType">The type of element missing alt text.</param>
        /// <param name="elementName">The name of the element.</param>
        /// <returns>A human-readable remediation string.</returns>
        public static string SuggestAltTextFix(string elementType, string elementName)
        {
            // ArcGIS Pro stores element descriptions in CustomProperties via
            // Element Properties > General. There is no dedicated "Alt Text" field
            // in the Pro UI — the Description custom property serves this purpose
            // and is included when exporting to accessible PDF (Pro 3.2+).
            return elementType switch
            {
                "MapFrame" =>
                    $"Add a description to the map frame '{elementName}'. " +
                    "In the Contents pane, right-click the map frame > Properties > General. " +
                    "Enter a description of the map's geographic extent, key features, and purpose. " +
                    "This description is used as alt text when exporting to accessible PDF.",

                "PictureElement" =>
                    $"Add a description to the picture element '{elementName}'. " +
                    "In the Contents pane, right-click the element > Properties > General. " +
                    "Provide a concise description of the image content. " +
                    "If the image is purely decorative, describe it as 'Decorative image'.",

                "TextElement" or "ParagraphTextElement" =>
                    $"The text element '{elementName}' does not require alt text as it is already text content.",

                "Legend" =>
                    $"Add a description to the legend '{elementName}'. " +
                    "In the Contents pane, right-click the legend > Properties > General. " +
                    "Describe what the legend represents (e.g., 'Legend showing land use categories by color and pattern').",

                "ScaleBar" =>
                    $"Add a description to the scale bar '{elementName}'. " +
                    "In the Contents pane, right-click the scale bar > Properties > General. " +
                    "Describe the scale (e.g., 'Scale bar showing distances in miles').",

                "NorthArrow" =>
                    $"Add a description to the north arrow '{elementName}'. " +
                    "In the Contents pane, right-click the north arrow > Properties > General. " +
                    "A simple description like 'North arrow indicating map orientation' is sufficient.",

                _ =>
                    $"Add a description to '{elementName}'. " +
                    "In the Contents pane, right-click the element > Properties > General. " +
                    "Provide a concise description of what this element conveys. " +
                    "This description is included as alt text in accessible PDF exports."
            };
        }

        /// <summary>
        /// Suggests a fix for a missing or generic title.
        /// </summary>
        /// <param name="targetType">The type of target with the title issue.</param>
        /// <returns>A human-readable remediation string.</returns>
        public static string SuggestTitleFix(AuditTargetType targetType)
        {
            return targetType switch
            {
                AuditTargetType.Map =>
                    "Set a descriptive map name via Map Properties > General > Name. " +
                    "The title should describe the map's geographic area and subject (e.g., 'Kentucky Coal Mining Permits 2026').",

                AuditTargetType.Layout =>
                    "Set a descriptive layout name via Layout Properties > General > Name. " +
                    "The title should describe the map product's purpose and geographic extent.",

                _ =>
                    "Provide a descriptive, unique title that conveys the content's geographic scope and subject matter."
            };
        }

        /// <summary>
        /// Suggests a fix for renderers that rely solely on color to convey information.
        /// </summary>
        /// <param name="layerName">The name of the layer using color-only encoding.</param>
        /// <returns>A human-readable remediation string.</returns>
        public static string SuggestUseOfColorFix(string layerName)
        {
            return $"Layer '{layerName}' uses only color to distinguish categories. " +
                   "Add a second visual variable such as symbol shape, pattern fill, or size variation " +
                   "so that the information is conveyed without relying solely on color. " +
                   "In ArcGIS Pro, edit the symbology to use different marker shapes for point data, " +
                   "different dash patterns for lines, or hatching/pattern fills for polygons.";
        }

        /// <summary>
        /// Suggests a fix for non-text elements with insufficient contrast.
        /// </summary>
        /// <param name="layerName">The layer name.</param>
        /// <param name="symbolLabel">The symbol class label.</param>
        /// <param name="currentRatio">The current contrast ratio.</param>
        /// <returns>A human-readable remediation string.</returns>
        public static string SuggestNonTextContrastFix(string layerName, string symbolLabel, double currentRatio)
        {
            return $"Symbol '{symbolLabel}' on layer '{layerName}' has a contrast ratio of {currentRatio:F2}:1 " +
                   $"against the background (minimum required: {ContrastThresholds.NonTextGraphics}:1). " +
                   "Darken or lighten the symbol color to increase contrast. " +
                   "Adding a visible outline/stroke to the symbol can also help meet the contrast requirement.";
        }
    }
}
