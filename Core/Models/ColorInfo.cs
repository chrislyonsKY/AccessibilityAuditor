using System;

namespace AccessibilityAuditor.Core.Models
{
    /// <summary>
    /// Represents a color value with RGB components and optional metadata.
    /// All colors are normalized to sRGB for WCAG calculations.
    /// </summary>
    public sealed class ColorInfo
    {
        /// <summary>Gets the red channel (0–255).</summary>
        public byte R { get; }

        /// <summary>Gets the green channel (0–255).</summary>
        public byte G { get; }

        /// <summary>Gets the blue channel (0–255).</summary>
        public byte B { get; }

        /// <summary>Gets the alpha channel (0–255, where 255 is fully opaque).</summary>
        public byte A { get; }

        /// <summary>
        /// Gets the hex representation of this color (e.g., "#FF8800").
        /// </summary>
        public string Hex => $"#{R:X2}{G:X2}{B:X2}";

        /// <summary>
        /// Initializes a new <see cref="ColorInfo"/> with the specified RGBA values.
        /// </summary>
        public ColorInfo(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Composites this color over the specified background using alpha blending.
        /// </summary>
        /// <param name="background">The background color to composite over.</param>
        /// <returns>A new fully-opaque <see cref="ColorInfo"/> representing the composited result.</returns>
        public ColorInfo CompositeOver(ColorInfo background)
        {
            if (background is null) throw new ArgumentNullException(nameof(background));

            double alpha = A / 255.0;
            double invAlpha = 1.0 - alpha;

            byte r = (byte)Math.Clamp(R * alpha + background.R * invAlpha, 0, 255);
            byte g = (byte)Math.Clamp(G * alpha + background.G * invAlpha, 0, 255);
            byte b = (byte)Math.Clamp(B * alpha + background.B * invAlpha, 0, 255);

            return new ColorInfo(r, g, b);
        }

        /// <inheritdoc />
        public override string ToString() => A == 255 ? Hex : $"{Hex} (alpha={A})";

        /// <inheritdoc />
        public override bool Equals(object? obj) =>
            obj is ColorInfo other && R == other.R && G == other.G && B == other.B && A == other.A;

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(R, G, B, A);
    }
}
