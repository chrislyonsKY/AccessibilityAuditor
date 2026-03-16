using System.Collections.Generic;
using System.Linq;
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
    /// WCAG 1.4.1 — Checks that renderers use shape or size variation,
    /// not just color, to convey information. This is a semi-automated check:
    /// unique value renderers that use only color differences are flagged for review.
    /// </summary>
    public sealed class UseOfColorRule : IComplianceRule
    {
        /// <inheritdoc />
        public string RuleId => "WCAG_1_4_1_USE_OF_COLOR";

        /// <inheritdoc />
        public WcagCriterion Criterion => WcagCriteria.UseOfColor;

        /// <inheritdoc />
        public string Description => "Information is not conveyed by color alone";

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

            foreach (var renderer in context.Renderers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Simple renderers don't use color to distinguish categories — skip
                if (renderer.RendererType == "CIMSimpleRenderer")
                    continue;

                // Only relevant for multi-class renderers
                if (renderer.SymbolClasses.Count < 2)
                    continue;

                if (renderer.UsesShapeOrSizeVariation)
                {
                    findings.Add(new Finding
                    {
                        RuleId = RuleId,
                        Criterion = Criterion,
                        Severity = FindingSeverity.Pass,
                        Element = $"Renderer on layer '{renderer.LayerName}'",
                        LayerName = renderer.LayerName,
                        Detail = $"Renderer ({renderer.RendererType}) uses shape or size variation in addition to color."
                    });
                }
                else
                {
                    // Check if colors are distinguishable under colorblind simulation
                    var colors = renderer.SymbolClasses
                        .Where(sc => sc.FillColor is not null)
                        .Select(sc => sc.FillColor!)
                        .ToList();

                    bool cbSafe = true;
                    if (colors.Count >= 2)
                    {
                        var cbResults = PaletteEvaluator.EvaluateColorBlindSafety(colors);
                        cbSafe = cbResults.All(r => r.AllDistinguishable);
                    }

                    var severity = cbSafe ? FindingSeverity.ManualReview : FindingSeverity.Fail;
                    string detail = cbSafe
                        ? $"Renderer on layer '{renderer.LayerName}' uses only color to distinguish {renderer.SymbolClasses.Count} categories. " +
                          "Colors are distinguishable under colorblind simulation, but manual review is recommended."
                        : $"Renderer on layer '{renderer.LayerName}' uses only color to distinguish {renderer.SymbolClasses.Count} categories, " +
                          "and some colors become indistinguishable under colorblind simulation.";

                    findings.Add(new Finding
                    {
                        RuleId = RuleId,
                        Criterion = Criterion,
                        Severity = severity,
                        Element = $"Renderer on layer '{renderer.LayerName}'",
                        LayerName = renderer.LayerName,
                        Detail = detail,
                        Remediation = RemediationEngine.SuggestUseOfColorFix(renderer.LayerName)
                    });
                }
            }

            return Task.FromResult<IReadOnlyList<Finding>>(findings);
        }
    }
}
