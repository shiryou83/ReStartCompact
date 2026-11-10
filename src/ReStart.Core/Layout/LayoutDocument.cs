namespace ReStart.Core;

/// <summary>Defines the versioned, locally persisted layout and its normalization rules.</summary>
public sealed class LayoutDocument
{
    /// <summary>Current supported layout schema version.</summary>
    public const int CurrentVersion = 4;
    /// <summary>Current supported layout schema version.</summary>
    public int Version { get; set; } = CurrentVersion;
    /// <summary>Gets the legacy version migrated by normalization; omitted from saved layouts.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int? MigratedFromVersion { get; private set; }
    /// <summary>Gets or sets the application version that last saved this document; empty denotes a legacy producer.</summary>
    public string SavedWithAppVersion { get; set; } = "";
    /// <summary>Gets or sets the last successful application/schema upgrade and its durable backup filename.</summary>
    public LayoutUpgradeInfo? LastUpgrade { get; set; }
    /// <summary>Gets or sets local UI and shell integration preferences.</summary>
    public Preferences Preferences { get; set; } = new();
    /// <summary>Gets or sets groups in display order.</summary>
    public List<TileGroup> Groups { get; set; } = [];

    /// <summary>Repairs bounded presentation values and duplicate identities; rejects unsupported schemas and nested folders.</summary>
    /// <exception cref="UnsupportedLayoutVersionException">The document schema is newer than supported or is invalid.</exception>
    /// <exception cref="InvalidDataException">The layout exceeds tile limits or contains nested folders.</exception>
    public void Normalize()
    {
        var migrateStandalone = Version is >= 1 and < 3;
        var alignStandalone = Version == 3;
        if (Version is >= 1 and < CurrentVersion) { MigratedFromVersion = Version; Version = CurrentVersion; }
        else if (Version != CurrentVersion)
            throw new UnsupportedLayoutVersionException(Version);
        Preferences ??= new();
        if (!Enum.IsDefined(Preferences.StartMenuMode)) Preferences.StartMenuMode = StartMenuMode.Fullscreen;
        if (Preferences.CompactMenuWidth <= 0) Preferences.CompactMenuWidth = ReStart.Core.Preferences.DefaultCompactWidth;
        if (Preferences.CompactMenuHeight <= 0) Preferences.CompactMenuHeight = ReStart.Core.Preferences.DefaultCompactHeight;
        Preferences.CompactBackgroundOpacity = double.IsFinite(Preferences.CompactBackgroundOpacity)
            ? Math.Clamp(Preferences.CompactBackgroundOpacity, 0, 1) : ReStart.Core.Preferences.DefaultCompactBackgroundOpacity;
        if (ReleaseVersion.Parse(SavedWithAppVersion) == null) SavedWithAppVersion = "";
        if (LastUpgrade != null && (LastUpgrade.BackupFile == null || LastUpgrade.FromAppVersion == null || LastUpgrade.ToAppVersion == null
            || LastUpgrade.BackupFile.Length > 100 || LastUpgrade.BackupFile != Path.GetFileName(LastUpgrade.BackupFile)
            || LastUpgrade.FromAppVersion.Length > 128 || LastUpgrade.ToAppVersion.Length > 128)) LastUpgrade = null;
        Groups ??= [];
        Preferences.LockedGroupIds = (Preferences.LockedGroupIds ?? []).Where(id => Groups.Any(g => g.Id == id)).Distinct(StringComparer.Ordinal).Take(2000).ToList();
        Preferences.Columns = Preferences.Columns == 8 ? 8 : 6;
        Preferences.TileScale = double.IsFinite(Preferences.TileScale) ? Math.Clamp(Preferences.TileScale, 0.7, 1.5) : 1;
        Preferences.BackgroundOpacity = double.IsFinite(Preferences.BackgroundOpacity) ? Math.Clamp(Preferences.BackgroundOpacity, 0.1, 1) : 0.86;
        Preferences.BackgroundBlur = double.IsFinite(Preferences.BackgroundBlur) ? Math.Clamp(Preferences.BackgroundBlur, 0, 50) : 12;
        if (!Enum.IsDefined(Preferences.Theme)) Preferences.Theme = ThemeMode.Dark;
        Preferences.Accent = ThemePresets.IsColor(Preferences.Accent) ? Preferences.Accent.ToUpperInvariant() : "#0078D7";
        Preferences.BackgroundColor = ThemePresets.IsColor(Preferences.BackgroundColor) ? Preferences.BackgroundColor.ToUpperInvariant() : "";
        if (!Guid.TryParseExact(Preferences.CustomBackgroundId, "N", out _)) Preferences.CustomBackgroundId = "";
        if (Preferences.ColorThemeId != null && !ThemePresets.All.Any(p => p.Id == Preferences.ColorThemeId)) Preferences.ColorThemeId = "";
        var ids = new HashSet<string>();
        foreach (var group in Groups)
        {
            if (string.IsNullOrEmpty(group.Id) || !ids.Add(group.Id))
            {
                group.Id = Guid.NewGuid().ToString("N");
                ids.Add(group.Id);
            }
            group.Name = (group.Name ?? "").Trim();
            if (group.X.HasValue && (!double.IsFinite(group.X.Value) || group.X is < 0 or > 10000)) group.X = null;
            if (group.Column is < 0 or > 63 || group.Row is < 0 or > 20000 || group.Column.HasValue != group.Row.HasValue)
            { group.Column = null; group.Row = null; }
            group.Tiles ??= [];
            NormalizeTiles(group.Tiles, ids, 0);
            if (group.IsStandalone && group.Tiles.Count != 1) group.IsStandalone = false;
            if (migrateStandalone && group.Name.Length == 0 && group.Tiles.Count == 1)
            {
                // Earlier versions represented every detached tile as an unnamed group.
                group.IsStandalone = true;
                if (group.Row.HasValue) group.Row = Math.Min(20000, group.Row.Value + 2 * (group.Tiles[0].Row ?? 0));
            }
            if (alignStandalone && group.IsStandalone && group.Row.HasValue)
                group.Row = Math.Max(0, (int)Math.Round(group.Row.Value - GroupLayout.HeaderHeight / (52 * Preferences.TileScale / 2), MidpointRounding.AwayFromZero));
            if (group.IsStandalone) { group.Name = ""; group.Tiles[0].Row = 0; }
        }
    }

