using System;
using System.Collections.Generic;
using System.Linq;
using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.Services.ColorAnalysis
{
    /// <summary>
    /// Evaluates an entire color palette for accessibility, including pairwise contrast
    /// and colorblind distinguishability.
    /// </summary>
    public sealed class PaletteEvaluator
    {
        /// <summary>
        /// Evaluates pairwise contrast ratios among a set of colors against a background.
        /// </summary>
        /// <param name="colors">The palette colors to evaluate.</param>
        /// <param name="background">The background color.</param>
        /// <param name="minimumRatio">The minimum required contrast ratio (default: 3:1 for non-text).</param>
        /// <returns>A list of failing color pairs and their contrast ratios.</returns>
        public static IReadOnlyList<PaletteContrastResult> EvaluateAgainstBackground(
            IReadOnlyList<ColorInfo> colors, ColorInfo background, double minimumRatio = 3.0)
        {
            if (colors is null) throw new ArgumentNullException(nameof(colors));
            if (background is null) throw new ArgumentNullException(nameof(background));

            var results = new List<PaletteContrastResult>();

            foreach (var color in colors)
            {
                double ratio = ContrastCalculator.Calculate(color, background);
                results.Add(new PaletteContrastResult
                {
                    Color1 = color,
                    Color2 = background,
                    ContrastRatio = ratio,
                    MeetsThreshold = ratio >= minimumRatio
                });
            }

            return results;
        }

        /// <summary>
        /// Evaluates whether all colors in the palette remain pairwise distinguishable
        /// under each type of color vision deficiency.
        /// </summary>
        /// <param name="colors">The palette colors to evaluate.</param>
        /// <returns>Results for each color blind type.</returns>
        public static IReadOnlyList<ColorBlindDistinguishabilityResult> EvaluateColorBlindSafety(
            IReadOnlyList<ColorInfo> colors)
        {
            if (colors is null) throw new ArgumentNullException(nameof(colors));

            var results = new List<ColorBlindDistinguishabilityResult>();

            foreach (ColorBlindType cbType in Enum.GetValues(typeof(ColorBlindType)))
            {
                var failingPairs = new List<(ColorInfo, ColorInfo)>();

                for (int i = 0; i < colors.Count; i++)
                {
                    for (int j = i + 1; j < colors.Count; j++)
                    {
                        if (!ColorBlindSimulator.AreDistinguishable(colors[i], colors[j], cbType))
                        {
                            failingPairs.Add((colors[i], colors[j]));
                        }
                    }
                }

                results.Add(new ColorBlindDistinguishabilityResult
                {
                    Type = cbType,
                    FailingPairs = failingPairs,
                    AllDistinguishable = failingPairs.Count == 0
                });
            }

            return results;
        }
    }

    /// <summary>
    /// Result of a pairwise contrast evaluation.
    /// </summary>
    public sealed class PaletteContrastResult
    {
        /// <summary>Gets or sets the first color.</summary>
        public ColorInfo Color1 { get; set; } = null!;

        /// <summary>Gets or sets the second color.</summary>
        public ColorInfo Color2 { get; set; } = null!;

        /// <summary>Gets or sets the computed contrast ratio.</summary>
        public double ContrastRatio { get; set; }

        /// <summary>Gets or sets a value indicating whether the pair meets the threshold.</summary>
        public bool MeetsThreshold { get; set; }
    }

    /// <summary>
    /// Result of colorblind distinguishability evaluation for one deficiency type.
    /// </summary>
    public sealed class ColorBlindDistinguishabilityResult
    {
        /// <summary>Gets or sets the color vision deficiency type tested.</summary>
        public ColorBlindType Type { get; set; }

        /// <summary>Gets or sets the pairs of colors that are not distinguishable.</summary>
        public List<(ColorInfo, ColorInfo)> FailingPairs { get; set; } = new();

        /// <summary>Gets or sets a value indicating whether all colors are distinguishable.</summary>
        public bool AllDistinguishable { get; set; }
    }
}
