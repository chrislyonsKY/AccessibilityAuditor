using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AccessibilityAuditor.Core.Constants;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Core.Rules;
using AccessibilityAuditor.Orchestration;
using AccessibilityAuditor.Services.PortalInspector;

namespace AccessibilityAuditor.Rules
{
    /// <summary>
    /// WCAG 4.1.1 + 2.4.6 + 3.3.2 — Checks pop-up HTML validity, headings, and field labels.
    /// Delegates to <see cref="PopupChecker"/> for the actual inspection.
    /// Applies to: WebMap.
    /// </summary>
    public sealed class PopupAccessibilityRule : IComplianceRule
    {
        private readonly PopupChecker _checker = new();

        public string RuleId => "WCAG_4_1_1_POPUP";
        public WcagCriterion Criterion => WcagCriteria.Parsing;
        public string Description => "Pop-up content uses valid HTML with alt text, table headers, and meaningful field labels";
        public AuditTargetType[] ApplicableTargets => new[]
        {
            AuditTargetType.WebMap
        };
        public bool RequiresNetwork => true;

        public Task<IReadOnlyList<Finding>> EvaluateAsync(AuditContext context, CancellationToken cancellationToken = default)
        {
            if (context.Popups.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<Finding>>(new[]
                {
                    new Finding
                    {
                        RuleId = RuleId,
                        Criterion = Criterion,
                        Severity = FindingSeverity.ManualReview,
                        Element = "Web map pop-ups",
                        Detail = "No pop-up configurations found in this web map. If layers are interactive, they should have configured pop-ups.",
                        Remediation = "Configure pop-ups for operational layers so users can access feature details."
                    }
                });
            }

            return Task.FromResult(_checker.CheckPopups(context));
        }
    }
}
