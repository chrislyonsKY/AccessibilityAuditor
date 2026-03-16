using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AccessibilityAuditor.Core.Constants;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.Services.PortalInspector
{
    /// <summary>
    /// Validates pop-up HTML content for accessibility issues including
    /// unclosed tags, images without alt text, tables without headers,
    /// and missing field labels. Runs on a background thread.
    /// </summary>
    public sealed class PopupChecker
    {
        /// <summary>
        /// Checks all pop-up configurations in the audit context for accessibility issues.
        /// </summary>
        /// <param name="context">The audit context containing popup data.</param>
        /// <returns>A list of findings from pop-up inspection.</returns>
        public IReadOnlyList<Finding> CheckPopups(AuditContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            var findings = new List<Finding>();

            foreach (var popup in context.Popups)
            {
                CheckPopupHtml(popup, findings);
                CheckFieldLabels(popup, findings);
            }

            return findings;
        }

        private static void CheckPopupHtml(PopupInfo popup, List<Finding> findings)
        {
            if (!popup.HasCustomHtml || string.IsNullOrWhiteSpace(popup.DescriptionHtml))
                return;

            string html = popup.DescriptionHtml;
            string element = $"Pop-up HTML on layer '{popup.LayerName}'";

            // WCAG 4.1.1 — Check for images without alt attributes
            if (Regex.IsMatch(html, @"<img\b(?![^>]*\balt\s*=)[^>]*>", RegexOptions.IgnoreCase))
            {
                findings.Add(new Finding
                {
                    RuleId = "WCAG_4_1_1_PARSING",
                    Criterion = WcagCriteria.Parsing,
                    Severity = FindingSeverity.Fail,
                    Element = element,
                    Detail = "Pop-up contains <img> tags without alt attributes. Screen readers cannot describe these images.",
                    Remediation = "Add alt=\"description\" to all <img> tags, or alt=\"\" for decorative images."
                });
            }

            // WCAG 4.1.1 — Check for tables without header cells
            if (Regex.IsMatch(html, @"<table\b", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(html, @"<th\b", RegexOptions.IgnoreCase))
            {
                findings.Add(new Finding
                {
                    RuleId = "WCAG_4_1_1_PARSING",
                    Criterion = WcagCriteria.Parsing,
                    Severity = FindingSeverity.Warning,
                    Element = element,
                    Detail = "Pop-up contains a <table> without <th> header cells. Screen readers cannot convey table structure.",
                    Remediation = "Add <th> elements to identify column and/or row headers in the table."
                });
            }

            // WCAG 4.1.1 — Check for deprecated or problematic HTML
            if (Regex.IsMatch(html, @"<(font|center|marquee)\b", RegexOptions.IgnoreCase))
            {
                findings.Add(new Finding
                {
                    RuleId = "WCAG_4_1_1_PARSING",
                    Criterion = WcagCriteria.Parsing,
                    Severity = FindingSeverity.Warning,
                    Element = element,
                    Detail = "Pop-up uses deprecated HTML elements (font, center, or marquee). These may not be parsed correctly by assistive technology.",
                    Remediation = "Replace deprecated elements with modern CSS-styled alternatives."
                });
            }

            // WCAG 1.1.1 — Check for inline styles with background-image (images without alt)
            if (Regex.IsMatch(html, @"background-image\s*:", RegexOptions.IgnoreCase))
            {
                findings.Add(new Finding
                {
                    RuleId = "WCAG_1_1_1_ALT_TEXT",
                    Criterion = WcagCriteria.NonTextContent,
                    Severity = FindingSeverity.ManualReview,
                    Element = element,
                    Detail = "Pop-up uses CSS background-image which cannot have alt text. If the image conveys information, it needs a text alternative.",
                    Remediation = "If the background image is informational, use an <img> tag with alt text instead."
                });
            }
        }

        private static void CheckFieldLabels(PopupInfo popup, List<Finding> findings)
        {
            // Check if visible fields have meaningful labels
            for (int i = 0; i < popup.FieldNames.Count; i++)
            {
                string fieldName = popup.FieldNames[i];
                string? label = i < popup.FieldLabels.Count ? popup.FieldLabels[i] : null;

                // If the label is the same as the raw field name (which is often UPPER_SNAKE_CASE),
                // it's likely not a user-friendly label
                if (string.IsNullOrWhiteSpace(label) || label == fieldName)
                {
                    // Only flag if the field name looks like a system/database name
                    if (LooksLikeSystemFieldName(fieldName))
                    {
                        findings.Add(new Finding
                        {
                            RuleId = "WCAG_3_3_2_LABELS",
                            Criterion = WcagCriteria.LabelsOrInstructions,
                            Severity = FindingSeverity.Warning,
                            Element = $"Field '{fieldName}' in pop-up on layer '{popup.LayerName}'",
                            Detail = $"Field '{fieldName}' has no user-friendly label. Database column names are not meaningful to end users.",
                            Remediation = $"Set a descriptive label for field '{fieldName}' in the pop-up configuration (e.g., '{HumanizeFieldName(fieldName)}')."
                        });
                    }
                }
            }
        }

        private static bool LooksLikeSystemFieldName(string name)
        {
            // Matches: UPPER_CASE, starts with underscore, contains double underscore, or ALL CAPS
            return Regex.IsMatch(name, @"^[A-Z_][A-Z0-9_]*$") ||
                   name.StartsWith("_") ||
                   name.Contains("__");
        }

        private static string HumanizeFieldName(string name)
        {
            // Simple: PERMIT_NUMBER ? Permit Number
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(name.Replace('_', ' ').ToLowerInvariant());
        }
    }
}
