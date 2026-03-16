namespace AccessibilityAuditor.Core.Models
{
    /// <summary>
    /// A color from the map palette with its source context (layer name, category label).
    /// This is what differentiates our simulation from Pro's built-in pixel filter:
    /// we know what each color *means* in the map's symbology.
    /// </summary>
    public sealed class LabeledColor
    {
        public LabeledColor(ColorInfo color, string layerName, string categoryLabel, string colorRole)
        {
            Color = color;
            LayerName = layerName;
            CategoryLabel = categoryLabel;
            ColorRole = colorRole;
        }

        /// <summary>Gets the color value.</summary>
        public ColorInfo Color { get; }

        /// <summary>Gets the layer this color belongs to.</summary>
        public string LayerName { get; }

        /// <summary>Gets the symbol class/category label (e.g., "Residential", "0 - 100").</summary>
        public string CategoryLabel { get; }

        /// <summary>Gets the color role (e.g., "Fill", "Stroke", "Label").</summary>
        public string ColorRole { get; }

        /// <summary>Gets a display label combining layer, category, and role.</summary>
        public string DisplayLabel =>
            string.IsNullOrEmpty(CategoryLabel)
                ? $"{LayerName} ({ColorRole})"
                : $"{LayerName} — {CategoryLabel} ({ColorRole})";
    }
}
