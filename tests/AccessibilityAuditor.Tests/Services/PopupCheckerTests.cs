using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Orchestration;
using AccessibilityAuditor.Services.PortalInspector;

namespace AccessibilityAuditor.Tests.Services;

/// <summary>
/// Unit tests for <see cref="PopupChecker"/> covering HTML validation,
/// field label checks, and pop-up title analysis.
/// </summary>
public sealed class PopupCheckerTests
{
    private readonly PopupChecker _checker = new();

    [Fact]
    public void CheckPopups_NullContext_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _checker.CheckPopups(null!));
    }

    [Fact]
    public void CheckPopups_EmptyPopups_NoFindings()
    {
        var context = new AuditContext();
        var findings = _checker.CheckPopups(context);
        Assert.Empty(findings);
    }

    [Fact]
    public void CheckPopups_PopupWithTitleOnly_NoFindings()
    {
        // Title checks are now handled by HeadingsAndLabelsRule, not PopupChecker
        var context = new AuditContext();
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = null
        });

        var findings = _checker.CheckPopups(context);

        Assert.Empty(findings);
    }

    [Fact]
    public void CheckPopups_ImgWithoutAlt_Fail()
    {
        var context = new AuditContext();
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Test",
            HasCustomHtml = true,
            DescriptionHtml = "<p>Hello</p><img src=\"photo.jpg\">"
        });

        var findings = _checker.CheckPopups(context);

        Assert.Contains(findings, f =>
            f.Severity == FindingSeverity.Fail &&
            f.RuleId == "WCAG_4_1_1_PARSING" &&
            f.Detail.Contains("alt"));
    }

    [Fact]
    public void CheckPopups_ImgWithAlt_NoImageFinding()
    {
        var context = new AuditContext();
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Test",
            HasCustomHtml = true,
            DescriptionHtml = "<p>Hello</p><img src=\"photo.jpg\" alt=\"A photo\">"
        });

        var findings = _checker.CheckPopups(context);

        Assert.DoesNotContain(findings, f =>
            f.Detail.Contains("img") && f.Detail.Contains("alt"));
    }

    [Fact]
    public void CheckPopups_TableWithoutHeaders_Warning()
    {
        var context = new AuditContext();
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Test",
            HasCustomHtml = true,
            DescriptionHtml = "<table><tr><td>Data</td></tr></table>"
        });

        var findings = _checker.CheckPopups(context);

        Assert.Contains(findings, f =>
            f.Severity == FindingSeverity.Warning &&
            f.Detail.Contains("table") && f.Detail.Contains("header"));
    }

    [Fact]
    public void CheckPopups_DeprecatedHtml_Warning()
    {
        var context = new AuditContext();
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Test",
            HasCustomHtml = true,
            DescriptionHtml = "<font color='red'>Warning</font>"
        });

        var findings = _checker.CheckPopups(context);

        Assert.Contains(findings, f =>
            f.Detail.Contains("deprecated"));
    }

    [Fact]
    public void CheckPopups_SystemFieldName_Warning()
    {
        var context = new AuditContext();
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Test",
            FieldNames = { "PERMIT_NUMBER", "STATUS_CODE" },
            FieldLabels = { "PERMIT_NUMBER", "STATUS_CODE" } // Same as field name = no user-friendly label
        });

        var findings = _checker.CheckPopups(context);

        Assert.Contains(findings, f =>
            f.RuleId == "WCAG_3_3_2_LABELS" &&
            f.Severity == FindingSeverity.Warning);
    }

    [Fact]
    public void CheckPopups_HumanReadableLabel_NoWarning()
    {
        var context = new AuditContext();
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Test",
            FieldNames = { "PERMIT_NUMBER" },
            FieldLabels = { "Permit Number" } // Different = user-friendly
        });

        var findings = _checker.CheckPopups(context);

        Assert.DoesNotContain(findings, f => f.RuleId == "WCAG_3_3_2_LABELS");
    }

    [Fact]
    public void CheckPopups_BackgroundImage_ManualReview()
    {
        var context = new AuditContext();
        context.Popups.Add(new PopupInfo
        {
            LayerName = "TestLayer",
            TitleTemplate = "Test",
            HasCustomHtml = true,
            DescriptionHtml = "<div style=\"background-image: url('bg.png')\">Content</div>"
        });

        var findings = _checker.CheckPopups(context);

        Assert.Contains(findings, f =>
            f.Severity == FindingSeverity.ManualReview &&
            f.Detail.Contains("background-image"));
    }
}
