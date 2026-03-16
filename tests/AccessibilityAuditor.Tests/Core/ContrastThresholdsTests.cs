using AccessibilityAuditor.Core.Constants;

namespace AccessibilityAuditor.Tests.Core;

/// <summary>
/// Unit tests for <see cref="ContrastThresholds"/> covering threshold
/// constants and large text determination logic.
/// </summary>
public sealed class ContrastThresholdsTests
{
    [Fact]
    public void NormalText_Is4_5()
    {
        Assert.Equal(4.5, ContrastThresholds.NormalText);
    }

    [Fact]
    public void LargeText_Is3_0()
    {
        Assert.Equal(3.0, ContrastThresholds.LargeText);
    }

    [Fact]
    public void NonTextGraphics_Is3_0()
    {
        Assert.Equal(3.0, ContrastThresholds.NonTextGraphics);
    }

    [Theory]
    [InlineData(18.0, false, true)]   // 18pt normal = large
    [InlineData(17.9, false, false)]  // just below 18pt normal = not large
    [InlineData(14.0, true, true)]    // 14pt bold = large
    [InlineData(13.9, true, false)]   // just below 14pt bold = not large
    [InlineData(24.0, false, true)]   // well above 18pt = large
    [InlineData(14.0, false, false)]  // 14pt non-bold = not large
    [InlineData(10.0, false, false)]  // small non-bold = not large
    [InlineData(10.0, true, false)]   // small bold = not large
    public void IsLargeText_ReturnsCorrectly(double pointSize, bool isBold, bool expected)
    {
        Assert.Equal(expected, ContrastThresholds.IsLargeText(pointSize, isBold));
    }

    [Theory]
    [InlineData(12.0, false, 4.5)]   // normal text ? 4.5
    [InlineData(18.0, false, 3.0)]   // large text (18pt) ? 3.0
    [InlineData(14.0, true, 3.0)]    // large text (14pt bold) ? 3.0
    [InlineData(10.0, true, 4.5)]    // small bold ? 4.5
    [InlineData(24.0, true, 3.0)]    // large bold ? 3.0
    public void GetThreshold_ReturnsCorrectValue(double pointSize, bool isBold, double expected)
    {
        Assert.Equal(expected, ContrastThresholds.GetThreshold(pointSize, isBold));
    }
}
