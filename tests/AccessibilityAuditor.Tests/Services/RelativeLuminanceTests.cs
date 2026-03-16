using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Services.ColorAnalysis;

namespace AccessibilityAuditor.Tests.Services;

/// <summary>
/// Unit tests for <see cref="RelativeLuminance"/> verifying the exact WCAG 2.1 algorithm.
/// </summary>
public sealed class RelativeLuminanceTests
{
    [Fact]
    public void Black_ReturnsZero()
    {
        double luminance = RelativeLuminance.Calculate(0, 0, 0);
        Assert.Equal(0.0, luminance, 5);
    }

    [Fact]
    public void White_ReturnsOne()
    {
        double luminance = RelativeLuminance.Calculate(255, 255, 255);
        Assert.Equal(1.0, luminance, 5);
    }

    [Fact]
    public void PureRed_MatchesCoefficient()
    {
        // Pure red (255,0,0) ? linear R = 1.0
        // L = 0.2126 * 1.0 + 0.7152 * 0.0 + 0.0722 * 0.0 = 0.2126
        double luminance = RelativeLuminance.Calculate(255, 0, 0);
        Assert.Equal(0.2126, luminance, 4);
    }

    [Fact]
    public void PureGreen_MatchesCoefficient()
    {
        // Pure green (0,255,0) ? linear G = 1.0
        // L = 0.2126 * 0.0 + 0.7152 * 1.0 + 0.0722 * 0.0 = 0.7152
        double luminance = RelativeLuminance.Calculate(0, 255, 0);
        Assert.Equal(0.7152, luminance, 4);
    }

    [Fact]
    public void PureBlue_MatchesCoefficient()
    {
        // Pure blue (0,0,255) ? linear B = 1.0
        // L = 0.2126 * 0.0 + 0.7152 * 0.0 + 0.0722 * 1.0 = 0.0722
        double luminance = RelativeLuminance.Calculate(0, 0, 255);
        Assert.Equal(0.0722, luminance, 4);
    }

    [Fact]
    public void MidGray_777777()
    {
        // #777777 ? R=G=B=119 ? sRGB = 119/255 ? 0.4667
        // 0.4667 > 0.03928, so linear = ((0.4667+0.055)/1.055)^2.4 ? 0.1845
        // L = 0.2126*0.1845 + 0.7152*0.1845 + 0.0722*0.1845 = 0.1845
        double luminance = RelativeLuminance.Calculate(0x77, 0x77, 0x77);
        Assert.InRange(luminance, 0.18, 0.19);
    }

    [Theory]
    [InlineData(0, 0, 0, 0.0)]
    [InlineData(255, 255, 255, 1.0)]
    [InlineData(128, 128, 128, 0.2158)] // ~mid-gray
    public void KnownValues_AreWithinTolerance(byte r, byte g, byte b, double expected)
    {
        double luminance = RelativeLuminance.Calculate(r, g, b);
        Assert.Equal(expected, luminance, 3);
    }

    [Fact]
    public void LowValues_UseBelowThresholdFormula()
    {
        // sRGB values ? 0.03928 (? 10 in byte) use the linear formula: C/12.92
        // R=G=B=10 ? 10/255 ? 0.03922 ? 0.03928 ? linear = 0.03922/12.92 ? 0.003035
        double luminance = RelativeLuminance.Calculate(10, 10, 10);
        Assert.InRange(luminance, 0.002, 0.004);
    }
}
