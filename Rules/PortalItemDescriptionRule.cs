using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AccessibilityAuditor.Core.Constants;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Core.Rules;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.Rules
{
    /// <summary>
    /// WCAG 1.1.1 for portal items — Checks that the portal item has a description,
    /// and that web map layers have meaningful titles.
    /// Applies to: WebMap, ExperienceBuilder.
    /// </summary>
    public sealed class PortalItemDescriptionRule : IComplianceRule
    {
        public string RuleId => "WCAG_1_1_1_PORTAL_DESC";
        public WcagCriterion Criterion => WcagCriteria.NonTextContent;
        public string Description => "Portal item has a meaningful description and layer titles";
        public AuditTargetType[] ApplicableTargets => new[]
        {
            AuditTargetType.WebMap, AuditTargetType.ExperienceBuilder
        };
        public bool RequiresNetwork => true;

        public Task<IReadOnlyList<Finding>> EvaluateAsync(AuditContext context, CancellationToken cancellationToken = default)
        {
            var findings = new List<Finding>();

            if (context.PortalItem is null)
                return Task.FromResult<IReadOnlyList<Finding>>(findings);

            var item = context.PortalItem;
            string itemLabel = item.Title ?? item.ItemId;

            // Check item description
            if (string.IsNullOrWhiteSpace(item.Description))
            {
                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = FindingSeverity.Fail,
                    Element = $"Portal item '{itemLabel}'",
                    Detail = "Item has no description. Users and assistive technology have no context about what this item contains.",
                    Remediation = "Add a description in the portal item details that explains the map's purpose and content."
                });
            }
            else
            {
                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = FindingSeverity.Pass,
                    Element = $"Portal item '{itemLabel}'",
                    Detail = $"Item has a description ({item.Description.Length} characters)."
                });
            }

            // Check item snippet
            if (string.IsNullOrWhiteSpace(item.Snippet))
            {
                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = FindingSeverity.Warning,
                    Element = $"Portal item '{itemLabel}'",
                    Detail = "Item has no snippet (short summary). The snippet appears in search results and provides quick context.",
                    Remediation = "Add a concise snippet summarizing the item's purpose."
                });
            }

            // Check web map layer titles
            foreach (var layer in context.WebMapLayers)
            {
                if (string.IsNullOrWhiteSpace(layer.Title))
                {
                    findings.Add(new Finding
                    {
                        RuleId = RuleId,
                        Criterion = Criterion,
                        Severity = FindingSeverity.Fail,
                        Element = $"Web map layer '{layer.LayerId}'",
                        Detail = "Layer has no title. Users cannot identify what this layer represents.",
                        Remediation = "Set a descriptive title for this layer in the web map configuration."
                    });
                }
            }

            // Check ExB widget labels
            if (context.ExperienceBuilder is not null)
            {
                foreach (var widget in context.ExperienceBuilder.Widgets)
                {
                    if (!widget.HasLabel)
                    {
                        findings.Add(new Finding
                        {
                            RuleId = RuleId,
                            Criterion = Criterion,
                            Severity = FindingSeverity.Warning,
                            Element = $"ExB widget '{widget.WidgetId}' (type: {widget.WidgetType ?? "unknown"})",
                            Detail = "Widget has no label. Screen readers cannot identify the widget's purpose.",
                            Remediation = "Set a descriptive label for this widget in the Experience Builder configuration."
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<Finding>>(findings);
        }
    }
}
