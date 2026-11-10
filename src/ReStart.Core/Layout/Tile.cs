namespace ReStart.Core;

/// <summary>Stores a persistent root tile or one-level folder child.</summary>
public sealed class Tile
{
    /// <summary>Gets or sets explicit permission for a web tile's declared network capability; false blocks browser network requests.</summary>
    public bool AllowWebNetwork { get; set; }
    /// <summary>Gets or sets a stable identifier unique within the layout.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>Gets or sets the source application identity or built-in tile identity.</summary>
    public string AppId { get; set; } = "";
    /// <summary>Gets or sets the saved fallback or custom label; explicit custom names are never translated.</summary>
    public string Label { get; set; } = "";
    /// <summary>Gets or sets whether a live tile follows its localized default name; null infers legacy defaults without changing custom names.</summary>
    public bool? UseLocalizedName { get; set; }
    /// <summary>Gets or sets the launch target. A script tile does not execute this target.</summary>
    public string Target { get; set; } = "";
    /// <summary>Gets or sets whether the launch target is an AppsFolder shell identity.</summary>
    public bool IsShellApp { get; set; } = true;
    /// <summary>Gets or sets whether this tile applies its custom arguments, working directory and elevation preference when launched.</summary>
    public bool UseLaunchOptions { get; set; }
    /// <summary>Gets or sets the additional Windows command-line argument string; empty preserves the app or shortcut default.</summary>
    public string LaunchArguments { get; set; } = "";
    /// <summary>Gets or sets an optional working directory, with environment variables expanded at launch; empty uses the Shell default.</summary>
    public string WorkingDirectory { get; set; } = "";
    /// <summary>Gets or sets whether configured launches request the app's canonical runas verb and normal Windows consent.</summary>
    public bool RunAsAdministrator { get; set; }
    /// <summary>Gets or sets the logical tile footprint.</summary>
    public TileSize Size { get; set; } = TileSize.Medium;
    /// <summary>Gets or sets an explicit logical column, or null for automatic placement.</summary>
    public int? Column { get; set; }
    /// <summary>Gets or sets an explicit logical row, or null for automatic placement.</summary>
    public int? Row { get; set; }
    /// <summary>Gets or sets the optional saved tile color.</summary>
    public string Color { get; set; } = "";
    /// <summary>Gets or sets whether an explicitly selected tile color overrides the shared theme and script background.</summary>
    public bool UseCustomColor { get; set; }
    /// <summary>Gets or sets the adaptive color override when a fixed custom color is absent.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public TileThemeColor ThemeColor { get; set; }
    /// <summary>Gets or sets a built-in widget kind; script identifies a local JavaScript package.</summary>
    public string Widget { get; set; } = "";
    /// <summary>Gets or sets an installed package identifier, never an arbitrary source-file path.</summary>
    public string ScriptId { get; set; } = "";
    /// <summary>Gets or sets whether live updates are enabled for this tile.</summary>
    public bool Live { get; set; } = true;
    /// <summary>Gets or sets one-level folder contents; nested folders are rejected.</summary>
    public List<Tile> Children { get; set; } = [];
    /// <summary>Gets whether this tile currently owns folder children.</summary>
    public bool IsFolder => Children.Count > 0;

    /// <summary>Creates a new tile preserving the source application name and launch identity.</summary>
    /// <param name="app">App entry to copy; the app entry itself is not changed.</param>
    /// <returns>A fresh tile carrying the app identity, launch target and display label.</returns>
    public static Tile FromApp(AppEntry app) => new()
    {
        AppId = app.Id,
        Label = app.Name,
        Target = app.LaunchTarget,
        IsShellApp = app.IsShellApp
    };
}
