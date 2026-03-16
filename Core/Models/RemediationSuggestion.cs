namespace AccessibilityAuditor.Core.Models
{
    /// <summary>
    /// Provides remediation guidance for a specific accessibility finding.
    /// </summary>
    public sealed class RemediationSuggestion
    {
        /// <summary>
        /// Gets or sets the short summary of the suggested fix.
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the detailed step-by-step instructions.
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the suggested replacement color, if applicable.
        /// </summary>
        public ColorInfo? SuggestedColor { get; set; }

        /// <summary>
        /// Gets or sets the expected contrast ratio after applying the fix.
        /// </summary>
        public double? ExpectedContrastRatio { get; set; }
    }
}
