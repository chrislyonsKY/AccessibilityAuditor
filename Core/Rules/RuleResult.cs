using System;
using System.Collections.Generic;
using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.Core.Rules
{
    /// <summary>
    /// Encapsulates the result of evaluating a single compliance rule, including timing and error data.
    /// </summary>
    public sealed class RuleResult
    {
        /// <summary>Gets the rule identifier.</summary>
        public string RuleId { get; init; } = string.Empty;

        /// <summary>Gets the findings produced by the rule.</summary>
        public IReadOnlyList<Finding> Findings { get; init; } = Array.Empty<Finding>();

        /// <summary>Gets the elapsed time for rule execution.</summary>
        public TimeSpan Elapsed { get; init; }

        /// <summary>Gets a value indicating whether the rule executed successfully.</summary>
        public bool Succeeded { get; init; }

        /// <summary>Gets the exception, if the rule failed.</summary>
        public Exception? Error { get; init; }
    }
}
