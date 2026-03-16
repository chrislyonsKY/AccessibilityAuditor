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
    /// WCAG 1.4.5 — Detects picture/image elements in layouts that may contain
    /// rasterized text, and flags them for manual review.
    /// </summary>
    public sealed class ImagesOfTextRule : IComplianceRule
    {
        /// <inheritdoc />
        public string RuleId => "WCAG_1_4_5_IMAGES_TEXT";

        /// <inheritdoc />
        public WcagCriterion Criterion => WcagCriteria.ImagesOfText;

        /// <inheritdoc />
        public string Description => "Text is used to convey information rather than images of text";

        /// <inheritdoc />
        public AuditTargetType[] ApplicableTargets => new[]
        {
            AuditTargetType.Layout, AuditTargetType.WebMap,
            AuditTargetType.ExperienceBuilder, AuditTargetType.MapPackage
        };

        /// <inheritdoc />
        public bool RequiresNetwork => false;

        /// <inheritdoc />
        public Task<IReadOnlyList<Finding>> EvaluateAsync(AuditContext context, CancellationToken cancellationToken = default)
        {
            var findings = new List<Finding>();

            // Check layout picture elements
            CheckLayoutPictureElements(context, findings, cancellationToken);

            // Check web map popup HTML for images that might contain text
            CheckPopupImagesOfText(context, findings, cancellationToken);

            if (findings.Count == 0)
            {
                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = FindingSeverity.Pass,
                    Element = "Images of text",
                    Detail = "No picture elements detected that may contain images of text."
                });
            }

            return Task.FromResult<IReadOnlyList<Finding>>(findings);
        }

        private void CheckLayoutPictureElements(AuditContext context, List<Finding> findings, CancellationToken ct)
        {
            foreach (var element in context.LayoutElements)
            {
                ct.ThrowIfCancellationRequested();

                if (!element.IsPictureElement) continue;

                // Picture elements in a layout could be logos, diagrams, or rasterized text.
                // We can't inspect the image content, so flag for manual review.
                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = FindingSeverity.ManualReview,
                    Element = $"Picture element '{element.Name}'",
                    NavigationTarget = element.Name,
                    Detail = "This layout contains a picture/image element. If the image contains text " +
                             "(such as a title graphic, logo with text, or pre-rendered label), that text " +
                             "is not accessible to screen readers and cannot be resized.",
                    Remediation = "If the image contains text, replace it with a native ArcGIS Pro text element. " +
                                  "If the image is decorative or a logo, ensure it has a description/alt text set " +
                                  "in the element properties."
                });
            }
        }

        private void CheckPopupImagesOfText(AuditContext context, List<Finding> findings, CancellationToken ct)
        {
            foreach (var popup in context.Popups)
            {
                ct.ThrowIfCancellationRequested();

                if (!popup.HasCustomHtml || string.IsNullOrWhiteSpace(popup.DescriptionHtml))
                    continue;

                // Check for images in popup HTML that might be text-heavy
                // (we can't inspect the actual image, but we can flag large or numerous images)
                int imgCount = System.Text.RegularExpressions.Regex.Matches(
                    popup.DescriptionHtml, @"<img\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;

                if (imgCount > 0)
                {
                    findings.Add(new Finding
                    {
                        RuleId = RuleId,
                        Criterion = Criterion,
                        Severity = FindingSeverity.ManualReview,
                        Element = $"Pop-up images on layer '{popup.LayerName}'",
                        Detail = $"Pop-up contains {imgCount} image(s). Verify that none of these images contain " +
                                 "text that should be rendered as actual HTML text instead.",
                        Remediation = "If any images contain text (titles, labels, data values), replace them with " +
                                      "HTML text content. Use alt text for informational images and alt=\"\" for decorative ones."
                    });
                }
            }
        }
    }
}
