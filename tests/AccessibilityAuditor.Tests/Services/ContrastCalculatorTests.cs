using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Services.ColorAnalysis;

namespace AccessibilityAuditor.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ContrastCalculator"/> verifying WCAG 2.1 contrast ratio computation.
/// Test vectors include known ratios from the WCAG specification and W3C examples.
/// </summary>
public sealed class ContrastCalculatorTests
{
    [Fact]
    public void WhiteOnBlack_Returns21To1()
    {
        var white = new ColorInfo(255, 255, 255);
        var black = new ColorInfo(0, 0, 0);

        double ratio = ContrastCalculator.Calculate(white, black);

        Assert.Equal(21.0, ratio, 1);
    }

    [Fact]
    public void BlackOnWhite_Returns21To1()
    {
        // Order should not matter — lighter/darker is determined internally
        var white = new ColorInfo(255, 255, 255);
        var black = new ColorInfo(0, 0, 0);

        double ratio = ContrastCalculator.Calculate(black, white);

        Assert.Equal(21.0, ratio, 1);
    }

    [Fact]
    public void SameColor_Returns1To1()
    {
        var red = new ColorInfo(255, 0, 0);

        double ratio = ContrastCalculator.Calculate(red, red);

        Assert.Equal(1.0, ratio, 2);
    }

    [Fact]
    public void Gray777_OnWhite_ReturnsApprox4_48()
    {
        // #777777 on #FFFFFF — known WCAG test vector ? 4.48:1
        var gray = new ColorInfo(0x77, 0x77, 0x77);
        var white = new ColorInfo(255, 255, 255);

        double ratio = ContrastCalculator.Calculate(gray, white);

        // Should be approximately 4.48
        Assert.InRange(ratio, 4.4, 4.6);
    }

    [Fact]
    public void Gray777_OnWhite_FailsNormalTextThreshold()
    {
        var gray = new ColorInfo(0x77, 0x77, 0x77);
        var white = new ColorInfo(255, 255, 255);

        double ratio = ContrastCalculator.Calculate(gray, white);

        // 4.48 < 4.5 — should fail normal text AA
        Assert.True(ratio < 4.5, $"Expected < 4.5:1 but got {ratio:F2}:1");
    }

    [Fact]
    public void Gray777_OnWhite_PassesLargeTextThreshold()
    {
        var gray = new ColorInfo(0x77, 0x77, 0x77);
        var white = new ColorInfo(255, 255, 255);

        double ratio = ContrastCalculator.Calculate(gray, white);

        // 4.48 >= 3.0 — should pass large text AA
        Assert.True(ratio >= 3.0, $"Expected >= 3:1 but got {ratio:F2}:1");
    }

    [Fact]
    public void DarkGray333_OnGray555_LowContrast()
    {
        // #333333 on #555555 — low contrast pair
        var dark = new ColorInfo(0x33, 0x33, 0x33);
        var mid = new ColorInfo(0x55, 0x55, 0x55);

        double ratio = ContrastCalculator.Calculate(dark, mid);

        // Should be around 2:1 — fails both normal and large text
        Assert.InRange(ratio, 1.5, 2.5);
    }

    [Fact]
    public void AlphaCompositing_SemiTransparentBlackOnWhite()
    {
        // 50% transparent black (#000000 at alpha 128) on white
        // Composited: (0*0.502 + 255*0.498) ? (127, 127, 127)
        var semiBlack = new ColorInfo(0, 0, 0, 128);
        var white = new ColorInfo(255, 255, 255);

        double ratio = ContrastCalculator.Calculate(semiBlack, white);

        // Should be the contrast of mid-gray on white, roughly 4-5:1
        Assert.InRange(ratio, 3.5, 5.5);
    }

    [Fact]
    public void FullyOpaqueColor_NoCompositing()
    {
        var blue = new ColorInfo(0, 0, 255, 255);
        var white = new ColorInfo(255, 255, 255);

        double ratio = ContrastCalculator.Calculate(blue, white);

        // Pure blue on white — known to be high contrast
        Assert.True(ratio > 5.0);
    }

    [Fact]
    public void NullForeground_Throws()
    {
        var bg = new ColorInfo(255, 255, 255);
        Assert.Throws<ArgumentNullException>(() => ContrastCalculator.Calculate(null!, bg));
    }

    [Fact]
    public void NullBackground_Throws()
    {
        var fg = new ColorInfo(0, 0, 0);
        Assert.Throws<ArgumentNullException>(() => ContrastCalculator.Calculate(fg, null!));
    }

    [Fact]
    public void ContrastRatio_SymmetricInputs()
    {
        // ContrastRatio(l1, l2) should equal ContrastRatio(l2, l1)
        double r1 = ContrastCalculator.ContrastRatio(0.5, 0.1);
        double r2 = ContrastCalculator.ContrastRatio(0.1, 0.5);

        Assert.Equal(r1, r2, 10);
    }

    [Fact]
    public void ContrastRatio_BothZero_Returns1()
    {
        double ratio = ContrastCalculator.ContrastRatio(0.0, 0.0);
        Assert.Equal(1.0, ratio, 5);
    }

    [Theory]
    [InlineData(255, 255, 255, 0, 0, 0, 21.0)]       // white on black
    [InlineData(0, 0, 0, 255, 255, 255, 21.0)]         // black on white
    [InlineData(255, 0, 0, 255, 0, 0, 1.0)]           // same color
    public void KnownPairs_ReturnExpectedRatio(
        byte fgR, byte fgG, byte fgB,
        byte bgR, byte bgG, byte bgB,
        double expectedRatio)
    {
        var fg = new ColorInfo(fgR, fgG, fgB);
        var bg = new ColorInfo(bgR, bgG, bgB);

        double ratio = ContrastCalculator.Calculate(fg, bg);

        Assert.Equal(expectedRatio, ratio, 0);
    }
}
