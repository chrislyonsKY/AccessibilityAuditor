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
    /// WCAG 1.4.11 — Checks that non-text graphical elements (symbology)
    /// have a contrast ratio of at least 3:1 against the background.
    /// </summary>
    public sealed class NonTextContrastRule : IComplianceRule
    {
        /// <inheritdoc />
        public string RuleId => "WCAG_1_4_11_NON_TEXT";

        /// <inheritdoc />
        public WcagCriterion Criterion => WcagCriteria.NonTextContrast;

        /// <inheritdoc />
        public string Description => "Graphical elements have sufficient contrast against background";

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

            foreach (var renderer in context.Renderers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var symbolClass in renderer.SymbolClasses)
                {
                    // Check fill color against background
                    if (symbolClass.FillColor is not null)
                    {
                        double ratio = ContrastCalculator.Calculate(symbolClass.FillColor, context.DefaultBackgroundColor);

                        FindingSeverity severity;
                        if (ratio >= ContrastThresholds.NonTextGraphics)
                        {
                            severity = (ratio < ContrastThresholds.NonTextGraphics + warningMargin)
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
                            Element = $"Symbol '{symbolClass.Label}' on layer '{renderer.LayerName}'",
                            LayerName = renderer.LayerName,
                            Detail = $"Fill color {symbolClass.FillColor.Hex} contrast ratio {ratio:F2}:1 against background (required: {ContrastThresholds.NonTextGraphics}:1)",
                            ForegroundColor = symbolClass.FillColor,
                            BackgroundColor = context.DefaultBackgroundColor,
                            ContrastRatio = ratio,
                            Remediation = severity == FindingSeverity.Fail
                                ? RemediationEngine.SuggestNonTextContrastFix(renderer.LayerName, symbolClass.Label, ratio)
                                : null
                        });
                    }

                    // Check stroke color against background if it's a meaningful element (lines, outlines)
                    if (symbolClass.StrokeColor is not null && symbolClass.StrokeWidth >= 1.0)
                    {
                        double strokeRatio = ContrastCalculator.Calculate(symbolClass.StrokeColor, context.DefaultBackgroundColor);

                        if (strokeRatio < ContrastThresholds.NonTextGraphics)
                        {
                            findings.Add(new Finding
                            {
                                RuleId = RuleId,
                                Criterion = Criterion,
                                Severity = FindingSeverity.Fail,
                                Element = $"Stroke on symbol '{symbolClass.Label}' on layer '{renderer.LayerName}'",
                                LayerName = renderer.LayerName,
                                Detail = $"Stroke color {symbolClass.StrokeColor.Hex} contrast ratio {strokeRatio:F2}:1 against background (required: {ContrastThresholds.NonTextGraphics}:1)",
                                ForegroundColor = symbolClass.StrokeColor,
                                BackgroundColor = context.DefaultBackgroundColor,
                                ContrastRatio = strokeRatio,
                                Remediation = RemediationEngine.SuggestNonTextContrastFix(renderer.LayerName, symbolClass.Label, strokeRatio)
                            });
                        }
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<Finding>>(findings);
        }
    }
}
