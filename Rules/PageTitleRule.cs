using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AccessibilityAuditor.Core.Constants;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Core.Rules;
using AccessibilityAuditor.Orchestration;
using AccessibilityAuditor.Services.RuleEngine;

namespace AccessibilityAuditor.Rules
{
    /// <summary>
    /// WCAG 2.4.2 — Checks that the map or layout has a meaningful,
    /// descriptive title (not empty, not a generic default).
    /// </summary>
    public sealed class PageTitleRule : IComplianceRule
    {
        /// <inheritdoc />
        public string RuleId => "WCAG_2_4_2_TITLE";

        /// <inheritdoc />
        public WcagCriterion Criterion => WcagCriteria.PageTitled;

        /// <inheritdoc />
        public string Description => "Map or layout has a meaningful, descriptive title";

        /// <inheritdoc />
        public AuditTargetType[] ApplicableTargets => new[]
        {
            AuditTargetType.Map, AuditTargetType.Layout,
            AuditTargetType.WebMap, AuditTargetType.MapPackage
        };

        /// <inheritdoc />
        public bool RequiresNetwork => false;

        // Common default/generic titles that indicate the author didn't set a real title
        private static readonly HashSet<string> GenericTitles = new(StringComparer.OrdinalIgnoreCase)
        {
            "map", "map1", "map 1", "new map", "untitled",
            "layout", "layout1", "layout 1", "new layout",
            "project", "default", "test", "copy"
        };

        /// <inheritdoc />
        public Task<IReadOnlyList<Finding>> EvaluateAsync(AuditContext context, CancellationToken cancellationToken = default)
        {
            var findings = new List<Finding>();

            // Check map title
            if (context.Target.TargetType is AuditTargetType.Map or AuditTargetType.MapPackage)
            {
                findings.Add(EvaluateTitle(context.MapTitle, "Map", context.Target.TargetType));
            }

            // Check layout title
            if (context.Target.TargetType == AuditTargetType.Layout)
            {
                findings.Add(EvaluateTitle(context.LayoutTitle, "Layout", context.Target.TargetType));
            }

            return Task.FromResult<IReadOnlyList<Finding>>(findings);
        }

        private Finding EvaluateTitle(string? title, string targetLabel, AuditTargetType targetType)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = FindingSeverity.Fail,
                    Element = targetLabel,
                    Detail = $"{targetLabel} has no title.",
                    Remediation = RemediationEngine.SuggestTitleFix(targetType)
                };
            }

            if (GenericTitles.Contains(title.Trim()))
            {
                return new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = FindingSeverity.Warning,
                    Element = $"{targetLabel} '{title}'",
                    Detail = $"{targetLabel} title '{title}' appears to be a generic default. " +
                             "A descriptive title helps users understand the content's purpose.",
                    Remediation = RemediationEngine.SuggestTitleFix(targetType)
                };
            }

            return new Finding
            {
                RuleId = RuleId,
                Criterion = Criterion,
                Severity = FindingSeverity.Pass,
                Element = $"{targetLabel} '{title}'",
                Detail = $"{targetLabel} has a descriptive title: \"{title}\"."
            };
        }
    }
}
