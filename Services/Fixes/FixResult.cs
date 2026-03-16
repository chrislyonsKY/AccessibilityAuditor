namespace AccessibilityAuditor.Services.Fixes
{
    /// <summary>
    /// Immutable outcome of a fix strategy execution.
    /// Created by fix strategies; never mutated downstream.
    /// </summary>
    /// <param name="Status">Whether the fix was applied, suggested for review, or failed.</param>
    /// <param name="Summary">Short human-readable description of what happened or why it failed.</param>
    /// <param name="SuggestedContent">
    /// For LLM strategies: the AI-generated suggestion awaiting user review.
    /// For deterministic color fixes: the corrected hex color.
    /// <c>null</c> when not applicable.
    /// </param>
    public record FixResult(
        FixStatus Status,
        string Summary,
        string? SuggestedContent = null
    );

    /// <summary>Outcome status for a fix attempt.</summary>
    public enum FixStatus
    {
        /// <summary>Fix was applied directly to the map/layout CIM element.</summary>
        Applied,

        /// <summary>
        /// Suggestion is ready for user review.
        /// Check <see cref="FixResult.SuggestedContent"/> for the draft.
        /// </summary>
        Suggested,

        /// <summary>Fix could not be applied. See <see cref="FixResult.Summary"/> for reason.</summary>
        Failed
    }
}
