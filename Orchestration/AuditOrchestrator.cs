using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Core.Rules;
using AccessibilityAuditor.Services.RuleEngine;

namespace AccessibilityAuditor.Orchestration
{
    /// <summary>
    /// Master scan controller that coordinates CIM inspection, rule execution,
    /// and score aggregation. Implements continue-on-error semantics.
    /// </summary>
    public sealed class AuditOrchestrator
    {
        private readonly RuleRegistry _rules;
        private readonly RuleExecutor _executor;

        /// <summary>
        /// Initializes a new <see cref="AuditOrchestrator"/>.
        /// </summary>
        /// <param name="rules">The registry of compliance rules.</param>
        public AuditOrchestrator(RuleRegistry rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _executor = new RuleExecutor();
        }

        /// <summary>
        /// Occurs when a rule begins execution.
        /// </summary>
        public event Action<string>? RuleStarted;

        /// <summary>
        /// Occurs when a rule completes execution.
        /// </summary>
        public event Action<RuleResult>? RuleCompleted;

        /// <summary>
        /// Runs a complete accessibility audit against a pre-populated context.
        /// The context should already have CIM data extracted via the scan pipeline.
        /// </summary>
        /// <param name="context">The audit context with extracted CIM data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An <see cref="AuditResult"/> with all findings and scores.</returns>
        public async Task<AuditResult> RunAuditAsync(AuditContext context, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            var result = new AuditResult
            {
                StartedAt = DateTime.UtcNow,
                Target = context.Target
            };

            var applicableRules = _rules.GetApplicableRules(context.Target.TargetType);

            foreach (var rule in applicableRules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                RuleStarted?.Invoke(rule.RuleId);

                var ruleResult = await _executor.ExecuteRuleAsync(rule, context, cancellationToken).ConfigureAwait(false);
                result.Findings.AddRange(ruleResult.Findings);

                RuleCompleted?.Invoke(ruleResult);

                if (!ruleResult.Succeeded)
                {
                    Debug.WriteLine($"Rule {rule.RuleId} failed: {ruleResult.Error?.Message}");
                }
            }

            result.CompletedAt = DateTime.UtcNow;

            // Calculate score from ALL findings (including Pass) for accurate compliance scoring
            result.Score = ScoreCard.Calculate(result.Findings);

            // Then filter out Pass findings from display if user disabled them
            bool includePass = context.Settings?.IncludePassFindings ?? true;
            if (!includePass)
            {
                result.Findings.RemoveAll(f => f.Severity == FindingSeverity.Pass);
            }

            return result;
        }
    }
}
