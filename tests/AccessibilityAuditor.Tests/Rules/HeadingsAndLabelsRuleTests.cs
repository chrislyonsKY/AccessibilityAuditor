using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Orchestration;
using AccessibilityAuditor.Rules;

namespace AccessibilityAuditor.Tests.Rules;

/// <summary>
/// Unit tests for <see cref="HeadingsAndLabelsRule"/> covering popup titles,
/// heading hierarchy, and ExB widget labels.
/// </summary>
public sealed class HeadingsAndLabelsRuleTests
{
    private readonly HeadingsAndLabelsRule _rule = new();

    [Fact]
    public async Task NoPopupsNoWidgets_Pass()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        var findings = await _rule.EvaluateAsync(context);

        Assert.Single(findings);
        Assert.Equal(FindingSeverity.Pass, findings[0].Severity);
    }

    [Fact]
    public async Task PopupMissingTitle_Fail()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        context.Popups.Add(new PopupInfo { LayerName = "TestLayer", TitleTemplate = null });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Contains(findings, f =>
            f.Severity == FindingSeverity.Fail &&
            f.RuleId == "WCAG_2_4_6_HEADINGS");
    }

    [Fact]
    public async Task PopupObjectIdTitle_Warning()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        context.Popups.Add(new PopupInfo { LayerName = "TestLayer", TitleTemplate = "{OBJECTID}" });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Contains(findings, f =>
            f.Severity == FindingSeverity.Warning &&
            f.Detail.Contains("non-descriptive"));
    }

    [Fact]
    public async Task PopupDescriptiveTitle_NoTitleFinding()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        context.Popups.Add(new PopupInfo { LayerName = "TestLayer", TitleTemplate = "{PERMIT_NAME}" });

        var findings = await _rule.EvaluateAsync(context);

        Assert.DoesNotContain(findings, f =>
            f.Severity == FindingSeverity.Fail && f.Detail.Contains("title"));
    }

    [Fact]
    public async Task PopupSkippedHeadingLevel_Warning()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Details",
            HasCustomHtml = true,
            DescriptionHtml = "<h1>Title</h1><h3>Subsection</h3>"
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Contains(findings, f =>
            f.Detail.Contains("skip") && f.Detail.Contains("h1") && f.Detail.Contains("h3"));
    }

    [Fact]
    public async Task PopupEmptyHeading_Fail()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Details",
            HasCustomHtml = true,
            DescriptionHtml = "<h2>  </h2><p>Content</p>"
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Contains(findings, f =>
            f.Severity == FindingSeverity.Fail &&
            f.Detail.Contains("empty heading"));
    }

    [Fact]
    public async Task PopupProperHeadingHierarchy_NoHeadingFinding()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Details",
            HasCustomHtml = true,
            DescriptionHtml = "<h1>Main</h1><h2>Sub</h2><p>Content</p>"
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.DoesNotContain(findings, f =>
            f.Detail.Contains("skip") || f.Detail.Contains("empty heading"));
    }

    [Fact]
    public async Task ExBWidgetNoLabel_Warning()
    {
        var context = MakeContext(AuditTargetType.ExperienceBuilder);
        context.ExperienceBuilder = new ExperienceBuilderInfo
        {
            Title = "Test App"
        };
        context.ExperienceBuilder.Widgets.Add(new ExBWidgetInfo
        {
            WidgetId = "widget_1",
            WidgetType = "search",
            HasLabel = false,
            Label = null
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Contains(findings, f =>
            f.Severity == FindingSeverity.Warning &&
            f.Element.Contains("widget_1"));
    }

    [Fact]
    public async Task ExBWidgetLabelSameAsType_Warning()
    {
        var context = MakeContext(AuditTargetType.ExperienceBuilder);
        context.ExperienceBuilder = new ExperienceBuilderInfo { Title = "Test App" };
        context.ExperienceBuilder.Widgets.Add(new ExBWidgetInfo
        {
            WidgetId = "w1",
            WidgetType = "search",
            HasLabel = true,
            Label = "search"
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Contains(findings, f =>
            f.Severity == FindingSeverity.Warning &&
            f.Detail.Contains("same as the widget type"));
    }

    [Fact]
    public async Task ExBWidgetDescriptiveLabel_NoFinding()
    {
        var context = MakeContext(AuditTargetType.ExperienceBuilder);
        context.ExperienceBuilder = new ExperienceBuilderInfo { Title = "Test App" };
        context.ExperienceBuilder.Widgets.Add(new ExBWidgetInfo
        {
            WidgetId = "w1",
            WidgetType = "search",
            HasLabel = true,
            Label = "Search for permits"
        });

        var findings = await _rule.EvaluateAsync(context);

        // Should only have the Pass finding
        Assert.Single(findings);
        Assert.Equal(FindingSeverity.Pass, findings[0].Severity);
    }

    [Fact]
    public void ApplicableTargets_WebMapAndExB()
    {
        Assert.Contains(AuditTargetType.WebMap, _rule.ApplicableTargets);
        Assert.Contains(AuditTargetType.ExperienceBuilder, _rule.ApplicableTargets);
    }

    private static AuditContext MakeContext(AuditTargetType type) => new()
    {
        Target = new AuditTarget { TargetType = type, Name = "Test" }
    };
}
