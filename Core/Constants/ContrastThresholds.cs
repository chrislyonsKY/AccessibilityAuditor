namespace AccessibilityAuditor.Core.Constants
{
    /// <summary>
    /// WCAG AA contrast ratio thresholds.
    /// </summary>
    public static class ContrastThresholds
    {
        /// <summary>Minimum contrast ratio for normal text (&lt;18pt or &lt;14pt bold): 4.5:1.</summary>
        public const double NormalText = 4.5;

        /// <summary>Minimum contrast ratio for large text (?18pt or ?14pt bold): 3:1.</summary>
        public const double LargeText = 3.0;

        /// <summary>Minimum contrast ratio for non-text graphical elements and UI components: 3:1.</summary>
        public const double NonTextGraphics = 3.0;

        /// <summary>Point size threshold for large text (normal weight): 18pt.</summary>
        public const double LargeTextSizeNormal = 18.0;

        /// <summary>Point size threshold for large text (bold weight): 14pt.</summary>
        public const double LargeTextSizeBold = 14.0;

        /// <summary>Warning margin — findings within this margin above the threshold are flagged as warnings.</summary>
        public const double WarningMargin = 0.5;

        /// <summary>
        /// Determines whether the given font size and bold state qualifies as "large text" under WCAG.
        /// </summary>
        /// <param name="pointSize">The font size in points.</param>
        /// <param name="isBold">Whether the text is bold.</param>
        /// <returns><c>true</c> if the text qualifies as large text.</returns>
        public static bool IsLargeText(double pointSize, bool isBold)
        {
            return isBold ? pointSize >= LargeTextSizeBold : pointSize >= LargeTextSizeNormal;
        }

        /// <summary>
        /// Returns the appropriate contrast threshold for the given text properties.
        /// </summary>
        /// <param name="pointSize">The font size in points.</param>
        /// <param name="isBold">Whether the text is bold.</param>
        /// <returns>The minimum required contrast ratio.</returns>
        public static double GetThreshold(double pointSize, bool isBold)
        {
            return IsLargeText(pointSize, isBold) ? LargeText : NormalText;
        }
    }
}
