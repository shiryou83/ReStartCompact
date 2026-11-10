using System.Text.Json;
using ReStart.Core;

namespace ReStart.Core.Tests;

public sealed class BackgroundPreferencesTests
{
    [Theory]
    [InlineData(-1, .1)]
    [InlineData(.1, .1)]
    [InlineData(.25, .25)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(double.NaN, .86)]
    [InlineData(double.PositiveInfinity, .86)]
    public void TintAllowsTenPercentAndNormalizesMalformedValues(double input, double expected)
    {
        var document = new LayoutDocument { Preferences = new() { BackgroundOpacity = input } };
        document.Normalize(); Assert.Equal(expected, document.Preferences.BackgroundOpacity);
    }
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(.4, .4)]
    [InlineData(2, 1)]
    [InlineData(double.NaN, .65)]
    public void CompactTintAllowsFullTransparencyAndKeepsItsOwnRange(double input, double expected)
    {
        var document = new LayoutDocument { Preferences = new() { CompactBackgroundOpacity = input, BackgroundOpacity = .3 } };
        document.Normalize();
        Assert.Equal(expected, document.Preferences.CompactBackgroundOpacity);
        Assert.Equal(.3, document.Preferences.BackgroundOpacity);
    }
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(12, 12)]
    [InlineData(35, 35)]
    [InlineData(50, 50)]
    [InlineData(80, 50)]
    [InlineData(double.NaN, 12)]
    [InlineData(double.NegativeInfinity, 12)]
    public void BlurCanBeDisabledAndHasAFiniteBoundedRadius(double input, double expected)
    {
        var document = new LayoutDocument { Preferences = new() { BackgroundBlur = input } };
        document.Normalize(); Assert.Equal(expected, document.Preferences.BackgroundBlur);
        var restored = JsonSerializer.Deserialize<LayoutDocument>(JsonSerializer.Serialize(document))!;
        restored.Normalize(); Assert.Equal(expected, restored.Preferences.BackgroundBlur);
    }
    [Fact]
    public void ExistingLayoutsKeepTheirTintAndGainTheLighterDefaultBlur()
    {
        var old = JsonSerializer.Deserialize<LayoutDocument>("{\"Version\":4,\"Preferences\":{\"BackgroundOpacity\":0.3},\"Groups\":[]}")!;
        old.Normalize(); Assert.Equal(.3, old.Preferences.BackgroundOpacity); Assert.Equal(12, old.Preferences.BackgroundBlur);
        Assert.Equal(4, old.Version);
    }
}
