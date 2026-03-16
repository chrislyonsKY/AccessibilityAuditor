using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Core.Rules;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.Services.RuleEngine
{
    /// <summary>
    /// Executes compliance rules with error isolation, timing, and cancellation support.
    /// A rule throwing an exception produces an <see cref="FindingSeverity.Error"/> finding
    /// without halting execution of other rules.
    /// </summary>
    public sealed class RuleExecutor
    {
        /// <summary>
        /// Executes a single compliance rule and returns the result with timing data.
        /// Exceptions are caught and converted to Error findings.
        /// </summary>
        /// <param name="rule">The rule to execute.</param>
        /// <param name="context">The audit context.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="RuleResult"/> containing findings and execution metadata.</returns>
        public async Task<RuleResult> ExecuteRuleAsync(
            IComplianceRule rule,
            AuditContext context,
            CancellationToken cancellationToken = default)
        {
            if (rule is null) throw new ArgumentNullException(nameof(rule));
            if (context is null) throw new ArgumentNullException(nameof(context));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var findings = await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                return new RuleResult
                {
                    RuleId = rule.RuleId,
                    Findings = findings,
                    Elapsed = stopwatch.Elapsed,
                    Succeeded = true
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                throw; // Let cancellation propagate
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var errorFinding = new Finding
                {
                    RuleId = rule.RuleId,
                    Criterion = rule.Criterion,
                    Severity = FindingSeverity.Error,
                    Element = rule.Description,
                    Detail = $"Rule execution failed: {ex.Message}"
                };

                return new RuleResult
                {
                    RuleId = rule.RuleId,
                    Findings = new[] { errorFinding },
                    Elapsed = stopwatch.Elapsed,
                    Succeeded = false,
                    Error = ex
                };
            }
        }

        /// <summary>
        /// Executes all provided rules sequentially with error isolation.
        /// </summary>
        /// <param name="rules">The rules to execute.</param>
        /// <param name="context">The audit context.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A list of all rule results.</returns>
        public async Task<IReadOnlyList<RuleResult>> ExecuteAllAsync(
            IReadOnlyList<IComplianceRule> rules,
            AuditContext context,
            CancellationToken cancellationToken = default)
        {
            if (rules is null) throw new ArgumentNullException(nameof(rules));

            var results = new List<RuleResult>(rules.Count);

            foreach (var rule in rules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await ExecuteRuleAsync(rule, context, cancellationToken).ConfigureAwait(false);
                results.Add(result);
            }

            return results;
        }
    }
}
