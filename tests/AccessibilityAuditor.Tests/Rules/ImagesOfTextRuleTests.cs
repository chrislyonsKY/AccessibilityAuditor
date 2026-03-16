using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Orchestration;
using AccessibilityAuditor.Rules;

namespace AccessibilityAuditor.Tests.Rules;

/// <summary>
/// Unit tests for <see cref="ImagesOfTextRule"/> covering layout picture element
/// detection and popup image flagging.
/// </summary>
public sealed class ImagesOfTextRuleTests
{
    private readonly ImagesOfTextRule _rule = new();

    [Fact]
    public async Task NoElements_Pass()
    {
        var context = MakeContext(AuditTargetType.Layout);
        var findings = await _rule.EvaluateAsync(context);

        Assert.Single(findings);
        Assert.Equal(FindingSeverity.Pass, findings[0].Severity);
    }

    [Fact]
    public async Task TextElementOnly_Pass()
    {
        var context = MakeContext(AuditTargetType.Layout);
        context.LayoutElements.Add(new LayoutElementInfo
        {
            Name = "Title",
            ElementType = "TextElement",
            IsPictureElement = false
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Single(findings);
        Assert.Equal(FindingSeverity.Pass, findings[0].Severity);
    }

    [Fact]
    public async Task PictureElement_ManualReview()
    {
        var context = MakeContext(AuditTargetType.Layout);
        context.LayoutElements.Add(new LayoutElementInfo
        {
            Name = "Logo",
            ElementType = "PictureElement",
            IsPictureElement = true
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Contains(findings, f =>
            f.Severity == FindingSeverity.ManualReview &&
            f.Element.Contains("Logo"));
    }

    [Fact]
    public async Task MultiplePictures_MultipleFindingsPerElement()
    {
        var context = MakeContext(AuditTargetType.Layout);
        context.LayoutElements.Add(new LayoutElementInfo { Name = "Logo1", IsPictureElement = true });
        context.LayoutElements.Add(new LayoutElementInfo { Name = "Logo2", IsPictureElement = true });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Equal(2, findings.Count(f => f.Severity == FindingSeverity.ManualReview));
    }

    [Fact]
    public async Task PopupWithImages_ManualReview()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        context.Popups.Add(new PopupInfo
        {
            LayerName = "Photos",
            TitleTemplate = "Photo",
            HasCustomHtml = true,
            DescriptionHtml = "<p>View:</p><img src=\"photo.jpg\" alt=\"Site photo\"><img src=\"map.png\" alt=\"\">"
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Contains(findings, f =>
            f.Severity == FindingSeverity.ManualReview &&
            f.Detail.Contains("2 image"));
    }

    [Fact]
    public async Task PopupNoImages_Pass()
    {
        var context = MakeContext(AuditTargetType.WebMap);
        context.Popups.Add(new PopupInfo
        {
            LayerName = "Data",
            TitleTemplate = "Info",
            HasCustomHtml = true,
            DescriptionHtml = "<p>No images here</p>"
        });

        var findings = await _rule.EvaluateAsync(context);

        Assert.Single(findings);
        Assert.Equal(FindingSeverity.Pass, findings[0].Severity);
    }

    [Fact]
    public void ApplicableTargets_IncludesLayoutAndWebMap()
    {
        Assert.Contains(AuditTargetType.Layout, _rule.ApplicableTargets);
        Assert.Contains(AuditTargetType.WebMap, _rule.ApplicableTargets);
    }

    private static AuditContext MakeContext(AuditTargetType type) => new()
    {
        Target = new AuditTarget { TargetType = type, Name = "Test" }
    };
}
