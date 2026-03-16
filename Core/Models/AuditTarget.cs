namespace AccessibilityAuditor.Core.Models
{
    /// <summary>
    /// Specifies the type of target being audited.
    /// </summary>
    public enum AuditTargetType
    {
        /// <summary>An ArcGIS Pro map view.</summary>
        Map,

        /// <summary>An ArcGIS Pro layout.</summary>
        Layout,

        /// <summary>A portal web map item.</summary>
        WebMap,

        /// <summary>An Experience Builder application.</summary>
        ExperienceBuilder,

        /// <summary>A map package (.mpkx).</summary>
        MapPackage
    }

    /// <summary>
    /// Describes the target of an accessibility audit.
    /// </summary>
    public sealed class AuditTarget
    {
        /// <summary>
        /// Gets or sets the type of target being audited.
        /// </summary>
        public AuditTargetType TargetType { get; set; }

        /// <summary>
        /// Gets or sets the display name of the target (e.g., map name, layout name, portal item title).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional identifier (URI, portal item ID, file path).
        /// </summary>
        public string? Identifier { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"{TargetType}: {Name}";
    }
}
