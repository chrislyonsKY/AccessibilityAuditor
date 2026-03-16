using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Layouts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Services.ColorAnalysis;

namespace AccessibilityAuditor.ViewModels
{
    /// <summary>
    /// ViewModel for the finding detail view / ProWindow, showing color preview,
    /// colorblind simulation, and remediation guidance.
    /// </summary>
    internal sealed class FindingDetailViewModel : ObservableObject
    {
        /// <summary>
        /// Initializes a new <see cref="FindingDetailViewModel"/> for the specified finding.
        /// </summary>
        /// <param name="finding">The finding to display.</param>
        public FindingDetailViewModel(Finding finding)
        {
            Finding = finding;

            if (finding.ForegroundColor is not null)
            {
                ForegroundBrush = ToBrush(finding.ForegroundColor);
                ForegroundHex = finding.ForegroundColor.Hex;

                ProtanForegroundBrush = ToBrush(ColorBlindSimulator.Simulate(finding.ForegroundColor, ColorBlindType.Protanopia));
                DeuteranForegroundBrush = ToBrush(ColorBlindSimulator.Simulate(finding.ForegroundColor, ColorBlindType.Deuteranopia));
                TritanForegroundBrush = ToBrush(ColorBlindSimulator.Simulate(finding.ForegroundColor, ColorBlindType.Tritanopia));
            }

            if (finding.BackgroundColor is not null)
            {
                BackgroundBrush = ToBrush(finding.BackgroundColor);
                BackgroundHex = finding.BackgroundColor.Hex;

                ProtanBackgroundBrush = ToBrush(ColorBlindSimulator.Simulate(finding.BackgroundColor, ColorBlindType.Protanopia));
                DeuteranBackgroundBrush = ToBrush(ColorBlindSimulator.Simulate(finding.BackgroundColor, ColorBlindType.Deuteranopia));
                TritanBackgroundBrush = ToBrush(ColorBlindSimulator.Simulate(finding.BackgroundColor, ColorBlindType.Tritanopia));
            }

            NavigateCommand = new RelayCommand(NavigateToElement, () => CanNavigate);
            CopyDetailCommand = new RelayCommand(CopyDetailToClipboard);
        }

        /// <summary>Gets the finding being displayed.</summary>
        public Finding Finding { get; }

        /// <summary>Gets the window title.</summary>
        public string WindowTitle => $"Finding Detail — {Finding.Criterion?.Id ?? Finding.RuleId}";

        /// <summary>Gets the severity display text (icon + label combined, for non-MDL2 contexts).</summary>
        public string SeverityText => $"{SeverityIcon} {SeverityLabel}";

        /// <summary>Gets the Segoe MDL2 Assets glyph for the severity.</summary>
        public string SeverityIcon => Finding.Severity switch
        {
            FindingSeverity.Pass => "\uE73E",
            FindingSeverity.Warning => "\uE7BA",
            FindingSeverity.Fail => "\uE711",
            FindingSeverity.ManualReview => "\uE7B3",
            FindingSeverity.Error => "\uEA39",
            _ => "\uE897"
        };

