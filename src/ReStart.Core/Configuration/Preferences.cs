namespace ReStart.Core;

/// <summary>Stores local UI and integration choices; imported layouts do not replace these settings.</summary>
public sealed class Preferences
{
    internal const int DefaultCompactWidth = 800;
    internal const int DefaultCompactHeight = 640;

    /// <summary>Gets or sets the Start presentation. Existing layouts default to Fullscreen.</summary>
    public StartMenuMode StartMenuMode { get; set; }
    /// <summary>Gets or sets the last user-selected Compact width in device-independent pixels.</summary>
    public int CompactMenuWidth { get; set; } = DefaultCompactWidth;
    /// <summary>Gets or sets the last user-selected Compact height in device-independent pixels.</summary>
    public int CompactMenuHeight { get; set; } = DefaultCompactHeight;
    /// <summary>Gets or sets a legacy tablet preference retained for layout compatibility; current shell integration ignores it. False by default.</summary>
    public bool TabletFeaturesEnabled { get; set; }
    /// <summary>Gets or sets a legacy keyboard-policy preference retained for layout compatibility; current shell integration ignores it. False by default.</summary>
    public bool TabletKeyboardMeansDesktop { get; set; }
    /// <summary>Gets or sets a legacy taskbar-hide preference retained for layout compatibility; it never creates a new hide operation. False by default.</summary>
    public bool TabletHideTaskbar { get; set; }
    /// <summary>Gets or sets whether an automatically hidden taskbar is revealed while Start is open; true by default, independently of tablet opt-in.</summary>
    public bool ShowTaskbarWhileOpen { get; set; } = true;
    /// <summary>Gets or sets whether live tiles outside the scrolled viewport are suspended; defaults to true.</summary>
    public bool PauseOffscreenLiveTiles { get; set; } = true;
    /// <summary>Gets or sets whether failed live tiles receive bounded automatic recovery attempts; defaults to true.</summary>
    public bool AutoRecoverLiveTiles { get; set; } = true;
    /// <summary>Gets or sets whether tile/group positions, sizes and membership are protected from editing; launch and appearance remain available.</summary>
    public bool LayoutLocked { get; set; }
    /// <summary>Gets or sets whether live tiles show their bottom name; defaults to true. Browser packages receive the preference and a title marker contract.</summary>
    public bool ShowLiveTileNames { get; set; } = true;
    /// <summary>Gets or sets the read-only compatibility adapter for active Windows tile/badge notifications of pinned package apps. Defaults to false; it does not read toast messages, change notification registrations or download remote images.</summary>
    public bool LegacyUwpLiveTiles { get; set; }
    /// <summary>Gets or sets stable IDs of groups whose geometry and membership are protected independently of the global lock. Locks are outside undo history.</summary>
    public List<string> LockedGroupIds { get; set; } = [];
    /// <summary>Gets or sets whether search includes localized Windows settings entry points.</summary>
    public bool SearchSettings { get; set; } = true;
    /// <summary>Gets or sets whether app menus query Windows Recent/Frequent destinations; Windows history privacy settings also apply.</summary>
    public bool ShowJumpLists { get; set; } = true;
    /// <summary>Gets or sets whether app menus show tasks registered by installed applications; defaults to true and does not enable Windows document history.</summary>
    public bool ShowAppTasks { get; set; } = true;
    /// <summary>Gets or sets whether app menus show application-defined Windows Jump List categories; defaults to true and does not change registrations or document-history policy.</summary>
    public bool ShowCustomJumpLists { get; set; } = true;
    /// <summary>Gets or sets whether app menus show and edit the application's Windows-pinned destinations; defaults to true and does not change existing pins when disabled.</summary>
    public bool ShowPinnedJumpLists { get; set; } = true;
    /// <summary>Gets or sets whether dwelling over the collapsed navigation rail expands it automatically.</summary>
    public bool ExpandNavigationOnHover { get; set; } = true;
    /// <summary>Gets or sets whether tile surfaces brighten on pointer hover; keyboard focus remains visible.</summary>
    public bool TileHoverHighlight { get; set; } = true;
    /// <summary>Gets or sets whether dragging near a board edge scrolls and extends available board space.</summary>
    public bool DragEdgeScrolling { get; set; } = true;
    /// <summary>Gets or sets whether drop previews display explanatory captions and dwell progress; destination geometry remains visible.</summary>
    public bool ShowDropHints { get; set; } = true;
    /// <summary>Gets or sets whether tile drags show a noninteractive modifier-key toast for five seconds; defaults to true.</summary>
    public bool ShowDragShortcutHints { get; set; } = true;
    /// <summary>Gets or sets whether empty-space drops create independent tiles instead of unnamed groups. Defaults to false; existing independent entries are retained unchanged.</summary>
    public bool FreeTilePlacement { get; set; }
    /// <summary>Gets or sets whether overlapping independent tiles can create an ordinary group.</summary>
    public bool GroupTilesOnDrop { get; set; } = true;
    /// <summary>Gets or sets whether overlapping grouped tiles can create a tile folder; existing folders remain usable.</summary>
    public bool CreateFoldersOnDrop { get; set; } = true;
    /// <summary>Gets or sets whether hovering a normal group reveals its boundary. Edit and drag boundaries remain visible.</summary>
    public bool ShowGroupBoundariesOnHover { get; set; } = true;
    /// <summary>Gets or sets whether search includes up to fifty local Windows-indexed filesystem results.</summary>
    public bool SearchFiles { get; set; } = true;
    /// <summary>Gets or sets whether enabled file search also matches Windows-indexed document contents. Defaults to false; never enables Windows indexing or reads unindexed contents.</summary>
    public bool SearchFileContents { get; set; }
    /// <summary>Gets or sets whether file queries use the Windows Advanced Query Syntax parser and native relevance ranking. Defaults to false; unsupported or content-disabled syntax falls back to literal matching.</summary>
    public bool SearchWindowsSyntax { get; set; }
    /// <summary>Gets or sets whether query results include an explicit Windows Search handoff for additional local, Web and cloud sources. Defaults to true; typing alone never invokes Windows Search.</summary>
    public bool ShowWindowsSearchHandoff { get; set; } = true;
    /// <summary>Gets or sets whether search offers an explicit Bing action in the default browser. True by default; typing never sends a query externally.</summary>
    public bool ShowWebSearch { get; set; } = true;
    /// <summary>Gets or sets whether enabled Most used history breaks equally strong app search matches. True by default; no history is recorded or read unless its separate usage options are enabled.</summary>
    public bool PersonalizeAppSearch { get; set; } = true;
    /// <summary>Gets or sets whether apps first observed after the initial catalog appear in Recently added for seven days.</summary>
    public bool ShowRecentlyAdded { get; set; } = true;
    /// <summary>Gets or sets whether successful ReStart app launches are counted locally and shown in Most used.</summary>
    public bool ShowMostUsed { get; set; }
    /// <summary>Gets or sets whether Most used also reads existing Windows UserAssist counts for exact installed app identities. Defaults to false; Windows tracking policies are respected and no Windows history is changed or copied to disk.</summary>
    public bool UseWindowsAppHistory { get; set; }
    /// <summary>Gets or sets the optional navigation folders; unknown identifiers are ignored and null restores the default pair.</summary>
    public List<string>? NavigationFolders { get; set; }
    /// <summary>Gets or sets whether a per-user Explorer command pins files, directories and shortcuts to ReStart.</summary>
    public bool ExplorerContextMenu { get; set; } = true;
    /// <summary>Gets or sets whether one read-only update check runs at normal startup.</summary>
    public bool CheckUpdatesOnStartup { get; set; } = true;
    /// <summary>Gets or sets whether published prereleases are eligible for update notifications.</summary>
    public bool IncludePrereleaseUpdates { get; set; }
    /// <summary>Gets or sets a supported language tag or system for UI resource selection.</summary>
    public string Language { get; set; } = "system";
    /// <summary>Gets or sets whether a bare Windows-key press toggles ReStart.</summary>
    public bool CaptureWindowsKey { get; set; } = true;
    /// <summary>Gets or sets whether clicks on an identified native Windows Start button toggle ReStart.</summary>
    public bool CaptureStartButton { get; set; } = true;
    /// <summary>Gets or sets the saved startup preference; settings reconcile it with the real Run registration.</summary>
    public bool StartWithWindows { get; set; }
    /// <summary>Gets or sets whether switching apps or clicking the taskbar closes the menu.</summary>
    public bool CloseOnDeactivate { get; set; } = true;
    /// <summary>Gets or sets whether the desktop wallpaper is used as the background.</summary>
    public bool ShowWallpaper { get; set; } = true;
    /// <summary>Gets or sets whether enabled wallpaper uses a locally selected ReStart image instead of the desktop image.</summary>
    public bool UseCustomBackground { get; set; }
    /// <summary>Gets or sets the generated N-format GUID of a local background image; empty means no custom image. Source paths are never stored here.</summary>
    public string CustomBackgroundId { get; set; } = "";
    /// <summary>Gets or sets whether app tiles use classic accent-colored backgrounds.</summary>
    public bool ColoredTiles { get; set; }
    /// <summary>Gets or sets whether pinned apps use locally installed, size-qualified default-tile artwork when available. True by default; shell icons remain the fallback.</summary>
    public bool UseAppTileImages { get; set; } = true;
    /// <summary>Gets or sets whether the dark Start surface uses the selected accent color, as in late Windows 10.</summary>
    public bool ShowAccentOnStart { get; set; }
    /// <summary>Gets or sets whether showing the menu animates when system accessibility settings permit.</summary>
    public bool Animate { get; set; } = true;
    /// <summary>Gets or sets the requested application theme.</summary>
    public ThemeMode Theme { get; set; } = ThemeMode.Dark;
    /// <summary>Gets or sets the requested accent color.</summary>
    public string Accent { get; set; } = "#0078D7";
    /// <summary>Gets or sets an optional opaque RGB surface color; empty uses the selected base theme.</summary>
    public string BackgroundColor { get; set; } = "";
    /// <summary>Gets or sets the adaptive color family; empty means explicit custom RGB, while null identifies unlabelled legacy preferences.</summary>
    public string? ColorThemeId { get; set; }
    /// <summary>Gets or sets background-image tint opacity, normalized to 0.1 through 1; high contrast renders an opaque surface.</summary>
    public double BackgroundOpacity { get; set; } = 0.86;
    /// <summary>Gets or sets background-image blur radius in DIPs, normalized to 0–50. Zero keeps the image sharp; missing/nonfinite values use 12. Foreground controls are never blurred.</summary>
    public double BackgroundBlur { get; set; } = 12;
    /// <summary>Gets or sets the cell scale, normalized to 0.7 through 1.5.</summary>
    public double TileScale { get; set; } = 1;
    /// <summary>Gets or sets six or eight logical cells per tile group.</summary>
    public int Columns { get; set; } = 6;
}
