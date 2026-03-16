using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Services.ColorAnalysis;

namespace AccessibilityAuditor.ViewModels
{
    /// <summary>
    /// ViewModel for the ColorSimulationWindow (modeless ProWindow).
    /// Displays the full map palette under each colorblind simulation type
    /// with pairwise distinguishability analysis and simulated contrast ratios.
    /// 
    /// Key differentiators from Pro's built-in View > Accessibility simulator:
    /// - Symbology-aware: each swatch shows its layer name and category
    /// - Pairwise analysis: identifies exact pairs that become indistinguishable
    /// - Simulated contrast: checks if WCAG contrast ratios hold under simulation
    /// - Works on portal web maps, not just the active map view
    /// </summary>
    internal sealed class ColorSimulationViewModel : ObservableObject
    {
        /// <summary>
        /// Initializes from a labeled palette (preferred — includes layer/category context).
        /// </summary>
        public ColorSimulationViewModel(IReadOnlyList<LabeledColor> labeledPalette, ColorInfo background, string targetName)
        {
            TargetName = targetName ?? "Unknown";
            WindowTitle = $"Colorblind Simulation — {TargetName}";

            var flatColors = new List<ColorInfo>();

            foreach (var lc in labeledPalette)
            {
                flatColors.Add(lc.Color);

                OriginalSwatches.Add(new ColorSwatch(lc.Color, lc.DisplayLabel));

                ProtanSwatches.Add(new ColorSwatch(
                    ColorBlindSimulator.Simulate(lc.Color, ColorBlindType.Protanopia), lc.DisplayLabel));
                DeuteranSwatches.Add(new ColorSwatch(
                    ColorBlindSimulator.Simulate(lc.Color, ColorBlindType.Deuteranopia), lc.DisplayLabel));
                TritanSwatches.Add(new ColorSwatch(
                    ColorBlindSimulator.Simulate(lc.Color, ColorBlindType.Tritanopia), lc.DisplayLabel));
            }

            BuildAnalysis(flatColors, labeledPalette, background);
        }

        /// <summary>
        /// Initializes from a flat color palette (fallback for portal web maps).
        /// </summary>
        public ColorSimulationViewModel(IReadOnlyList<ColorInfo> palette, string targetName)
        {
            TargetName = targetName ?? "Unknown";
            WindowTitle = $"Colorblind Simulation — {TargetName}";

            var labeledPalette = new List<LabeledColor>();
            foreach (var color in palette)
            {
                labeledPalette.Add(new LabeledColor(color, "", "", ""));

                OriginalSwatches.Add(new ColorSwatch(color, "Original"));
                ProtanSwatches.Add(new ColorSwatch(
                    ColorBlindSimulator.Simulate(color, ColorBlindType.Protanopia), color.Hex));
                DeuteranSwatches.Add(new ColorSwatch(
                    ColorBlindSimulator.Simulate(color, ColorBlindType.Deuteranopia), color.Hex));
                TritanSwatches.Add(new ColorSwatch(
                    ColorBlindSimulator.Simulate(color, ColorBlindType.Tritanopia), color.Hex));
            }

            BuildAnalysis(palette, labeledPalette, new ColorInfo(255, 255, 255));
        }

        private void BuildAnalysis(IReadOnlyList<ColorInfo> flatColors, IReadOnlyList<LabeledColor> labeledPalette, ColorInfo background)
        {
            // Pairwise distinguishability
            var cbResults = PaletteEvaluator.EvaluateColorBlindSafety(flatColors);
            foreach (var result in cbResults)
            {
                if (result.AllDistinguishable) continue;

                foreach (var (c1, c2) in result.FailingPairs)
                {
                    // Find the labeled context for each color
                    var lc1 = labeledPalette.FirstOrDefault(l => l.Color.Equals(c1));
                    var lc2 = labeledPalette.FirstOrDefault(l => l.Color.Equals(c2));

                    Issues.Add(new DistinguishabilityIssue
                    {
                        Type = result.Type,
                        Color1 = c1,
                        Color2 = c2,
                        TypeLabel = result.Type.ToString(),
                        Label1 = lc1?.DisplayLabel ?? c1.Hex,
                        Label2 = lc2?.DisplayLabel ?? c2.Hex
                    });
                }
            }

            // Simulated contrast: check if colors still meet 3:1 against background under simulation
            foreach (ColorBlindType cbType in Enum.GetValues(typeof(ColorBlindType)))
            {
                var simBg = ColorBlindSimulator.Simulate(background, cbType);

                foreach (var lc in labeledPalette)
                {
                    var simColor = ColorBlindSimulator.Simulate(lc.Color, cbType);
                    double originalRatio = ContrastCalculator.Calculate(lc.Color, background);
                    double simRatio = ContrastCalculator.Calculate(simColor, simBg);

                    // Flag if contrast drops below 3:1 under simulation when it was passing before
                    if (originalRatio >= 3.0 && simRatio < 3.0)
                    {
                        ContrastLosses.Add(new SimulatedContrastLoss
                        {
                            Type = cbType,
                            TypeLabel = cbType.ToString(),
                            Color = lc.Color,
                            Label = lc.DisplayLabel.Length > 0 ? lc.DisplayLabel : lc.Color.Hex,
                            OriginalRatio = originalRatio,
                            SimulatedRatio = simRatio
                        });
                    }
                }
            }

            HasIssues = Issues.Count > 0;
            HasContrastLosses = ContrastLosses.Count > 0;

            IssuesSummary = HasIssues
                ? $"{Issues.Count} pair(s) become indistinguishable under colorblind simulation"
                : "All colors remain distinguishable under all simulation types";

            ContrastLossSummary = HasContrastLosses
                ? $"{ContrastLosses.Count} color(s) drop below 3:1 contrast under simulation"
                : "All colors maintain sufficient contrast under simulation";
        }

