using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AccessibilityAuditor.Core.Constants;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Core.Rules;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.Rules
{
    /// <summary>
    /// WCAG 2.4.6 — Checks that headings and labels describe topic or purpose.
    /// Evaluates pop-up titles, pop-up heading structure, and ExB widget labels.
    /// </summary>
    public sealed class HeadingsAndLabelsRule : IComplianceRule
    {
        /// <inheritdoc />
        public string RuleId => "WCAG_2_4_6_HEADINGS";

        /// <inheritdoc />
        public WcagCriterion Criterion => WcagCriteria.HeadingsAndLabels;

        /// <inheritdoc />
        public string Description => "Headings and labels describe topic or purpose";

        /// <inheritdoc />
        public AuditTargetType[] ApplicableTargets => new[]
        {
            AuditTargetType.WebMap, AuditTargetType.ExperienceBuilder
        };

        /// <inheritdoc />
        public bool RequiresNetwork => false;

        /// <inheritdoc />
        public Task<IReadOnlyList<Finding>> EvaluateAsync(AuditContext context, CancellationToken cancellationToken = default)
        {
            var findings = new List<Finding>();

            CheckPopupHeadings(context, findings, cancellationToken);
            CheckExBWidgetLabels(context, findings, cancellationToken);

            if (findings.Count == 0)
            {
                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = FindingSeverity.Pass,
                    Element = "Headings and labels",
                    Detail = "All headings and labels describe their topic or purpose."
                });
            }

            return Task.FromResult<IReadOnlyList<Finding>>(findings);
        }

        private void CheckPopupHeadings(AuditContext context, List<Finding> findings, CancellationToken ct)
        {
            foreach (var popup in context.Popups)
            {
                ct.ThrowIfCancellationRequested();

                // Check popup title
                if (string.IsNullOrWhiteSpace(popup.TitleTemplate))
                {
                    findings.Add(new Finding
                    {
                        RuleId = RuleId,
                        Criterion = Criterion,
                        Severity = FindingSeverity.Fail,
                        Element = $"Pop-up on layer '{popup.LayerName}'",
                        Detail = "Pop-up has no title configured. Users of assistive technology cannot identify what this pop-up describes.",
                        Remediation = "Configure a descriptive pop-up title using field names or a custom expression."
                    });
                }
                else if (IsNonDescriptiveTitle(popup.TitleTemplate))
                {
                    findings.Add(new Finding
                    {
                        RuleId = RuleId,
                        Criterion = Criterion,
                        Severity = FindingSeverity.Warning,
                        Element = $"Pop-up on layer '{popup.LayerName}'",
                        Detail = $"Pop-up title '{popup.TitleTemplate}' uses a non-descriptive system field. This is not meaningful to users.",
                        Remediation = "Use a descriptive field like a name, address, or category instead of OBJECTID/FID."
                    });
                }

                // Check heading hierarchy in custom HTML
                if (popup.HasCustomHtml && !string.IsNullOrWhiteSpace(popup.DescriptionHtml))
                {
                    CheckHeadingHierarchy(popup, findings);
                }
            }
        }

        private static void CheckHeadingHierarchy(PopupInfo popup, List<Finding> findings)
        {
            string html = popup.DescriptionHtml!;
            string element = $"Pop-up HTML on layer '{popup.LayerName}'";

            // Check for skipped heading levels (e.g., h1 then h3 without h2)
            var headingMatches = Regex.Matches(html, @"<h(\d)\b", RegexOptions.IgnoreCase);
            if (headingMatches.Count > 1)
            {
                int previousLevel = 0;
                foreach (Match match in headingMatches)
                {
                    int level = int.Parse(match.Groups[1].Value);
                    if (previousLevel > 0 && level > previousLevel + 1)
                    {
                        findings.Add(new Finding
                        {
                            RuleId = "WCAG_2_4_6_HEADINGS",
                            Criterion = WcagCriteria.HeadingsAndLabels,
                            Severity = FindingSeverity.Warning,
                            Element = element,
                            Detail = $"Heading levels skip from <h{previousLevel}> to <h{level}>. " +
                                     "Skipped heading levels confuse screen reader users navigating by headings.",
                            Remediation = $"Use <h{previousLevel + 1}> instead of <h{level}>, or restructure the heading hierarchy to avoid skipping levels."
                        });
                        break;
                    }
                    previousLevel = level;
                }
            }

            // Check for empty heading elements
            if (Regex.IsMatch(html, @"<h[1-6]\b[^>]*>\s*</h[1-6]>", RegexOptions.IgnoreCase))
            {
                findings.Add(new Finding
                {
                    RuleId = "WCAG_2_4_6_HEADINGS",
                    Criterion = WcagCriteria.HeadingsAndLabels,
                    Severity = FindingSeverity.Fail,
                    Element = element,
                    Detail = "Pop-up contains empty heading elements. Empty headings are announced by screen readers but provide no information.",
                    Remediation = "Remove empty heading tags or add descriptive text content to them."
                });
            }
        }

        private void CheckExBWidgetLabels(AuditContext context, List<Finding> findings, CancellationToken ct)
        {
            if (context.ExperienceBuilder is null) return;

            foreach (var widget in context.ExperienceBuilder.Widgets)
            {
                ct.ThrowIfCancellationRequested();

                if (!widget.HasLabel || string.IsNullOrWhiteSpace(widget.Label))
                {
                    findings.Add(new Finding
                    {
                        RuleId = RuleId,
                        Criterion = Criterion,
                        Severity = FindingSeverity.Warning,
                        Element = $"ExB widget '{widget.WidgetId}' ({widget.WidgetType})",
                        Detail = $"Widget '{widget.WidgetId}' of type '{widget.WidgetType}' has no descriptive label. " +
                                 "Users of assistive technology cannot identify the widget's purpose.",
                        Remediation = "Set a descriptive label for this widget in the Experience Builder widget configuration."
                    });
                }
                else if (widget.Label == widget.WidgetType || widget.Label == widget.WidgetId)
                {
                    findings.Add(new Finding
                    {
                        RuleId = RuleId,
                        Criterion = Criterion,
                        Severity = FindingSeverity.Warning,
                        Element = $"ExB widget '{widget.WidgetId}' ({widget.WidgetType})",
                        Detail = $"Widget label '{widget.Label}' is the same as the widget type or ID. " +
                                 "Labels should describe the specific purpose, not the generic type.",
                        Remediation = $"Change the label from '{widget.Label}' to something descriptive " +
                                      $"(e.g., 'Search permits' instead of 'search')."
                    });
                }
            }
        }

        private static bool IsNonDescriptiveTitle(string title)
        {
            var trimmed = title.Trim();
            return trimmed is "{OBJECTID}" or "{FID}" or "{OID}" or "{OBJECTID_1}";
        }
    }
}