        /// <summary>Gets the severity icon color brush.</summary>
        public SolidColorBrush SeverityBrush
        {
            get
            {
                var brush = Finding.Severity switch
                {
                    FindingSeverity.Pass => new SolidColorBrush(Color.FromRgb(0x3D, 0xA6, 0x3D)),
                    FindingSeverity.Warning => new SolidColorBrush(Color.FromRgb(0xE5, 0xA1, 0x00)),
                    FindingSeverity.Fail => new SolidColorBrush(Color.FromRgb(0xE0, 0x43, 0x43)),
                    FindingSeverity.ManualReview => new SolidColorBrush(Color.FromRgb(0x4D, 0x8F, 0xD6)),
                    FindingSeverity.Error => new SolidColorBrush(Color.FromRgb(0xCC, 0x33, 0x33)),
                    _ => new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99))
                };
                brush.Freeze();
                return brush;
            }
        }

        /// <summary>Gets the severity label text.</summary>
        public string SeverityLabel => Finding.Severity switch
        {
            FindingSeverity.Pass => "PASS",
            FindingSeverity.Warning => "WARNING",
            FindingSeverity.Fail => "FAIL",
            FindingSeverity.ManualReview => "MANUAL REVIEW",
            FindingSeverity.Error => "ERROR",
            _ => Finding.Severity.ToString()
        };

        /// <summary>Gets the criterion display text.</summary>
        public string CriterionText => Finding.Criterion?.ToString() ?? "Unknown Criterion";

        /// <summary>Gets whether this finding has color data to display.</summary>
        public bool HasColorData => Finding.ForegroundColor is not null && Finding.BackgroundColor is not null;

        /// <summary>Gets the contrast ratio formatted string.</summary>
        public string ContrastRatioText => Finding.ContrastRatio.HasValue
            ? $"{Finding.ContrastRatio.Value:F2}:1"
            : "N/A";

        // Foreground color
        public Brush? ForegroundBrush { get; }
        public string ForegroundHex { get; } = string.Empty;

        // Background color
        public Brush? BackgroundBrush { get; }
        public string BackgroundHex { get; } = string.Empty;

        // Colorblind simulation brushes
        public Brush? ProtanForegroundBrush { get; }
        public Brush? ProtanBackgroundBrush { get; }
        public Brush? DeuteranForegroundBrush { get; }
        public Brush? DeuteranBackgroundBrush { get; }
        public Brush? TritanForegroundBrush { get; }
        public Brush? TritanBackgroundBrush { get; }

        /// <summary>Gets whether navigation to the source element is possible.</summary>
        public bool CanNavigate => !string.IsNullOrEmpty(Finding.LayerName) || !string.IsNullOrEmpty(Finding.NavigationTarget);

        /// <summary>Gets the Navigate to Element command.</summary>
        public RelayCommand NavigateCommand { get; }

        /// <summary>Gets the Copy Detail command.</summary>
        public RelayCommand CopyDetailCommand { get; }

        private void NavigateToElement()
        {
            // Try to select the layer in the active map
            if (!string.IsNullOrEmpty(Finding.LayerName))
            {
                _ = QueuedTask.Run(() =>
                {
                    try
                    {
                        var mapView = MapView.Active;
                        if (mapView?.Map is null) return;

                        // Find the layer by name
                        var layer = mapView.Map.GetLayersAsFlattenedList()
                            .FirstOrDefault(l => l.Name == Finding.LayerName);

                        if (layer is not null)
                        {
                            // Ensure the layer is visible and select it in the Contents pane
                            layer.SetVisibility(true);
                            mapView.SelectLayers(new[] { layer });

                            System.Diagnostics.Debug.WriteLine($"Navigated to layer '{Finding.LayerName}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Navigate failed: {ex.Message}");
                    }
                });

                return;
            }

            // Try layout element navigation
            if (!string.IsNullOrEmpty(Finding.NavigationTarget))
            {
                _ = QueuedTask.Run(() =>
                {
                    try
                    {
                        var layoutView = LayoutView.Active;
                        if (layoutView?.Layout is null) return;

                        var elements = layoutView.Layout.GetElements();
                        var element = elements.FirstOrDefault(e => e.Name == Finding.NavigationTarget);

                        if (element is not null)
                        {
                            layoutView.ClearElementSelection();
                            layoutView.SelectElement(element);

                            System.Diagnostics.Debug.WriteLine($"Navigated to layout element '{Finding.NavigationTarget}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Navigate failed: {ex.Message}");
                    }
                });
            }
        }

        private void CopyDetailToClipboard()
        {
            var text = $"[{Finding.Severity}] {Finding.Criterion?.Id ?? Finding.RuleId}\n" +
                       $"Element: {Finding.Element}\n" +
                       $"Detail: {Finding.Detail}";

            if (Finding.ContrastRatio.HasValue)
                text += $"\nContrast: {Finding.ContrastRatio.Value:F2}:1";

            if (!string.IsNullOrEmpty(Finding.Remediation))
                text += $"\nRemediation: {Finding.Remediation}";

            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clipboard copy failed: {ex.Message}");
            }
        }

        private static SolidColorBrush ToBrush(ColorInfo color)
        {
            var brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
            brush.Freeze();
            return brush;
        }
    }
}
