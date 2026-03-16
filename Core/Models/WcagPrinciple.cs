namespace AccessibilityAuditor.Core.Models
{
    /// <summary>
    /// Represents the four WCAG 2.1 principles.
    /// </summary>
    public enum WcagPrinciple
    {
        /// <summary>Information and UI components must be presentable in ways users can perceive.</summary>
        Perceivable = 1,

        /// <summary>UI components and navigation must be operable.</summary>
        Operable = 2,

        /// <summary>Information and the operation of UI must be understandable.</summary>
        Understandable = 3,

        /// <summary>Content must be robust enough to be interpreted by assistive technologies.</summary>
        Robust = 4
    }
}
