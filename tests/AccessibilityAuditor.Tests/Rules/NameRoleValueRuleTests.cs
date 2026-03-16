using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Orchestration;
using AccessibilityAuditor.Rules;

namespace AccessibilityAuditor.Tests.Rules;

/// <summary>
/// Unit tests for <see cref="NameRoleValueRule"/> covering ExB widget accessibility
/// and popup interactive element checks.
/// </summary>
public sealed class NameRoleValueRuleTests
{
    private readonly NameRoleValueRule _rule = new();

    [Fact]
    public async Task NoWidgetsNoPopups_Pass()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        var findings = await _rule.EvaluateAsync(context);

        Assert.Single(findings);
        Assert.Equal(FindingSeverity.Pass, findings[0].Severity);
    }

    [Fact]
    public async Task InteractiveWidgetNoLabel_Fail()
    {
        var context = MakeContext(AuditTargetType.ExperienceBuilder);
        context.ExperienceBuilder = new ExperienceBuilderInfo { Title = "App" };
        context.ExperienceBuilder.Widgets.Add(new ExBWidgetInfo
        {
            WidgetId = "search_1",
            WidgetType = "search",
            HasLabel = false
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Contains(findings, f =>
            f.Severity == FindingSeverity.Fail &&
            f.RuleId == "WCAG_4_1_2_NAME_ROLE" &&
            f.Element.Contains("search_1"));
    }

    [Fact]
    public async Task NonInteractiveWidgetNoLabel_NoFail()
    {
        var context = MakeContext(AuditTargetType.ExperienceBuilder);
        context.ExperienceBuilder = new ExperienceBuilderInfo { Title = "App" };
        context.ExperienceBuilder.Widgets.Add(new ExBWidgetInfo
        {
            WidgetId = "map_1",
            WidgetType = "map",
            HasLabel = false
        });

        var findings = await _rule.EvaluateAsync(context);

        // Non-interactive widgets (like map display) don't require labels for 4.1.2
        Assert.DoesNotContain(findings, f =>
            f.Severity == FindingSeverity.Fail && f.Element.Contains("map_1"));
    }

    [Fact]
    public async Task InteractiveWidgetWithLabel_Pass()
    {
        var context = MakeContext(AuditTargetType.ExperienceBuilder);
        context.ExperienceBuilder = new ExperienceBuilderInfo { Title = "App" };
        context.ExperienceBuilder.Widgets.Add(new ExBWidgetInfo
        {
            WidgetId = "search_1",
            WidgetType = "search",
            HasLabel = true,
            Label = "Search parcels"
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Single(findings);
        Assert.Equal(FindingSeverity.Pass, findings[0].Severity);
    }

    [Fact]
    public async Task PopupEmptyButton_Fail()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Test",
            HasCustomHtml = true,
            DescriptionHtml = "<p>Info</p><button></button>"
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Contains(findings, f =>
            f.Severity == FindingSeverity.Fail &&
            f.Detail.Contains("empty <button>"));
    }

    [Fact]
    public async Task PopupInputWithoutLabel_Fail()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Test",
            HasCustomHtml = true,
            DescriptionHtml = "<form><input type=\"text\" name=\"search\"></form>"
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Contains(findings, f =>
            f.Severity == FindingSeverity.Fail &&
            f.Detail.Contains("input"));
    }

    [Fact]
    public async Task PopupInputWithAriaLabel_NoInputFinding()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Test",
            HasCustomHtml = true,
            DescriptionHtml = "<form><input type=\"text\" aria-label=\"Search permits\"></form>"
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.DoesNotContain(findings, f =>
            f.Detail.Contains("input") && f.Severity == FindingSeverity.Fail);
    }

    [Fact]
    public async Task PopupLinkWrappingImageWithoutAria_Warning()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Test",
            HasCustomHtml = true,
            DescriptionHtml = "<a href=\"details.html\"><img src=\"icon.png\"></a>"
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Contains(findings, f =>
            f.Severity == FindingSeverity.Warning &&
            f.Detail.Contains("link") && f.Detail.Contains("image"));
    }

    [Fact]
    public async Task PopupNoInteractiveElements_Pass()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Test",
            HasCustomHtml = true,
            DescriptionHtml = "<p>Just text content</p>"
        });

        var findings = await _rule.EvaluateAsync(context);

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
