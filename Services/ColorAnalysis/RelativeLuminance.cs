using System;

namespace AccessibilityAuditor.Services.ColorAnalysis
{
    /// <summary>
    /// Computes relative luminance from sRGB values per WCAG 2.1 specification.
    /// </summary>
    public static class RelativeLuminance
    {
        /// <summary>
        /// Calculates the relative luminance of an sRGB color per WCAG 2.1 §1.4.3.
        /// </summary>
        /// <param name="r">Red channel (0–255).</param>
        /// <param name="g">Green channel (0–255).</param>
        /// <param name="b">Blue channel (0–255).</param>
        /// <returns>Relative luminance in the range [0, 1].</returns>
        public static double Calculate(byte r, byte g, byte b)
        {
            double Rs = r / 255.0;
            double Gs = g / 255.0;
            double Bs = b / 255.0;

            double R = Rs <= 0.03928 ? Rs / 12.92 : Math.Pow((Rs + 0.055) / 1.055, 2.4);
            double G = Gs <= 0.03928 ? Gs / 12.92 : Math.Pow((Gs + 0.055) / 1.055, 2.4);
            double B = Bs <= 0.03928 ? Bs / 12.92 : Math.Pow((Bs + 0.055) / 1.055, 2.4);

            return 0.2126 * R + 0.7152 * G + 0.0722 * B;
        }
    }
}
