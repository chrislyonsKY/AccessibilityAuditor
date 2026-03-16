using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.Core.Rules
{
    /// <summary>
    /// Contract for a single WCAG compliance rule that can evaluate an audit context.
    /// </summary>
    public interface IComplianceRule
    {
        /// <summary>
        /// Gets the unique rule identifier (e.g., "WCAG_1_4_3_CONTRAST").
        /// </summary>
        string RuleId { get; }

        /// <summary>
        /// Gets the WCAG criterion this rule checks.
        /// </summary>
        WcagCriterion Criterion { get; }

        /// <summary>
        /// Gets a human-readable description of what this rule checks.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the target types this rule is applicable to.
        /// </summary>
        AuditTargetType[] ApplicableTargets { get; }

        /// <summary>
        /// Gets a value indicating whether this rule requires network access (e.g., Portal REST calls).
        /// </summary>
        bool RequiresNetwork { get; }

        /// <summary>
        /// Evaluates this rule against the provided audit context.
        /// </summary>
        /// <param name="context">The audit context containing target and inspection data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A read-only list of findings produced by this rule.</returns>
        Task<IReadOnlyList<Finding>> EvaluateAsync(AuditContext context, CancellationToken cancellationToken = default);
    }
}
