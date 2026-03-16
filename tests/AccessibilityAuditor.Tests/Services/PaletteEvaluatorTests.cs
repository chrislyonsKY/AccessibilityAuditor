using System.Collections.Generic;
using System.Linq;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Services.ColorAnalysis;

namespace AccessibilityAuditor.Tests.Services;

/// <summary>
/// Unit tests for <see cref="PaletteEvaluator"/> covering batch contrast
/// evaluation and colorblind safety analysis.
/// </summary>
public sealed class PaletteEvaluatorTests
{
    [Fact]
    public void EvaluateAgainstBackground_AllMeetThreshold()
    {
        var colors = new List<ColorInfo>
        {
            new ColorInfo(0, 0, 0),       // black
            new ColorInfo(0, 0, 255),     // blue
        };
        var background = new ColorInfo(255, 255, 255); // white

        var results = PaletteEvaluator.EvaluateAgainstBackground(colors, background, 3.0);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.MeetsThreshold));
    }

    [Fact]
    public void EvaluateAgainstBackground_LowContrastFails()
    {
        var colors = new List<ColorInfo>
        {
            new ColorInfo(240, 240, 240), // very light gray on white
        };
        var background = new ColorInfo(255, 255, 255);

        var results = PaletteEvaluator.EvaluateAgainstBackground(colors, background, 3.0);

        Assert.Single(results);
        Assert.False(results[0].MeetsThreshold);
        Assert.True(results[0].ContrastRatio < 3.0);
    }

    [Fact]
    public void EvaluateAgainstBackground_NullColors_Throws()
    {
        var bg = new ColorInfo(255, 255, 255);
        Assert.Throws<ArgumentNullException>(() =>
            PaletteEvaluator.EvaluateAgainstBackground(null!, bg));
    }

    [Fact]
    public void EvaluateAgainstBackground_NullBackground_Throws()
    {
        var colors = new List<ColorInfo> { new ColorInfo(0, 0, 0) };
        Assert.Throws<ArgumentNullException>(() =>
            PaletteEvaluator.EvaluateAgainstBackground(colors, null!));
    }

    [Fact]
    public void EvaluateColorBlindSafety_DistinctColors_AllPass()
    {
        var colors = new List<ColorInfo>
        {
            new ColorInfo(0, 0, 0),       // black
            new ColorInfo(255, 255, 255), // white
        };

        var results = PaletteEvaluator.EvaluateColorBlindSafety(colors);

        Assert.Equal(3, results.Count); // One per ColorBlindType
        Assert.All(results, r => Assert.True(r.AllDistinguishable));
    }

    [Fact]
    public void EvaluateColorBlindSafety_SameColor_AllFail()
    {
        var colors = new List<ColorInfo>
        {
            new ColorInfo(128, 128, 128),
            new ColorInfo(128, 128, 128),
        };

        var results = PaletteEvaluator.EvaluateColorBlindSafety(colors);

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.False(r.AllDistinguishable));
    }

    [Fact]
    public void EvaluateColorBlindSafety_SingleColor_AllPass()
    {
        var colors = new List<ColorInfo>
        {
            new ColorInfo(128, 64, 32),
        };

        var results = PaletteEvaluator.EvaluateColorBlindSafety(colors);

        // No pairs to test ? all distinguishable
        Assert.All(results, r => Assert.True(r.AllDistinguishable));
        Assert.All(results, r => Assert.Empty(r.FailingPairs));
    }

    [Fact]
    public void EvaluateColorBlindSafety_NullColors_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PaletteEvaluator.EvaluateColorBlindSafety(null!));
    }
}
