namespace AccessibilityAuditor.Core.Models
{
    /// <summary>
    /// Severity levels for accessibility audit findings.
    /// </summary>
    public enum FindingSeverity
    {
        /// <summary>Criterion met successfully.</summary>
        Pass = 0,

        /// <summary>Borderline or best-practice issue; not a hard failure.</summary>
        Warning = 1,

        /// <summary>Criterion clearly not met; measurable threshold violated.</summary>
        Fail = 2,

        /// <summary>Cannot be determined automatically; human judgment required.</summary>
        ManualReview = 3,

        /// <summary>Rule execution failed due to an internal error.</summary>
        Error = 4
    }
}
