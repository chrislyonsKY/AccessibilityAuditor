namespace AccessibilityAuditor.Core.Models
{
    /// <summary>
    /// Represents a specific WCAG 2.1 success criterion.
    /// </summary>
    public sealed class WcagCriterion
    {
        /// <summary>
        /// Gets the criterion identifier (e.g., "1.4.3").
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the criterion name (e.g., "Contrast (Minimum)").
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the WCAG principle this criterion belongs to.
        /// </summary>
        public WcagPrinciple Principle { get; }

        /// <summary>
        /// Gets the WCAG conformance level (A, AA, AAA).
        /// </summary>
        public string Level { get; }

        /// <summary>
        /// Gets a brief description of what this criterion requires.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the URL to the WCAG specification for this criterion.
        /// </summary>
        public string SpecUrl { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WcagCriterion"/> class.
        /// </summary>
        public WcagCriterion(string id, string name, WcagPrinciple principle, string level, string description, string specUrl)
        {
            Id = id ?? throw new System.ArgumentNullException(nameof(id));
            Name = name ?? throw new System.ArgumentNullException(nameof(name));
            Principle = principle;
            Level = level ?? throw new System.ArgumentNullException(nameof(level));
            Description = description ?? throw new System.ArgumentNullException(nameof(description));
            SpecUrl = specUrl ?? throw new System.ArgumentNullException(nameof(specUrl));
        }

        /// <inheritdoc />
        public override string ToString() => $"WCAG {Id} {Name} (Level {Level})";
    }
}
