using System;
using System.Collections.Generic;
using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.Orchestration
{
    /// <summary>
    /// Aggregate result of a complete accessibility audit.
    /// </summary>
    public sealed class AuditResult
    {
        /// <summary>Gets or sets the time the audit started.</summary>
        public DateTime StartedAt { get; set; }

        /// <summary>Gets or sets the time the audit completed.</summary>
        public DateTime CompletedAt { get; set; }

        /// <summary>Gets or sets the audit target that was scanned.</summary>
        public AuditTarget? Target { get; set; }

        /// <summary>Gets the list of all findings from the audit.</summary>
        public List<Finding> Findings { get; } = new();

        /// <summary>Gets or sets the computed score card.</summary>
        public ScoreCard? Score { get; set; }

        /// <summary>Gets the elapsed duration of the audit.</summary>
        public TimeSpan Elapsed => CompletedAt - StartedAt;
    }
}
