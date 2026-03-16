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
    /// WCAG 1.1.1 — Checks that non-text content (layout elements, map frames)
    /// has text alternatives (descriptions/alt text).
    /// </summary>
    public sealed class AltTextRule : IComplianceRule
    {
        /// <inheritdoc />
        public string RuleId => "WCAG_1_1_1_ALT_TEXT";

        /// <inheritdoc />
        public WcagCriterion Criterion => WcagCriteria.NonTextContent;

        /// <inheritdoc />
        public string Description => "Non-text content has text alternatives";

        /// <inheritdoc />
        public AuditTargetType[] ApplicableTargets => new[]
        {
            AuditTargetType.Layout, AuditTargetType.WebMap,
            AuditTargetType.MapPackage
        };

        /// <inheritdoc />
        public bool RequiresNetwork => false;

        // Element types that should have descriptions
        private static readonly HashSet<string> RequiresDescription = new()
        {
            "MapFrame", "GraphicElement", "PictureElement", "Legend", "ScaleBar",
            "NorthArrow", "TableFrame", "ChartFrame"
        };

        /// <inheritdoc />
        public Task<IReadOnlyList<Finding>> EvaluateAsync(AuditContext context, CancellationToken cancellationToken = default)
        {
            var findings = new List<Finding>();

            foreach (var element in context.LayoutElements)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Text elements are themselves text — they don't need alt text
                if (element.ElementType is "TextElement" or "ParagraphTextElement")
                    continue;

                bool needsDescription = RequiresDescription.Contains(element.ElementType);
                if (!needsDescription) continue;

                bool hasDescription = !string.IsNullOrWhiteSpace(element.Description);

                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = hasDescription ? FindingSeverity.Pass : FindingSeverity.Fail,
                    Element = $"{element.ElementType} '{element.Name}'",
                    NavigationTarget = element.Name,
                    Detail = hasDescription
                        ? $"Element has description: \"{Truncate(element.Description!, 80)}\""
                        : $"Element '{element.Name}' ({element.ElementType}) has no description or alt text.",
                    Remediation = hasDescription
                        ? null
                        : RemediationEngine.SuggestAltTextFix(element.ElementType, element.Name)
                });
            }

            // Check map description
            if (context.Target.TargetType == AuditTargetType.Map ||
                context.Target.TargetType == AuditTargetType.MapPackage)
            {
                bool hasMapDesc = !string.IsNullOrWhiteSpace(context.MapDescription);

                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = hasMapDesc ? FindingSeverity.Pass : FindingSeverity.Warning,
                    Element = $"Map '{context.MapTitle ?? "Untitled"}'",
                    Detail = hasMapDesc
                        ? "Map has a description."
                        : "Map has no description. Adding a description helps with accessibility metadata.",
                    Remediation = hasMapDesc
                        ? null
                        : "Add a map description via Map Properties > General > Description. " +
                          "Describe the geographic extent, subject matter, and key data layers."
                });
            }

            return Task.FromResult<IReadOnlyList<Finding>>(findings);
        }

        private static string Truncate(string text, int maxLength)
        {
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }
    }
}
