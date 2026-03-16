using System;
using System.Collections.Generic;
using System.Linq;
using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.Core.Rules
{
    /// <summary>
    /// Registry of all known compliance rules. Provides filtering by target type.
    /// </summary>
    public sealed class RuleRegistry
    {
        private readonly List<IComplianceRule> _rules = new();

        /// <summary>
        /// Gets all registered rules.
        /// </summary>
        public IReadOnlyList<IComplianceRule> AllRules => _rules.AsReadOnly();

        /// <summary>
        /// Registers a compliance rule.
        /// </summary>
        /// <param name="rule">The rule to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a rule with the same ID is already registered.</exception>
        public void Register(IComplianceRule rule)
        {
            if (rule is null) throw new ArgumentNullException(nameof(rule));

            if (_rules.Any(r => r.RuleId == rule.RuleId))
                throw new InvalidOperationException($"Rule '{rule.RuleId}' is already registered.");

            _rules.Add(rule);
        }

        /// <summary>
        /// Returns all rules applicable to the specified target type.
        /// </summary>
        /// <param name="targetType">The audit target type to filter by.</param>
        /// <returns>Rules applicable to the given target type.</returns>
        public IReadOnlyList<IComplianceRule> GetApplicableRules(AuditTargetType targetType)
        {
            return _rules
                .Where(r => r.ApplicableTargets.Contains(targetType))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Returns all rules that do not require network access.
        /// </summary>
        public IReadOnlyList<IComplianceRule> GetOfflineRules()
        {
            return _rules
                .Where(r => !r.RequiresNetwork)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets the count of registered rules.
        /// </summary>
        public int Count => _rules.Count;
    }
}
