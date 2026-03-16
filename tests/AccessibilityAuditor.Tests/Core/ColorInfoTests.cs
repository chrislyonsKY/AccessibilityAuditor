using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.Tests.Core;

/// <summary>
/// Unit tests for <see cref="ColorInfo"/> covering construction,
/// alpha compositing, hex formatting, and equality.
/// </summary>
public sealed class ColorInfoTests
{
    [Fact]
    public void Constructor_SetsRgba()
    {
        var color = new ColorInfo(100, 150, 200, 128);

        Assert.Equal(100, color.R);
        Assert.Equal(150, color.G);
        Assert.Equal(200, color.B);
        Assert.Equal(128, color.A);
    }

    [Fact]
    public void Constructor_DefaultAlpha_Is255()
    {
        var color = new ColorInfo(10, 20, 30);

        Assert.Equal(255, color.A);
    }

    [Fact]
    public void Hex_FormatsCorrectly()
    {
        var color = new ColorInfo(0xFF, 0x88, 0x00);
        Assert.Equal("#FF8800", color.Hex);
    }

    [Fact]
    public void Hex_Black()
    {
        var black = new ColorInfo(0, 0, 0);
        Assert.Equal("#000000", black.Hex);
    }

    [Fact]
    public void Hex_White()
    {
        var white = new ColorInfo(255, 255, 255);
        Assert.Equal("#FFFFFF", white.Hex);
    }

    [Fact]
    public void ToString_OpaqueColor_ShowsHexOnly()
    {
        var color = new ColorInfo(255, 0, 0);
        Assert.Equal("#FF0000", color.ToString());
    }

    [Fact]
    public void ToString_TransparentColor_ShowsAlpha()
    {
        var color = new ColorInfo(255, 0, 0, 128);
        Assert.Contains("alpha=128", color.ToString());
    }

    [Fact]
    public void CompositeOver_FullyOpaque_ReturnsForeground()
    {
        var fg = new ColorInfo(100, 150, 200, 255);
        var bg = new ColorInfo(50, 50, 50);

        var result = fg.CompositeOver(bg);

        Assert.Equal(100, result.R);
        Assert.Equal(150, result.G);
        Assert.Equal(200, result.B);
        Assert.Equal(255, result.A);
    }

    [Fact]
    public void CompositeOver_FullyTransparent_ReturnsBackground()
    {
        var fg = new ColorInfo(100, 150, 200, 0);
        var bg = new ColorInfo(50, 60, 70);

        var result = fg.CompositeOver(bg);

        Assert.Equal(50, result.R);
        Assert.Equal(60, result.G);
        Assert.Equal(70, result.B);
    }

    [Fact]
    public void CompositeOver_HalfTransparent_Blends()
    {
        // 50% opacity: result = fg*0.5 + bg*0.5
        var fg = new ColorInfo(200, 200, 200, 128); // ~50% alpha
        var bg = new ColorInfo(0, 0, 0);

        var result = fg.CompositeOver(bg);

        // 200 * (128/255) + 0 * (127/255) ? 100
        Assert.InRange(result.R, 95, 105);
        Assert.InRange(result.G, 95, 105);
        Assert.InRange(result.B, 95, 105);
    }

    [Fact]
    public void CompositeOver_NullBackground_Throws()
    {
        var fg = new ColorInfo(100, 100, 100, 128);
        Assert.Throws<ArgumentNullException>(() => fg.CompositeOver(null!));
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new ColorInfo(10, 20, 30, 40);
        var b = new ColorInfo(10, 20, 30, 40);

        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var a = new ColorInfo(10, 20, 30);
        var b = new ColorInfo(10, 20, 31);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_DifferentAlpha_ReturnsFalse()
    {
        var a = new ColorInfo(10, 20, 30, 255);
        var b = new ColorInfo(10, 20, 30, 128);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var a = new ColorInfo(10, 20, 30);
        Assert.False(a.Equals(null));
    }

    [Fact]
    public void GetHashCode_SameValues_SameHash()
    {
        var a = new ColorInfo(10, 20, 30, 40);
        var b = new ColorInfo(10, 20, 30, 40);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
