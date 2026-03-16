using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AccessibilityAuditor.Core.Constants;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Core.Rules;
using AccessibilityAuditor.Orchestration;
using AccessibilityAuditor.Services.ColorAnalysis;
using AccessibilityAuditor.Services.RuleEngine;

namespace AccessibilityAuditor.Rules
{
    /// <summary>
    /// WCAG 1.4.3 — Checks that label/text colors meet minimum contrast ratio
    /// requirements against their estimated background color.
    /// </summary>
    public sealed class TextContrastRule : IComplianceRule
    {
        /// <inheritdoc />
        public string RuleId => "WCAG_1_4_3_CONTRAST";

        /// <inheritdoc />
        public WcagCriterion Criterion => WcagCriteria.ContrastMinimum;

        /// <inheritdoc />
        public string Description => "Text and labels have sufficient contrast against background";

        /// <inheritdoc />
        public AuditTargetType[] ApplicableTargets => new[]
        {
            AuditTargetType.Map, AuditTargetType.Layout,
            AuditTargetType.WebMap, AuditTargetType.MapPackage
        };

        /// <inheritdoc />
        public bool RequiresNetwork => false;

        /// <inheritdoc />
        public Task<IReadOnlyList<Finding>> EvaluateAsync(AuditContext context, CancellationToken cancellationToken = default)
        {
            var findings = new List<Finding>();
            double warningMargin = context.Settings?.ContrastWarningMargin ?? ContrastThresholds.WarningMargin;

            // Check label classes
            foreach (var label in context.LabelClasses)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!label.IsVisible) continue;

                // If label has a halo, check halo contrast instead (halo guarantees contrast boundary)
                ColorInfo effectiveBg = label.HaloColor ?? label.EstimatedBackgroundColor;
                bool usingHalo = label.HaloColor is not null;

                double ratio = ContrastCalculator.Calculate(label.ForegroundColor, effectiveBg);
                double threshold = ContrastThresholds.GetThreshold(label.FontSize, label.IsBold);
                bool isLargeText = ContrastThresholds.IsLargeText(label.FontSize, label.IsBold);

                FindingSeverity severity;
                if (ratio >= threshold)
                {
                    severity = (ratio < threshold + warningMargin)
                        ? FindingSeverity.Warning
                        : FindingSeverity.Pass;
                }
                else
                {
                    severity = FindingSeverity.Fail;
                }

                // If background is heterogeneous (imagery) and no halo, downgrade to ManualReview
                // because the estimated background color may not represent the actual label placement
                if (!usingHalo && context.IsHeterogeneousBackground && severity == FindingSeverity.Pass)
                {
                    severity = FindingSeverity.ManualReview;
                }

                string bgNote = context.IsHeterogeneousBackground && !usingHalo
                    ? " (imagery background — contrast varies by location)"
                    : string.Empty;

                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = severity,
                    Element = $"Label class '{label.ClassName}' on layer '{label.LayerName}'",
                    LayerName = label.LayerName,
                    Detail = $"Contrast ratio {ratio:F2}:1 (required: {threshold}:1 for {(isLargeText ? "large" : "normal")} text, {label.FontSize:F1}pt {label.FontFamily}){bgNote}",
                    ForegroundColor = label.ForegroundColor,
                    BackgroundColor = effectiveBg,
                    ContrastRatio = ratio,
                    Remediation = severity == FindingSeverity.Fail
                        ? RemediationEngine.SuggestContrastFix(label.ForegroundColor, effectiveBg, threshold)
                        : severity == FindingSeverity.ManualReview
                            ? "Background varies across the map (imagery basemap). Verify label contrast at key locations, or add a halo effect to guarantee readability."
                            : null
                });
            }

            // Check layout text elements
            foreach (var element in context.LayoutElements)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (element.TextColor is null || element.FontSize is null) continue;
                if (element.ElementType is not ("TextElement" or "ParagraphTextElement")) continue;

                var bg = element.BackgroundColor ?? context.DefaultBackgroundColor;
                double ratio = ContrastCalculator.Calculate(element.TextColor, bg);
                double threshold = ContrastThresholds.GetThreshold(element.FontSize.Value, element.IsBold);

                FindingSeverity severity;
                if (ratio >= threshold)
                {
                    severity = (ratio < threshold + warningMargin)
                        ? FindingSeverity.Warning
                        : FindingSeverity.Pass;
                }
                else
                {
                    severity = FindingSeverity.Fail;
                }

                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Criterion = Criterion,
                    Severity = severity,
                    Element = $"Text element '{element.Name}'",
                    NavigationTarget = element.Name,
                    Detail = $"Contrast ratio {ratio:F2}:1 (required: {threshold}:1, {element.FontSize.Value:F1}pt)",
                    ForegroundColor = element.TextColor,
                    BackgroundColor = bg,
                    ContrastRatio = ratio,
                    Remediation = severity == FindingSeverity.Fail
                        ? RemediationEngine.SuggestContrastFix(element.TextColor, bg, threshold)
                        : null
                });
            }

            return Task.FromResult<IReadOnlyList<Finding>>(findings);
        }
    }
}
