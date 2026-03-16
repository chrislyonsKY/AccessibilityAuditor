using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Orchestration;
using AccessibilityAuditor.Rules;

namespace AccessibilityAuditor.Tests.Rules;

/// <summary>
/// Unit tests for <see cref="ReadingOrderRule"/> covering layout element
/// z-order vs. spatial position analysis.
/// </summary>
public sealed class ReadingOrderRuleTests
{
    private readonly ReadingOrderRule _rule = new();

    [Fact]
    public async Task EmptyLayout_Pass()
    {
        var context = MakeContext();
        var findings = await _rule.EvaluateAsync(context);

        Assert.Single(findings);
        Assert.Equal(FindingSeverity.Pass, findings[0].Severity);
    }

    [Fact]
    public async Task SingleElement_Pass()
    {
        var context = MakeContext();
        context.LayoutElements.Add(MakeElement("Title", 0, 10, 0));

        var findings = await _rule.EvaluateAsync(context);

        Assert.Single(findings);
        Assert.Equal(FindingSeverity.Pass, findings[0].Severity);
    }

    [Fact]
    public async Task CorrectOrder_TopToBottom_Pass()
    {
        var context = MakeContext();
        // z-order 0 = top element (Y=10), z-order 1 = bottom element (Y=2)
        context.LayoutElements.Add(MakeElement("Title", 0, 10, 0));
        context.LayoutElements.Add(MakeElement("Body", 0, 2, 1));

        var findings = await _rule.EvaluateAsync(context);

        Assert.Contains(findings, f => f.Severity == FindingSeverity.Pass);
    }

    [Fact]
    public async Task ReversedOrder_Warning()
    {
        var context = MakeContext();
        // z-order 0 is the BOTTOM element, z-order 1 is the TOP element — wrong
        context.LayoutElements.Add(MakeElement("Body", 0, 2, 0));
        context.LayoutElements.Add(MakeElement("Title", 0, 10, 1));

        var findings = await _rule.EvaluateAsync(context);

        // The reading order finding should not be Pass
        Assert.Contains(findings, f =>
            f.RuleId == "WCAG_1_3_1_STRUCTURE" &&
            f.Severity is FindingSeverity.Warning or FindingSeverity.Fail);
    }

    [Fact]
    public async Task MeaningfulSequence_TitleAfterSubtitle_Warning()
    {
        var context = MakeContext();
        // Title (24pt) is visually above subtitle (12pt), but has higher SortOrder (comes later)
        context.LayoutElements.Add(MakeTextElement("Subtitle", 0, 5, 0, 12));
        context.LayoutElements.Add(MakeTextElement("Title", 0, 10, 1, 24));

        var findings = await _rule.EvaluateAsync(context);

        Assert.Contains(findings, f =>
            f.RuleId == "WCAG_1_3_2_SEQUENCE" &&
            f.Severity == FindingSeverity.Warning);
    }

    [Fact]
    public async Task MeaningfulSequence_CorrectOrder_NoSequenceWarning()
    {
        var context = MakeContext();
        // Title (24pt) has lower SortOrder and is visually above — correct
        context.LayoutElements.Add(MakeTextElement("Title", 0, 10, 0, 24));
        context.LayoutElements.Add(MakeTextElement("Subtitle", 0, 5, 1, 12));

        var findings = await _rule.EvaluateAsync(context);

        Assert.DoesNotContain(findings, f => f.RuleId == "WCAG_1_3_2_SEQUENCE");
    }

    [Fact]
    public async Task OnlyApplicableToLayouts()
    {
        Assert.Contains(AuditTargetType.Layout, _rule.ApplicableTargets);
        Assert.DoesNotContain(AuditTargetType.WebMap, _rule.ApplicableTargets);
    }

    private static AuditContext MakeContext() => new()
    {
        Target = new AuditTarget { TargetType = AuditTargetType.Layout, Name = "Test Layout" }
    };

    private static LayoutElementInfo MakeElement(string name, double x, double y, int sortOrder)
    {
        return new LayoutElementInfo
        {
            Name = name,
            ElementType = "TextElement",
            X = x,
            Y = y,
            SortOrder = sortOrder
        };
    }

    private static LayoutElementInfo MakeTextElement(string name, double x, double y, int sortOrder, double fontSize)
    {
        return new LayoutElementInfo
        {
            Name = name,
            ElementType = "TextElement",
            X = x,
            Y = y,
            SortOrder = sortOrder,
            FontSize = fontSize
        };
    }
}
