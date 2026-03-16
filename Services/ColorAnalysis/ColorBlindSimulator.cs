using System;
using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.Services.ColorAnalysis
{
    /// <summary>
    /// The type of color vision deficiency to simulate.
    /// </summary>
    public enum ColorBlindType
    {
        /// <summary>No L-cones (red-blind); ~1% of males.</summary>
        Protanopia,

        /// <summary>No M-cones (green-blind); ~1% of males.</summary>
        Deuteranopia,

        /// <summary>No S-cones (blue-blind); rare.</summary>
        Tritanopia
    }

    /// <summary>
    /// Simulates color vision deficiency using Brettel/Viénot/Mollon transformation matrices.
    /// </summary>
    public static class ColorBlindSimulator
    {
        // Protanopia matrix (Brettel/Viénot/Mollon 1997)
        private static readonly double[,] ProtanopiaMatrix =
        {
            { 0.152286,  1.052583, -0.204868 },
            { 0.114503,  0.786281,  0.099216 },
            { -0.003882, -0.048116,  1.051998 }
        };

        // Deuteranopia matrix
        private static readonly double[,] DeuteranopiaMatrix =
        {
            { 0.367322,  0.860646, -0.227968 },
            { 0.280085,  0.672501,  0.047413 },
            { -0.011820,  0.042940,  0.968881 }
        };

        // Tritanopia matrix
        private static readonly double[,] TritanopiaMatrix =
        {
            { 1.255528, -0.076749, -0.178779 },
            { -0.078411,  0.930809,  0.147602 },
            { 0.004733,  0.691367,  0.303900 }
        };

        /// <summary>
        /// Simulates how a color appears to a person with the specified color vision deficiency.
        /// </summary>
        /// <param name="color">The original sRGB color.</param>
        /// <param name="type">The type of color vision deficiency.</param>
        /// <returns>The simulated color as perceived by the deficient observer.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is <c>null</c>.</exception>
        public static ColorInfo Simulate(ColorInfo color, ColorBlindType type)
        {
            if (color is null) throw new ArgumentNullException(nameof(color));

            double[,] matrix = type switch
            {
                ColorBlindType.Protanopia => ProtanopiaMatrix,
                ColorBlindType.Deuteranopia => DeuteranopiaMatrix,
                ColorBlindType.Tritanopia => TritanopiaMatrix,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double sr = matrix[0, 0] * r + matrix[0, 1] * g + matrix[0, 2] * b;
            double sg = matrix[1, 0] * r + matrix[1, 1] * g + matrix[1, 2] * b;
            double sb = matrix[2, 0] * r + matrix[2, 1] * g + matrix[2, 2] * b;

            return new ColorInfo(
                ClampToByte(sr * 255.0),
                ClampToByte(sg * 255.0),
                ClampToByte(sb * 255.0),
                color.A);
        }

        /// <summary>
        /// Evaluates whether two colors remain distinguishable under the specified color vision deficiency.
        /// Uses Euclidean distance in sRGB space with a threshold of 40 units.
        /// </summary>
        /// <param name="color1">First color.</param>
        /// <param name="color2">Second color.</param>
        /// <param name="type">The color vision deficiency type.</param>
        /// <param name="threshold">Minimum Euclidean distance to consider distinguishable (default: 40).</param>
        /// <returns><c>true</c> if the colors remain distinguishable.</returns>
        public static bool AreDistinguishable(ColorInfo color1, ColorInfo color2, ColorBlindType type, double threshold = 40.0)
        {
            if (color1 is null) throw new ArgumentNullException(nameof(color1));
            if (color2 is null) throw new ArgumentNullException(nameof(color2));

            var sim1 = Simulate(color1, type);
            var sim2 = Simulate(color2, type);

            double distance = EuclideanDistance(sim1, sim2);
            return distance >= threshold;
        }

        private static double EuclideanDistance(ColorInfo a, ColorInfo b)
        {
            double dr = a.R - b.R;
            double dg = a.G - b.G;
            double db = a.B - b.B;
            return Math.Sqrt(dr * dr + dg * dg + db * db);
        }

        private static byte ClampToByte(double value)
        {
            return (byte)Math.Clamp(value, 0, 255);
        }
    }
}
