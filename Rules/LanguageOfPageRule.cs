using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AccessibilityAuditor.Core.Constants;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Core.Rules;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.Rules
{
    /// <summary>
    /// WCAG 3.1.1 — Checks that the portal item has a language/culture attribute set.
    /// Applies to: WebMap, ExperienceBuilder.
    /// </summary>
    public sealed class LanguageOfPageRule : IComplianceRule
    {
        public string RuleId => "WCAG_3_1_1_LANGUAGE";
        public WcagCriterion Criterion => WcagCriteria.LanguageOfPage;
        public string Description => "Portal item or app has a language attribute set";
        public AuditTargetType[] ApplicableTargets => new[]
        {
            AuditTargetType.WebMap, AuditTargetType.ExperienceBuilder
        };
        public bool RequiresNetwork => true;

        public Task<IReadOnlyList<Finding>> EvaluateAsync(AuditContext context, CancellationToken cancellationToken = default)
        {
            var findings = new List<Finding>();

            // Check portal item culture
            if (context.PortalItem is not null)
            {
                string? culture = context.PortalItem.Culture;

                if (string.IsNullOrWhiteSpace(culture))
                {
                    findings.Add(new Finding
                    {
                        RuleId = RuleId,
                        Criterion = Criterion,
                        Severity = FindingSeverity.Fail,
                        Element = $"Portal item '{context.PortalItem.Title ?? context.PortalItem.ItemId}'",
                        Detail = "No language/culture attribute is set on this portal item. Assistive technology cannot determine the content language.",
                        Remediation = "Set the item's culture/language property in the portal item details (e.g., 'en-us' for English)."
                    });
                }
                else
                {
                    findings.Add(new Finding
                    {
                        RuleId = RuleId,
                        Criterion = Criterion,
                        Severity = FindingSeverity.Pass,
                        Element = $"Portal item '{context.PortalItem.Title ?? context.PortalItem.ItemId}'",
                        Detail = $"Language is set to '{culture}'."
                    });
                }
            }

            // Check ExB language setting
            if (context.ExperienceBuilder is not null)
            {
                if (string.IsNullOrWhiteSpace(context.ExperienceBuilder.Language))
                {
                    findings.Add(new Finding
                    {
                        RuleId = RuleId,
                        Criterion = Criterion,
                        Severity = FindingSeverity.Warning,
                        Element = $"ExB app '{context.ExperienceBuilder.Title ?? "Unknown"}'",
                        Detail = "No locale/language is configured in the Experience Builder app settings.",
                        Remediation = "Set the locale in the ExB app settings to declare the page language."
                    });
                }
            }

            return Task.FromResult<IReadOnlyList<Finding>>(findings);
        }
    }
}
