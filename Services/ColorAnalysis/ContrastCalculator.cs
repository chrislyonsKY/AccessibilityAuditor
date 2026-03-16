using System;
using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.Services.ColorAnalysis
{
    /// <summary>
    /// Calculates WCAG 2.1 contrast ratios between two colors.
    /// </summary>
    public static class ContrastCalculator
    {
        /// <summary>
        /// Computes the WCAG 2.1 contrast ratio between two colors.
        /// </summary>
        /// <param name="foreground">The foreground color.</param>
        /// <param name="background">The background color.</param>
        /// <returns>The contrast ratio (1:1 to 21:1).</returns>
        /// <exception cref="ArgumentNullException">Thrown when either color is <c>null</c>.</exception>
        public static double Calculate(ColorInfo foreground, ColorInfo background)
        {
            if (foreground is null) throw new ArgumentNullException(nameof(foreground));
            if (background is null) throw new ArgumentNullException(nameof(background));

            // Handle alpha compositing — composite foreground over background
            var fg = foreground.A < 255
                ? foreground.CompositeOver(background)
                : foreground;

            double l1 = RelativeLuminance.Calculate(fg.R, fg.G, fg.B);
            double l2 = RelativeLuminance.Calculate(background.R, background.G, background.B);

            return ContrastRatio(l1, l2);
        }

        /// <summary>
        /// Computes the contrast ratio from two relative luminance values.
        /// </summary>
        /// <param name="l1">First relative luminance.</param>
        /// <param name="l2">Second relative luminance.</param>
        /// <returns>The contrast ratio (always ? 1).</returns>
        public static double ContrastRatio(double l1, double l2)
        {
            double lighter = Math.Max(l1, l2);
            double darker = Math.Min(l1, l2);
            return (lighter + 0.05) / (darker + 0.05);
        }
    }
}
