using System.Text.Json;
using ReStart.Core;

namespace ReStart.Core.Tests;

public sealed class TileColorTests
{
    [Fact]
    public void AnExplicitColorRoundTripsWithoutPromotingLegacyDecorativeColors()
    {
        var document = new LayoutDocument { Groups = [new() { Tiles = [new() { Color = "#107C10", UseCustomColor = true }, new() { Color = "#E81123" }] }] };
        var restored = JsonSerializer.Deserialize<LayoutDocument>(JsonSerializer.Serialize(document, LayoutStore.JsonOptions), LayoutStore.JsonOptions)!;
        restored.Normalize();
        Assert.True(restored.Groups[0].Tiles[0].UseCustomColor);
        Assert.Equal("#107C10", restored.Groups[0].Tiles[0].Color);
        Assert.False(restored.Groups[0].Tiles[1].UseCustomColor);
        Assert.Equal(TileThemeColor.Automatic, restored.Groups[0].Tiles[1].ThemeColor);
    }

    [Fact]
    public void ThemeColorsRoundTripWithFolderChildrenAndKeepFixedColorsIndependent()
    {
        var document = new LayoutDocument
        {
            Groups = [new() { Tiles = [new() { ThemeColor = TileThemeColor.Background, Children = [new() { ThemeColor = TileThemeColor.Accent }] },
                new() { UseCustomColor = true, Color = "#123456", ThemeColor = TileThemeColor.Accent }, new() { ThemeColor = (TileThemeColor)42 }] }]
        };
        var restored = JsonSerializer.Deserialize<LayoutDocument>(JsonSerializer.Serialize(document, LayoutStore.JsonOptions), LayoutStore.JsonOptions)!;
        restored.Normalize();
        var tiles = restored.Groups[0].Tiles;
        Assert.Equal(TileThemeColor.Background, tiles[0].ThemeColor);
        Assert.Equal(TileThemeColor.Accent, tiles[0].Children[0].ThemeColor);
        Assert.Equal("", tiles[0].Color);
        Assert.True(tiles[1].UseCustomColor);
        Assert.Equal("#123456", tiles[1].Color);
        Assert.Equal(TileThemeColor.Automatic, tiles[1].ThemeColor);
        Assert.Equal(TileThemeColor.Automatic, tiles[2].ThemeColor);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#FF000000")]
    [InlineData("#GG0000")]
    public void InvalidImportedOverridesFallBackToTheTheme(string color)
    {
        var tile = new Tile { Color = color, UseCustomColor = true };
        new LayoutDocument { Groups = [new() { Tiles = [tile] }] }.Normalize();
        Assert.False(tile.UseCustomColor);
    }
}
