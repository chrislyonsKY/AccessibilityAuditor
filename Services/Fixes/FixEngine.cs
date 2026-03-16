using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.Services.Fixes
{
    /// <summary>
    /// Resolves and orchestrates fix strategies for audit findings.
    /// Prefers deterministic strategies over LLM strategies.
    /// </summary>
    public sealed class FixEngine
    {
        private readonly DeterministicFixStrategy _deterministic;
        private readonly LLMFixStrategy? _llm;

        /// <summary>Initialises the engine with available fix strategies.</summary>
        /// <param name="deterministic">The deterministic fix strategy.</param>
        /// <param name="llm">
        /// The LLM fix strategy, or <c>null</c> if no LLM provider is configured.
        /// </param>
        public FixEngine(DeterministicFixStrategy deterministic, LLMFixStrategy? llm = null)
        {
            _deterministic = deterministic;
            _llm = llm;
        }

        /// <summary>
        /// Resolves the appropriate fix strategy for the given finding.
        /// Prefers deterministic strategies over LLM strategies when available.
        /// Returns <c>null</c> if no strategy can handle this finding.
        /// </summary>
        public IFixStrategy? ResolveStrategy(Finding finding)
        {
            // Only attempt fixes for Fail or Warning findings
            if (finding.Severity is not (FindingSeverity.Fail or FindingSeverity.Warning))
                return null;

            // Prefer deterministic if the rule ID is supported
            if (DeterministicFixStrategy.SupportedRuleIds.Contains(finding.RuleId))
                return _deterministic;

            // Fall back to LLM if configured
            return _llm;
        }

        /// <summary>
        /// Applies all available deterministic fixes across the provided findings in a single pass.
        /// Skips findings with no deterministic strategy. LLM strategies are not invoked.
        /// </summary>
        public async Task<IReadOnlyList<(Finding Finding, FixResult Result)>> ApplyAllDeterministicAsync(
            IEnumerable<Finding> findings,
            CancellationToken ct)
        {
            var results = new List<(Finding, FixResult)>();

            foreach (var finding in findings)
            {
                ct.ThrowIfCancellationRequested();

                if (!DeterministicFixStrategy.SupportedRuleIds.Contains(finding.RuleId))
                    continue;

                if (finding.Severity is not (FindingSeverity.Fail or FindingSeverity.Warning))
                    continue;

                try
                {
                    var result = await _deterministic.ApplyFixAsync(finding, ct);
                    results.Add((finding, result));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fix failed for {finding.RuleId}: {ex.Message}");
                    results.Add((finding,
                        new FixResult(FixStatus.Failed, $"Unexpected error: {ex.Message}")));
                }
            }

            return results;
        }
    }
}
