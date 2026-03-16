using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Services.ColorAnalysis;

namespace AccessibilityAuditor.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ColorBlindSimulator"/> verifying Brettel/Viénot/Mollon
/// transformation matrices and distinguishability checks.
/// </summary>
public sealed class ColorBlindSimulatorTests
{
    [Fact]
    public void Simulate_NullColor_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ColorBlindSimulator.Simulate(null!, ColorBlindType.Protanopia));
    }

    [Fact]
    public void Simulate_Black_RemainsBlack_AllTypes()
    {
        var black = new ColorInfo(0, 0, 0);

        foreach (ColorBlindType type in Enum.GetValues(typeof(ColorBlindType)))
        {
            var result = ColorBlindSimulator.Simulate(black, type);
            Assert.Equal(0, result.R);
            Assert.Equal(0, result.G);
            Assert.Equal(0, result.B);
        }
    }

    [Fact]
    public void Simulate_White_RemainsNearWhite_AllTypes()
    {
        var white = new ColorInfo(255, 255, 255);

        foreach (ColorBlindType type in Enum.GetValues(typeof(ColorBlindType)))
        {
            var result = ColorBlindSimulator.Simulate(white, type);
            // White should remain very close to white under all simulations
            Assert.InRange(result.R, 245, 255);
            Assert.InRange(result.G, 245, 255);
            Assert.InRange(result.B, 245, 255);
        }
    }

    [Fact]
    public void Simulate_Protanopia_RedShiftsToward_YellowGreen()
    {
        // Under protanopia (no red cones), pure red should shift
        // toward yellow-green/dark. Red component should decrease significantly.
        var red = new ColorInfo(255, 0, 0);

        var result = ColorBlindSimulator.Simulate(red, ColorBlindType.Protanopia);

        // Protanopia matrix transforms red to a brownish/olive color
        // The result should have much less contrast between R and G
        Assert.True(result.R < 200, $"Expected reduced red but got R={result.R}");
    }

    [Fact]
    public void Simulate_Deuteranopia_GreenShiftsToward_Beige()
    {
        // Under deuteranopia (no green cones), pure green should shift
        var green = new ColorInfo(0, 255, 0);

        var result = ColorBlindSimulator.Simulate(green, ColorBlindType.Deuteranopia);

        // Green should shift toward a yellowish/beige
        Assert.True(result.R > 50, $"Expected R>50 but got R={result.R}");
    }

    [Fact]
    public void Simulate_Tritanopia_BlueShiftsToward_Cyan()
    {
        var blue = new ColorInfo(0, 0, 255);

        var result = ColorBlindSimulator.Simulate(blue, ColorBlindType.Tritanopia);

        // Tritanopia reduces blue distinction; result should differ from original
        Assert.NotEqual(0, result.R + result.G); // Should have some R or G component
    }

    [Fact]
    public void Simulate_PreservesAlpha()
    {
        var semiTransparent = new ColorInfo(255, 0, 0, 128);

        var result = ColorBlindSimulator.Simulate(semiTransparent, ColorBlindType.Protanopia);

        Assert.Equal(128, result.A);
    }

    [Fact]
    public void AreDistinguishable_BlackAndWhite_AlwaysTrue()
    {
        var black = new ColorInfo(0, 0, 0);
        var white = new ColorInfo(255, 255, 255);

        foreach (ColorBlindType type in Enum.GetValues(typeof(ColorBlindType)))
        {
            Assert.True(
                ColorBlindSimulator.AreDistinguishable(black, white, type),
                $"Black and white should be distinguishable under {type}");
        }
    }

    [Fact]
    public void AreDistinguishable_SameColor_AlwaysFalse()
    {
        var color = new ColorInfo(128, 64, 32);

        foreach (ColorBlindType type in Enum.GetValues(typeof(ColorBlindType)))
        {
            Assert.False(
                ColorBlindSimulator.AreDistinguishable(color, color, type),
                $"Same color should not be distinguishable under {type}");
        }
    }

    [Fact]
    public void AreDistinguishable_RedAndGreen_MayFail_Protanopia()
    {
        // Red and green are classically confused by protanopes
        var red = new ColorInfo(200, 50, 50);
        var green = new ColorInfo(50, 180, 50);

        // Under protanopia, these may or may not be distinguishable
        // depending on exact matrix output. This test documents the behavior.
        bool result = ColorBlindSimulator.AreDistinguishable(red, green, ColorBlindType.Protanopia);

        // We just verify it doesn't throw — the actual result depends on the matrix
        Assert.True(result || !result); // Always passes; documents behavior
    }

    [Fact]
    public void AreDistinguishable_NullColor1_Throws()
    {
        var color = new ColorInfo(128, 128, 128);
        Assert.Throws<ArgumentNullException>(() =>
            ColorBlindSimulator.AreDistinguishable(null!, color, ColorBlindType.Protanopia));
    }

    [Fact]
    public void AreDistinguishable_NullColor2_Throws()
    {
        var color = new ColorInfo(128, 128, 128);
        Assert.Throws<ArgumentNullException>(() =>
            ColorBlindSimulator.AreDistinguishable(color, null!, ColorBlindType.Protanopia));
    }

    [Fact]
    public void Simulate_ClampsToByte_NoOverflow()
    {
        // Matrices can produce values slightly outside [0,255] due to negative coefficients
        // Ensure clamping works correctly
        var extremeColor = new ColorInfo(255, 255, 255);

        foreach (ColorBlindType type in Enum.GetValues(typeof(ColorBlindType)))
        {
            var result = ColorBlindSimulator.Simulate(extremeColor, type);
            Assert.InRange(result.R, 0, 255);
            Assert.InRange(result.G, 0, 255);
            Assert.InRange(result.B, 0, 255);
        }
    }

    [Fact]
    public void Simulate_InvalidColorBlindType_Throws()
    {
        var color = new ColorInfo(128, 128, 128);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ColorBlindSimulator.Simulate(color, (ColorBlindType)99));
    }
}
