using System.Text.Json;

namespace ReStart.Core.Tests;

public class StartPreferencesTests
{
    [Fact]
    public void OlderPreferencesKeepFunctionalDefaultsWithoutEnablingUsageRecording()
    {
        var preferences = JsonSerializer.Deserialize<Preferences>("{}")!;
        Assert.True(preferences.SearchSettings && preferences.SearchFiles && preferences.ShowJumpLists && preferences.ExpandNavigationOnHover);
        Assert.True(preferences.TileHoverHighlight && preferences.DragEdgeScrolling && preferences.ShowDropHints && preferences.ShowDragShortcutHints);
        Assert.True(preferences.GroupTilesOnDrop && preferences.CreateFoldersOnDrop && preferences.ShowGroupBoundariesOnHover && preferences.ShowRecentlyAdded);
        Assert.False(preferences.ShowMostUsed); Assert.Null(preferences.NavigationFolders);
        Assert.True(preferences.ShowAppTasks && preferences.ShowPinnedJumpLists && preferences.ShowCustomJumpLists);
        Assert.False(preferences.SearchFileContents);
        Assert.Equal(StartMenuMode.Fullscreen, preferences.StartMenuMode);
        Assert.Equal(800, preferences.CompactMenuWidth);
        Assert.Equal(640, preferences.CompactMenuHeight);
    }

    [Fact]
    public void CompactMenuRoundTripsWithItsPreferredSizeAndNormalizesInvalidValues()
    {
        var document = new LayoutDocument
        {
            Preferences = new() { StartMenuMode = StartMenuMode.Compact, CompactMenuWidth = 987, CompactMenuHeight = 543 }
        };
        var restored = JsonSerializer.Deserialize<LayoutDocument>(JsonSerializer.Serialize(document, LayoutStore.JsonOptions), LayoutStore.JsonOptions)!;
        restored.Normalize();
        Assert.Equal(StartMenuMode.Compact, restored.Preferences.StartMenuMode);
        Assert.Equal(987, restored.Preferences.CompactMenuWidth);
        Assert.Equal(543, restored.Preferences.CompactMenuHeight);

        restored.Preferences.StartMenuMode = (StartMenuMode)42;
        restored.Preferences.CompactMenuWidth = 0;
        restored.Preferences.CompactMenuHeight = -1;
        restored.Normalize();
        Assert.Equal(StartMenuMode.Fullscreen, restored.Preferences.StartMenuMode);
        Assert.Equal(800, restored.Preferences.CompactMenuWidth);
        Assert.Equal(640, restored.Preferences.CompactMenuHeight);
    }
    [Fact]
    public void NativeJumpSettingsRoundTripIndependentlyOfHistoryAndLayoutLock()
    {
        var preferences = new Preferences { ShowAppTasks = false, ShowPinnedJumpLists = true, ShowCustomJumpLists = false, ShowJumpLists = false, LayoutLocked = true, SearchFiles = true, SearchFileContents = true };
        var restored = JsonSerializer.Deserialize<Preferences>(JsonSerializer.Serialize(preferences))!;
        Assert.False(restored.ShowAppTasks); Assert.False(restored.ShowJumpLists);
        Assert.True(restored.ShowPinnedJumpLists); Assert.True(restored.LayoutLocked);
        Assert.False(restored.ShowCustomJumpLists); Assert.True(restored.SearchFiles); Assert.True(restored.SearchFileContents);
    }
    [Fact]
    public void TileOptionsRoundTripWithoutChangingTheTargetOrDefaultLaunch()
    {
        var tile = new Tile { Target = "sample.exe", LaunchArguments = "--example \"two words\"", WorkingDirectory = "%TEMP%", RunAsAdministrator = true };
        var restored = JsonSerializer.Deserialize<Tile>(JsonSerializer.Serialize(tile))!;
        Assert.False(restored.UseLaunchOptions);
        Assert.Equal(tile.Target, restored.Target); Assert.Equal(tile.LaunchArguments, restored.LaunchArguments);
        Assert.Equal(tile.WorkingDirectory, restored.WorkingDirectory); Assert.True(restored.RunAsAdministrator);
        var document = new LayoutDocument { Groups = [new() { Tiles = [restored] }] };
        document.Normalize(); Assert.Equal(tile.LaunchArguments, restored.LaunchArguments);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RejectsNulInEitherLaunchField(bool arguments)
    {
        var tile = new Tile();
        if (arguments) tile.LaunchArguments = "ab\0cd"; else tile.WorkingDirectory = "ab\0cd";
        Assert.Throws<InvalidDataException>(() => new LayoutDocument { Groups = [new() { Tiles = [tile] }] }.Normalize());
    }
}
