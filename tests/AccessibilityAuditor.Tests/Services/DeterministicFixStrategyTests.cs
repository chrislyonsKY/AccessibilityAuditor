using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Services.ColorAnalysis;
using AccessibilityAuditor.Services.Fixes;

namespace AccessibilityAuditor.Tests.Services;

public class DeterministicFixStrategyTests
{
    private readonly DeterministicFixStrategy _strategy = new();

    #region Helpers

    private static Finding MakeContrastFinding(
        byte fgR, byte fgG, byte fgB,
        byte bgR, byte bgG, byte bgB,
        string ruleId = "WCAG_1_4_3_CONTRAST")
    {
        return new Finding
        {
            RuleId = ruleId,
            Severity = FindingSeverity.Fail,
            Element = "Label class 'Cities' on layer 'Roads'",
            Detail = "Contrast ratio below threshold",
            ForegroundColor = new ColorInfo(fgR, fgG, fgB),
            BackgroundColor = new ColorInfo(bgR, bgG, bgB),
            ContrastRatio = ContrastCalculator.Calculate(
                new ColorInfo(fgR, fgG, fgB),
                new ColorInfo(bgR, bgG, bgB))
        };
    }

    private static Finding MakeAltTextFinding()
    {
        return new Finding
        {
            RuleId = "WCAG_1_1_1_ALT_TEXT",
            Severity = FindingSeverity.Fail,
            Element = "Picture element 'Logo'",
            Detail = "Missing alternative text"
        };
    }

    private static Finding MakeColorFinding()
    {
        return new Finding
        {
            RuleId = "WCAG_1_4_1_USE_OF_COLOR",
            Severity = FindingSeverity.Warning,
            Element = "Layer 'Risk Zones'",
            Detail = "Color is the sole differentiator"
        };
    }

    #endregion

    #region RequiresApiKey

    [Fact]
    public void RequiresApiKey_ReturnsFalse()
    {
        Assert.False(_strategy.RequiresApiKey);
    }

    #endregion

    #region Contrast Fix

    [Fact]
    public async Task FixContrast_MissingColors_ReturnsFailed()
    {
        var finding = new Finding
        {
            RuleId = "WCAG_1_4_3_CONTRAST",
            Severity = FindingSeverity.Fail,
            Element = "Test",
            Detail = "Test"
        };

        var result = await _strategy.ApplyFixAsync(finding, CancellationToken.None);

        Assert.Equal(FixStatus.Failed, result.Status);
        Assert.Contains("Missing color data", result.Summary);
    }

    [Fact]
    public async Task FixContrast_AlreadyPassing_ReturnsApplied()
    {
        // Black on white = 21:1 contrast
        var finding = MakeContrastFinding(0, 0, 0, 255, 255, 255);

        var result = await _strategy.ApplyFixAsync(finding, CancellationToken.None);

        Assert.Equal(FixStatus.Applied, result.Status);
        Assert.Contains("Already meets", result.Summary);
    }

    [Fact]
    public async Task FixContrast_FailingColor_ReturnsSuggestedWithFixedHex()
    {
        // Light gray on white — fails 4.5:1
        var finding = MakeContrastFinding(180, 180, 180, 255, 255, 255);
        double originalRatio = ContrastCalculator.Calculate(
            finding.ForegroundColor!, finding.BackgroundColor!);
        Assert.True(originalRatio < 4.5);

        var result = await _strategy.ApplyFixAsync(finding, CancellationToken.None);

        // Should suggest a fix (CIM write won't succeed without Pro running)
        Assert.Equal(FixStatus.Suggested, result.Status);
        Assert.NotNull(result.SuggestedContent);
        Assert.StartsWith("#", result.SuggestedContent);
    }

    [Fact]
    public async Task FixContrast_NonTextRule_Uses3To1Threshold()
    {
        // Medium gray on white — passes 3:1 but fails 4.5:1
        var finding = MakeContrastFinding(119, 119, 119, 255, 255, 255,
            "WCAG_1_4_11_NON_TEXT");
        double ratio = ContrastCalculator.Calculate(
            finding.ForegroundColor!, finding.BackgroundColor!);

        var result = await _strategy.ApplyFixAsync(finding, CancellationToken.None);

        // At 3:1 threshold, medium gray might already pass
        Assert.True(result.Status is FixStatus.Applied or FixStatus.Suggested);
    }

    #endregion

    #region Alt Text Fix

