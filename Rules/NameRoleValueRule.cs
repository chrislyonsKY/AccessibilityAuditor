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
    /// WCAG 4.1.2 — Checks that user interface components have programmatically
    /// determinable names and roles. Evaluates ExB widget accessibility and
    /// interactive elements in pop-up HTML.
    /// </summary>
    public sealed class NameRoleValueRule : IComplianceRule
    {
        /// <inheritdoc />
        public string RuleId => "WCAG_4_1_2_NAME_ROLE";

        /// <inheritdoc />
        public WcagCriterion Criterion => WcagCriteria.NameRoleValue;

        /// <inheritdoc />
        public string Description => "User interface components have programmatically determinable names and roles";

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

            CheckExBWidgetAccessibility(context, findings, cancellationToken);
            CheckPopupInteractiveElements(context, findings, cancellationToken);

            if (findings.Count == 0)
            {
                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = FindingSeverity.Pass,
                    Element = "Name, Role, Value",
                    Detail = "All user interface components have programmatically determinable names and roles."
                });
            }

            return Task.FromResult<IReadOnlyList<Finding>>(findings);
        }

        private void CheckExBWidgetAccessibility(AuditContext context, List<Finding> findings, CancellationToken ct)
        {
            if (context.ExperienceBuilder is null) return;

            foreach (var widget in context.ExperienceBuilder.Widgets)
            {
                ct.ThrowIfCancellationRequested();

                // Widgets that are interactive (search, filter, edit, share) need accessible names
                if (IsInteractiveWidgetType(widget.WidgetType) && !widget.HasLabel)
                {
                    findings.Add(new Finding
                    {
                        RuleId = RuleId,
                        Criterion = Criterion,
                        Severity = FindingSeverity.Fail,
                        Element = $"ExB widget '{widget.WidgetId}' ({widget.WidgetType})",
                        Detail = $"Interactive widget '{widget.WidgetId}' of type '{widget.WidgetType}' " +
                                 "has no accessible name. Screen readers cannot convey the purpose of this control.",
                        Remediation = "Set an accessible label for this widget in the Experience Builder configuration. " +
                                      "This label should describe what the widget does (e.g., 'Search for permits')."
                    });
                }
            }
        }

        private void CheckPopupInteractiveElements(AuditContext context, List<Finding> findings, CancellationToken ct)
        {
            foreach (var popup in context.Popups)
            {
                ct.ThrowIfCancellationRequested();

                if (!popup.HasCustomHtml || string.IsNullOrWhiteSpace(popup.DescriptionHtml))
                    continue;

                string html = popup.DescriptionHtml;
                string element = $"Pop-up HTML on layer '{popup.LayerName}'";

                // Check for links without accessible text
                if (Regex.IsMatch(html, @"<a\b[^>]*>\s*<img\b[^>]*>\s*</a>", RegexOptions.IgnoreCase))
                {
                    // Link wrapping an image — needs either alt on img or aria-label on link
                    if (!Regex.IsMatch(html, @"<a\b[^>]*aria-label\s*=", RegexOptions.IgnoreCase))
                    {
                        findings.Add(new Finding
                        {
                            RuleId = RuleId,
                            Criterion = Criterion,
                            Severity = FindingSeverity.Warning,
                            Element = element,
                            Detail = "Pop-up contains a link that wraps an image without an accessible name. " +
                                     "Screen readers may announce the link URL instead of a meaningful description.",
                            Remediation = "Add aria-label to the <a> element, or ensure the <img> has descriptive alt text."
                        });
                    }
                }

                // Check for buttons/inputs without labels
                if (Regex.IsMatch(html, @"<(button|input)\b", RegexOptions.IgnoreCase))
                {
                    // Check for buttons without text content or aria-label
                    if (Regex.IsMatch(html, @"<button\b[^>]*>\s*</button>", RegexOptions.IgnoreCase))
                    {
                        findings.Add(new Finding
                        {
                            RuleId = RuleId,
                            Criterion = Criterion,
                            Severity = FindingSeverity.Fail,
                            Element = element,
                            Detail = "Pop-up contains empty <button> elements. Buttons must have text content or an aria-label.",
                            Remediation = "Add descriptive text content to <button> elements or set aria-label=\"description\"."
                        });
                    }

                    // Check for inputs without associated labels
                    if (Regex.IsMatch(html, @"<input\b", RegexOptions.IgnoreCase) &&
                        !Regex.IsMatch(html, @"<label\b", RegexOptions.IgnoreCase) &&
                        !Regex.IsMatch(html, @"aria-label\s*=", RegexOptions.IgnoreCase))
                    {
                        findings.Add(new Finding
                        {
                            RuleId = RuleId,
                            Criterion = Criterion,
                            Severity = FindingSeverity.Fail,
                            Element = element,
                            Detail = "Pop-up contains <input> elements without associated <label> elements or aria-label attributes.",
                            Remediation = "Associate each input with a <label for=\"id\"> element, or add aria-label=\"description\" to the input."
                        });
                    }
                }
            }
        }

        private static bool IsInteractiveWidgetType(string? widgetType)
        {
            if (string.IsNullOrWhiteSpace(widgetType)) return false;

            var lower = widgetType.ToLowerInvariant();
            return lower is "search" or "filter" or "edit" or "share" or "select"
                or "query" or "draw" or "print" or "directions" or "measurement"
                or "coordinate-conversion" or "screening" or "near-me"
                or "smart-editor" or "batch-attribute-editor";
        }
    }
}