    private static void NormalizeTiles(List<Tile> tiles, HashSet<string> ids, int depth)
    {
        if (tiles.Count > 2000) throw new InvalidDataException(L.T(K.ErrorTooManyTiles));
        foreach (var tile in tiles)
        {
            if (string.IsNullOrEmpty(tile.Id) || !ids.Add(tile.Id))
            {
                tile.Id = Guid.NewGuid().ToString("N");
                ids.Add(tile.Id);
            }
            tile.Label ??= L.T(K.AppFallback);
            tile.AppId ??= "";
            tile.Target ??= "";
            tile.LaunchArguments ??= "";
            tile.WorkingDirectory ??= "";
            if (tile.LaunchArguments.Length > 32760 || tile.WorkingDirectory.Length > 32760
                || tile.LaunchArguments.Contains('\0') || tile.WorkingDirectory.Contains('\0'))
                throw new InvalidDataException("Invalid tile launch options.");
            tile.Widget ??= "";
            tile.ScriptId ??= "";
            tile.Color ??= "";
            if (tile.UseCustomColor && (tile.Color.Length != 7 || tile.Color[0] != '#' || !tile.Color.Skip(1).All(Uri.IsHexDigit))) tile.UseCustomColor = false;
            if (tile.UseCustomColor || !Enum.IsDefined(tile.ThemeColor)) tile.ThemeColor = TileThemeColor.Automatic;
            tile.Children ??= [];
            if (!Enum.IsDefined(tile.Size)) tile.Size = TileSize.Medium;
            if (tile.Column is < 0 or > 15 || tile.Row is < 0 or > 200 || tile.Column.HasValue != tile.Row.HasValue)
            { tile.Column = null; tile.Row = null; }
            if (depth >= 1 && tile.Children.Count != 0)
                throw new InvalidDataException(L.T(K.ErrorNestedFolder));
            NormalizeTiles(tile.Children, ids, depth + 1);
        }
    }
}