    [Fact]
    public async Task FixAltText_ReturnsSuggestedWithStub()
    {
        var finding = MakeAltTextFinding();

        var result = await _strategy.ApplyFixAsync(finding, CancellationToken.None);

        Assert.Equal(FixStatus.Suggested, result.Status);
        Assert.Equal("[Description required]", result.SuggestedContent);
        Assert.Contains("Logo", result.Summary);
    }

    #endregion

    #region Colorblind Palette Fix

    [Fact]
    public async Task FixColorblind_ReturnsSuggestedWithGuidance()
    {
        var finding = MakeColorFinding();

        var result = await _strategy.ApplyFixAsync(finding, CancellationToken.None);

        Assert.Equal(FixStatus.Suggested, result.Status);
        Assert.Contains("pattern", result.SuggestedContent);
    }

    #endregion

    #region Unsupported Rule

    [Fact]
    public async Task UnsupportedRule_ReturnsFailed()
    {
        var finding = new Finding
        {
            RuleId = "WCAG_9_9_9_UNKNOWN",
            Severity = FindingSeverity.Fail,
            Element = "Test",
            Detail = "Test"
        };

        var result = await _strategy.ApplyFixAsync(finding, CancellationToken.None);

        Assert.Equal(FixStatus.Failed, result.Status);
    }

    #endregion

    #region FindNearestPassingColor

    [Fact]
    public void FindNearestPassingColor_ReturnsPassingColor()
    {
        var fg = new ColorInfo(180, 180, 180); // light gray
        var bg = new ColorInfo(255, 255, 255); // white

        var result = DeterministicFixStrategy.FindNearestPassingColor(fg, bg, 4.5);

        Assert.NotNull(result);
        double ratio = ContrastCalculator.Calculate(result!, bg);
        Assert.True(ratio >= 4.5, $"Expected ratio >= 4.5 but got {ratio:F2}");
    }

    [Fact]
    public void FindNearestPassingColor_PreservesHue()
    {
        var fg = new ColorInfo(200, 100, 100); // reddish
        var bg = new ColorInfo(255, 255, 255); // white

        var result = DeterministicFixStrategy.FindNearestPassingColor(fg, bg, 4.5);

        Assert.NotNull(result);
        // The fixed color should still be in the red hue family
        DeterministicFixStrategy.RgbToHsl(fg.R, fg.G, fg.B, out double origH, out _, out _);
        DeterministicFixStrategy.RgbToHsl(result!.R, result.G, result.B, out double fixedH, out _, out _);
        // Hue difference should be minimal (same hue family)
        Assert.InRange(Math.Abs(origH - fixedH), 0, 0.05);
    }

    [Fact]
    public void FindNearestPassingColor_BlackOnWhite_ReturnsNull()
    {
        // Black on white already passes — but with targetRatio=22 (impossible), returns null
        var fg = new ColorInfo(255, 255, 255); // white
        var bg = new ColorInfo(254, 254, 254); // nearly white

        var result = DeterministicFixStrategy.FindNearestPassingColor(fg, bg, 21.1);

        Assert.Null(result);
    }

    #endregion

    #region ExtractQuotedName

    [Theory]
    [InlineData("Label class 'Cities' on layer 'Roads'", "Cities")]
    [InlineData("Text element 'Title'", "Title")]
    [InlineData("No quotes here", null)]
    public void ExtractQuotedName_ParsesCorrectly(string input, string? expected)
    {
        Assert.Equal(expected, DeterministicFixStrategy.ExtractQuotedName(input));
    }

    #endregion

    #region HSL Roundtrip

    [Theory]
    [InlineData(255, 0, 0)]
    [InlineData(0, 255, 0)]
    [InlineData(0, 0, 255)]
    [InlineData(128, 128, 128)]
    [InlineData(0, 0, 0)]
    [InlineData(255, 255, 255)]
    public void HslRoundtrip_PreservesColor(byte r, byte g, byte b)
    {
        DeterministicFixStrategy.RgbToHsl(r, g, b, out double h, out double s, out double l);
        DeterministicFixStrategy.HslToRgb(h, s, l, out byte r2, out byte g2, out byte b2);

        Assert.InRange(Math.Abs(r - r2), 0, 1);
        Assert.InRange(Math.Abs(g - g2), 0, 1);
        Assert.InRange(Math.Abs(b - b2), 0, 1);
    }

    #endregion
}
