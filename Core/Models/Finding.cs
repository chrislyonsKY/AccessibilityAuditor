using System;
using System.ComponentModel;

namespace AccessibilityAuditor.Core.Models
{
    /// <summary>
    /// Represents a single accessibility compliance finding produced by a rule evaluation.
    /// </summary>
    public sealed class Finding : INotifyPropertyChanged
    {
        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        /// <summary>
        /// Gets or sets the rule identifier that produced this finding (e.g., "WCAG_1_4_3_CONTRAST").
        /// </summary>
        public string RuleId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the WCAG criterion this finding relates to.
        /// </summary>
        public WcagCriterion? Criterion { get; set; }

        /// <summary>
        /// Gets or sets the severity of this finding.
        /// </summary>
        public FindingSeverity Severity { get; set; }

        /// <summary>
        /// Gets or sets the element that was checked (e.g., "Label class 'Cities' on layer 'Transportation'").
        /// </summary>
        public string Element { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a human-readable description of what was found.
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets remediation guidance, if applicable.
        /// </summary>
        public string? Remediation { get; set; }

        /// <summary>
        /// Gets or sets the layer name for context, if applicable.
        /// </summary>
        public string? LayerName { get; set; }

        /// <summary>
        /// Gets or sets a navigation target (URI or path) to locate the element in ArcGIS Pro.
        /// </summary>
        public string? NavigationTarget { get; set; }

        /// <summary>
        /// Gets or sets the foreground color information, if this finding involves color contrast.
        /// </summary>
        public ColorInfo? ForegroundColor { get; set; }

        /// <summary>
        /// Gets or sets the background color information, if this finding involves color contrast.
        /// </summary>
        public ColorInfo? BackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the computed contrast ratio, if applicable.
        /// </summary>
        public double? ContrastRatio { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this finding was generated.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        #region Fix State (updated after fix application)

        private string? _fixStatusText;
        /// <summary>
        /// Display text for the fix result (e.g. "Fixed", "Suggested: #4A4A4A", "Failed").
        /// Null when no fix has been attempted.
        /// </summary>
        public string? FixStatusText
        {
            get => _fixStatusText;
            set { _fixStatusText = value; OnPropertyChanged(nameof(FixStatusText)); OnPropertyChanged(nameof(IsFixed)); }
        }

        private bool _isFixed;
        /// <summary>True after a fix has been successfully applied.</summary>
        public bool IsFixed
        {
            get => _isFixed;
            set { _isFixed = value; OnPropertyChanged(nameof(IsFixed)); }
        }

        #endregion
    }
}