        /// <summary>Gets the window title.</summary>
        public string WindowTitle { get; }

        /// <summary>Gets the target name.</summary>
        public string TargetName { get; }

        /// <summary>Gets the original color swatches.</summary>
        public ObservableCollection<ColorSwatch> OriginalSwatches { get; } = new();

        /// <summary>Gets the protanopia-simulated swatches.</summary>
        public ObservableCollection<ColorSwatch> ProtanSwatches { get; } = new();

        /// <summary>Gets the deuteranopia-simulated swatches.</summary>
        public ObservableCollection<ColorSwatch> DeuteranSwatches { get; } = new();

        /// <summary>Gets the tritanopia-simulated swatches.</summary>
        public ObservableCollection<ColorSwatch> TritanSwatches { get; } = new();

        /// <summary>Gets the distinguishability issues found.</summary>
        public ObservableCollection<DistinguishabilityIssue> Issues { get; } = new();

        /// <summary>Gets the simulated contrast losses found.</summary>
        public ObservableCollection<SimulatedContrastLoss> ContrastLosses { get; } = new();

        /// <summary>Gets whether any distinguishability issues exist.</summary>
        public bool HasIssues { get; private set; }

        /// <summary>Gets whether any contrast losses were found under simulation.</summary>
        public bool HasContrastLosses { get; private set; }

        /// <summary>Gets the issues summary text.</summary>
        public string IssuesSummary { get; private set; } = string.Empty;

        /// <summary>Gets the contrast loss summary text.</summary>
        public string ContrastLossSummary { get; private set; } = string.Empty;
    }

    /// <summary>
    /// A single color swatch for display in the simulation grid.
    /// </summary>
    internal sealed class ColorSwatch
    {
        public ColorSwatch(ColorInfo color, string label)
        {
            Color = color;
            Label = label;
            Hex = color.Hex;
            Brush = new SolidColorBrush(
                System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            Brush.Freeze();
        }

        /// <summary>Gets the color info.</summary>
        public ColorInfo Color { get; }

        /// <summary>Gets the label for the swatch (layer — category for labeled, hex for flat).</summary>
        public string Label { get; }

        /// <summary>Gets the hex code.</summary>
        public string Hex { get; }

        /// <summary>Gets the WPF brush.</summary>
        public SolidColorBrush Brush { get; }
    }

    /// <summary>
    /// A pair of colors that become indistinguishable under a specific color vision deficiency.
    /// </summary>
    internal sealed class DistinguishabilityIssue
    {
        /// <summary>Gets or sets the deficiency type.</summary>
        public ColorBlindType Type { get; set; }

        /// <summary>Gets or sets the type label.</summary>
        public string TypeLabel { get; set; } = string.Empty;

        /// <summary>Gets or sets the first color.</summary>
        public ColorInfo Color1 { get; set; } = null!;

        /// <summary>Gets or sets the second color.</summary>
        public ColorInfo Color2 { get; set; } = null!;

        /// <summary>Gets or sets the layer/category label for the first color.</summary>
        public string Label1 { get; set; } = string.Empty;

        /// <summary>Gets or sets the layer/category label for the second color.</summary>
        public string Label2 { get; set; } = string.Empty;

        /// <summary>Gets the first color hex code.</summary>
        public string Hex1 => Color1.Hex;

        /// <summary>Gets the second color hex code.</summary>
        public string Hex2 => Color2.Hex;

        /// <summary>Gets a description of the issue with semantic context.</summary>
        public string Description =>
            !string.IsNullOrEmpty(Label1) && Label1 != Color1.Hex
                ? $"{Label1} ? {Label2} under {TypeLabel}"
                : $"{Color1.Hex} ? {Color2.Hex} under {TypeLabel}";

        /// <summary>Gets the first color as a brush.</summary>
        public SolidColorBrush Brush1 => MakeBrush(Color1);

        /// <summary>Gets the second color as a brush.</summary>
        public SolidColorBrush Brush2 => MakeBrush(Color2);

        private static SolidColorBrush MakeBrush(ColorInfo c)
        {
            var b = new SolidColorBrush(
                System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B));
            b.Freeze();
            return b;
        }
    }

    /// <summary>
    /// A color whose WCAG contrast ratio drops below threshold under colorblind simulation.
    /// </summary>
    internal sealed class SimulatedContrastLoss
    {
        /// <summary>Gets or sets the deficiency type.</summary>
        public ColorBlindType Type { get; set; }

        /// <summary>Gets or sets the type label.</summary>
        public string TypeLabel { get; set; } = string.Empty;

        /// <summary>Gets or sets the color.</summary>
        public ColorInfo Color { get; set; } = null!;

        /// <summary>Gets or sets the layer/category label.</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Gets or sets the original contrast ratio.</summary>
        public double OriginalRatio { get; set; }

        /// <summary>Gets or sets the simulated contrast ratio.</summary>
        public double SimulatedRatio { get; set; }

        /// <summary>Gets a description of the contrast loss.</summary>
        public string Description =>
            $"{Label}: {OriginalRatio:F1}:1 ? {SimulatedRatio:F1}:1 under {TypeLabel}";

        /// <summary>Gets the color as a brush.</summary>
        public SolidColorBrush Brush => MakeBrush(Color);

        private static SolidColorBrush MakeBrush(ColorInfo c)
        {
            var b = new SolidColorBrush(
                System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B));
            b.Freeze();
            return b;
        }
    }
}
