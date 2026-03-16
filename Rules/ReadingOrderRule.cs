using System;
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
    /// WCAG 1.3.1 / 1.3.2 � Checks that layout elements follow a logical reading order
    /// (top-to-bottom, left-to-right) and that the CIM z-order matches the spatial sequence.
    /// </summary>
    public sealed class ReadingOrderRule : IComplianceRule
    {
        /// <inheritdoc />
        public string RuleId => "WCAG_1_3_1_STRUCTURE";

        /// <inheritdoc />
        public WcagCriterion Criterion => WcagCriteria.InfoAndRelationships;

        /// <inheritdoc />
        public string Description => "Layout elements follow a logical reading order";

        /// <inheritdoc />
        public AuditTargetType[] ApplicableTargets => new[]
        {
            AuditTargetType.Layout
        };

        /// <inheritdoc />
        public bool RequiresNetwork => false;

        /// <summary>
        /// The Y-distance threshold (in page units) below which two elements are considered
        /// to be on the same visual row. Defaults to 0.5 inches.
        /// </summary>
        internal double RowTolerance { get; set; } = 0.5;

        /// <inheritdoc />
        public Task<IReadOnlyList<Finding>> EvaluateAsync(AuditContext context, CancellationToken cancellationToken = default)
        {
            var findings = new List<Finding>();

            var elements = context.LayoutElements;
            if (elements.Count < 2)
            {
                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = FindingSeverity.Pass,
                    Element = "Layout reading order",
                    Detail = elements.Count == 0
                        ? "No layout elements to evaluate."
                        : "Single element layout � reading order is trivial."
                });
                return Task.FromResult<IReadOnlyList<Finding>>(findings);
            }

            // Build the expected reading order: top-to-bottom, then left-to-right
            // In layout coordinates, Y increases upward, so higher Y = higher on page
            var spatialOrder = elements
                .OrderByDescending(e => e.Y)   // top first (higher Y)
                .ThenBy(e => e.X)              // left first
                .ToList();

            // Compare CIM z-order (SortOrder) to spatial order
            var cimOrder = elements
                .OrderBy(e => e.SortOrder)
                .ToList();

            int mismatches = 0;
            var mismatchedElements = new List<string>();

            for (int i = 0; i < spatialOrder.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (i >= cimOrder.Count) break;

                // Allow tolerance: elements at roughly the same Y are in the same "row"
                // and their left-to-right order matters
                int spatialIndex = FindIndexInList(cimOrder, spatialOrder[i]);
                int cimIndex = i;

                // Flag if the element is out of order
                if (Math.Abs(spatialIndex - cimIndex) >= 1)
                {
                    mismatches++;
                    if (mismatchedElements.Count < 5)
                    {
                        mismatchedElements.Add($"'{spatialOrder[i].Name}' (spatial position {i + 1}, z-order {spatialIndex + 1})");
                    }
                }
            }

            if (mismatches == 0)
            {
                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = FindingSeverity.Pass,
                    Element = "Layout reading order",
                    Detail = $"All {elements.Count} elements follow a logical top-to-bottom, left-to-right reading order."
                });
            }
            else
            {
                string detail = $"{mismatches} of {elements.Count} elements have z-order that doesn't match their visual position.";
                if (mismatchedElements.Count > 0)
                {
                    detail += " Out-of-order elements: " + string.Join("; ", mismatchedElements);
                    if (mismatches > mismatchedElements.Count)
                        detail += $"; and {mismatches - mismatchedElements.Count} more";
                }

                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = mismatches > elements.Count / 2
                        ? FindingSeverity.Fail
                        : FindingSeverity.Warning,
                    Element = "Layout reading order",
                    Detail = detail,
                    Remediation = "Reorder layout elements so their z-order (draw order) matches the intended reading sequence. " +
                                  "In ArcGIS Pro, use the Contents pane to drag elements into the correct order. " +
                                  "Screen readers and PDF export follow the element z-order, not visual position."
                });
            }

            // Also check for the 1.3.2 Meaningful Sequence criterion specifically
            CheckMeaningfulSequence(elements, findings);

            return Task.FromResult<IReadOnlyList<Finding>>(findings);
        }

        private void CheckMeaningfulSequence(List<LayoutElementInfo> elements, List<Finding> findings)
        {
            // Check if there are text elements that appear to be title/subtitle pairs
            // where the subtitle is before the title in z-order
            var textElements = elements
                .Where(e => e.ElementType is "TextElement" or "ParagraphTextElement" && e.FontSize.HasValue)
                .ToList();

            if (textElements.Count < 2) return;

            // Sort by visual position (top-to-bottom)
            var sorted = textElements.OrderByDescending(e => e.Y).ToList();

            for (int i = 0; i < sorted.Count - 1; i++)
            {
                var upper = sorted[i];
                var lower = sorted[i + 1];

                // If the upper element has larger font (likely a heading) but comes AFTER
                // the lower element in z-order, the sequence is wrong
                if (upper.FontSize > lower.FontSize && upper.SortOrder > lower.SortOrder)
                {
                    findings.Add(new Finding
                    {
                        RuleId = "WCAG_1_3_2_SEQUENCE",
                        Criterion = WcagCriteria.MeaningfulSequence,
                        Severity = FindingSeverity.Warning,
                        Element = $"Text elements '{upper.Name}' and '{lower.Name}'",
                        Detail = $"'{upper.Name}' ({upper.FontSize:F0}pt) appears above '{lower.Name}' ({lower.FontSize:F0}pt) visually, " +
                                 $"but comes after it in the element order. This means a screen reader will read the subtitle before the title.",
                        Remediation = "Reorder the elements so the title precedes the subtitle in the Contents pane draw order."
                    });
                }
            }
        }

        private static int FindIndexInList(List<LayoutElementInfo> list, LayoutElementInfo target)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], target)) return i;
            }
            return -1;
        }
    }
}
