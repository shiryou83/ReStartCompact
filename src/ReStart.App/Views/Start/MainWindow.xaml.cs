using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using ReStart.App.Controls;
using ReStart.App.Services;
using ReStart.Core;

namespace ReStart.App;

/// <summary>Hosts the full-screen launcher and coordinates local models, native input, and tile rendering.</summary>
public partial class MainWindow : Window
{
    private readonly LayoutStore _store;
    private readonly bool _preview;
    private readonly AppCatalog _catalog;
    private readonly HashSet<string> _previewAppIcons = [];
    private readonly Func<Task<List<AppEntry>>> _loadCatalog;
    private readonly bool _catalogFixture;
    private readonly Action<AppEntry>? _activationFixture;
    private readonly DispatcherTimer _clock;
    private readonly List<(TextBlock Text, Tile Tile)> _liveLabels = [];
    private readonly Dictionary<string, FrameworkElement> _sections = [];
    private List<AppEntry> _apps = [];
    private bool _allApps, _dragging, _ready;
    private Point _dragOrigin;
    private Point _dragOffset;
    private GroupHeader? _editingGroupHeader;
    private bool _groupsRenderPending;
    private DropAdorner? _dropAdorner;
    private int _dialogDepth;
    private Tile? _openFolder;
    private bool _exit;
    private bool _searchRequested;
    private bool _focusOnActivation;
    private bool _verifyNativeActivation;
    private System.Drawing.Point? _startButtonPoint;
    private Task? _initialization;
    /// <summary>Gets the current local layout document.</summary>
    public LayoutDocument Document { get; private set; } = new();
    /// <summary>Gets or sets the callback that reapplies shell integration after loading or saving preferences.</summary>
    public Action? PreferencesChanged { get; set; }
    /// <summary>Gets whether initial catalog loading has completed, including a reported failure.</summary>
    public bool IsReady => _ready;
    /// <summary>Gets whether hooks, startup registration, and real user data are isolated for verification.</summary>
    public bool IsPreview => _preview;
    /// <summary>Gets whether background icon enumeration has completed.</summary>
    public bool IconsReady { get; private set; }
    /// <summary>Gets the number of real applications enumerated for diagnostics.</summary>
    public int CatalogCount => _catalog.Count;
    /// <summary>Gets the number of logical Start-menu directory paths found for all-apps diagnostics.</summary>
    public int CatalogFolderCount => _catalog.FolderCount;
    /// <summary>Gets the number of icons loaded for diagnostics.</summary>
    public int IconCount => _catalog.Icons.Count;
    /// <summary>Creates a localized default layout from the currently loaded app catalog.</summary>
    /// <returns>A new seed layout selected by installed app roles, with fresh offline bundled packages filling sparse groups; does not replace or save the current document.</returns>
    public LayoutDocument CreateInitialLayout() => SeedLayout.Create(_apps, _scriptPackages);

    /// <summary>Hosts the full-screen launcher and coordinates local models, native input, and tile rendering.</summary>
    /// <param name="store">Local layout store used for loading, migration and subsequent saves.</param>
    /// <param name="preview">Whether to isolate preview behavior and avoid real hooks, startup entries and user-layout writes.</param>
    public MainWindow(LayoutStore store, bool preview) : this(store, preview, null) { }

    internal MainWindow(LayoutStore store, bool preview, Func<Task<List<AppEntry>>>? catalogLoader, Action<AppEntry>? activation = null)
    {
        if (!preview && (catalogLoader != null || activation != null)) throw new ArgumentException("Application fixtures require preview mode.", nameof(catalogLoader));
        _store = store;
        _preview = preview;
        _startupComplete = preview;
        _catalog = new(Path.Combine(Path.GetDirectoryName(store.FilePath)!, "icon-cache"));
        _catalogFixture = catalogLoader != null;
        _activationFixture = activation;
        _loadCatalog = catalogLoader ?? _catalog.LoadAsync;
        _scriptPackages = new(Path.GetDirectoryName(store.FilePath)!);
        IsVisibleChanged += (_, _) => { if (!IsVisible) { PauseLegacyTiles(); PauseScripts(keepWorkers: true); } };
        InitializeComponent();
        InstallGroupBoundaries();
        InstallPlacementMotion();
        InstallNavigationBehavior();
        InstallTouchDrag();
        ThemeWindow.Attach(this);
        WatchDesktopAppearance();
        Activated += (_, _) =>
        {
            if (!_focusOnActivation) return;
            Dispatcher.BeginInvoke(() =>
            {
                if (IsVisible && IsActive && _focusOnActivation && _dialogDepth == 0)
                {
                    if (Keyboard.FocusedElement == null || Keyboard.FocusedElement == this || SearchBox.IsKeyboardFocused) FocusSearchInput();
                    _focusOnActivation = false;
                }
            }, DispatcherPriority.Input);
        };
        PinnedNav.AllowDrop = true;
        PinnedNav.PreviewDragOver += (_, e) => { if (HasDrag(e)) { ShowPinned(this, new()); e.Effects = DragDropEffects.Move; e.Handled = true; } };
        ContentScroll.AllowDrop = true;
        ContentScroll.DragLeave += (_, e) =>
        {
            if (new Rect(new Point(), ContentScroll.RenderSize).Contains(e.GetPosition(ContentScroll))) return;
            _folderHover.Reset(); StopFolderDwell(); _dragScrolling = false; ClearDropPreview();
        };
        ContentScroll.PreviewMouseWheel += OnContentMouseWheel;
        GroupsPanel.PreviewDragOver += OnBoardDragOver;
        GroupsPanel.PreviewDrop += OnBoardDrop;
        GroupsPanel.RequestBringIntoView += (_, e) => { if (_dragging) e.Handled = true; };
        ContentScroll.PreviewDragOver += (_, e) => TrackDragScrolling(e);
        UserRailLabel.Label = _preview ? "ReStart" : Environment.UserName;
        PreviewMouseDown += (_, _) => CompleteEntrance();
        IsVisibleChanged += (_, _) => { if (!IsVisible) { _touchPanning.Release(); StopMotion(); StopDragGhost(); ClearPlacementMotion(); SetLayoutEditing(false); } };
        Loaded += async (_, _) => await InitializeAsync();
        Closed += (_, _) => _artworkCancellation?.Cancel();
        SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(this).Handle;
            var square = 1;
            Native.DwmSetWindowAttribute(handle, 33, ref square, sizeof(int));
            InitializeCompactFrame(handle);
        };
        SizeChanged += (_, _) => FitPanels();
        ContentArea.SizeChanged += (_, _) => FitPanels();
        ContentScroll.SizeChanged += (_, _) => FitPanels();
        _clock = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(250) };
        _clock.Tick += async (_, _) => { UpdateLiveTiles(); if (IsVisible) RequestCatalogRefresh(); if (!_startupPreparing) await UpdateScriptTilesAsync(); };
        _clock.Start();
    }

    /// <summary>Initializes layout and catalog once; background startup may call this before showing a window.</summary>
    /// <returns>The shared initialization task, completing after catalog, initial views and icon loading finish; foreground presentation performs additional tile preparation.</returns>
    public Task InitializeAsync() => _initialization ??= InitializeCoreAsync();
    internal bool DeferLayoutSaves { get; set; }
    internal bool InitializedSuccessfully { get; private set; }

    private async Task InitializeCoreAsync()
    {
        try
        {
            Document = _store.Load() ?? new();
            ApplyAppearance();
            MarkStartup("preferences");
            // Preferences and the bare-Windows-key hook must not wait for shell
            // enumeration. Input is accepted while the catalog is still loading.
            PreferencesChanged?.Invoke();
            _apps = await _loadCatalog();
            MarkStartup("catalog");
            _catalogRefreshedAt = Environment.TickCount64;
            ReportStartup(K.StartupIcons, .4);
            var actualApps = _apps;
            if (_preview && !_catalogFixture)
            {
                var previews = PreviewApps();
                _apps = previews;
                Document.Preferences.ShowWallpaper = false;
            }
            if (Document.Groups.Count == 0 && !File.Exists(_store.FilePath)) Document = CreateInitialLayout();
            if (_preview) Document.Preferences.ShowWallpaper = false;
            ApplyAppearance();
            RenderGroups();
            RenderApps();
            _layoutHistory.Reset(Document);
            _historyReady = true;
            SetStatus(_store.RecoveryMessage ?? L.F(K.StatusCatalogReady, _apps.Count));
            _ready = true;
            MarkStartup("views");
            if (ScriptTileCount == 0) MarkStartup("tiles");
            StartStartupTileWarmup();
            UpdateEditingChrome();
            RefreshSearchResults();
            PreferencesChanged?.Invoke();
            if (IsActive && (Keyboard.FocusedElement == null || Keyboard.FocusedElement == this || SearchBox.IsKeyboardFocused)) FocusSearchInput();
            var pinnedIds = Document.Groups.SelectMany(g => g.Tiles).SelectMany(t => new[] { t }.Concat(t.Children)).Select(t => t.AppId).ToHashSet();
            var pinnedFiles = Document.Groups.SelectMany(g => g.Tiles).SelectMany(t => new[] { t }.Concat(t.Children))
                .Where(t => !t.IsFolder && t.Widget.Length == 0 && !t.IsShellApp)
                .Select(t => new AppEntry(t.AppId, t.Label, t.Target, false));
            await _catalog.LoadIconsAsync(actualApps.Concat(pinnedFiles).DistinctBy(a => a.Id, StringComparer.OrdinalIgnoreCase).OrderByDescending(a => pinnedIds.Contains(a.Id)));
            if (_preview)
            {
                foreach (var app in _apps)
                {
                    var actual = PreviewCatalog.Match(app, actualApps);
                    if (actual != null && _catalog.Icons.TryGetValue(actual.Id, out var icon))
                    { _catalog.Icons[app.Id] = icon; _previewAppIcons.Add(app.Id); }
                }
            }
            IconsReady = true;
            MarkStartup("icons");
            RenderGroups();
            if (_artworkReadTask != null) await _artworkReadTask;
            RenderApps();
            RefreshSearchResults();
            if (_store.RecoveryMessage == null) SetStatus("");
            if (!_preview && !_store.IsReadOnly) SaveLayout();
            MarkStartup("complete");
            InitializedSuccessfully = true;
            if (!_preview) _ = InitializeUpdateStateAsync();
        }
        catch (Exception ex)
        {
            SetStatus(L.T(K.ErrorCatalogLoad) + ex.Message);
            _ready = true;
            IconsReady = true;
            RefreshSearchResults();
        }
    }

    private static List<AppEntry> PreviewApps() =>
    [
        new("edge", "Microsoft Edge", "https://www.microsoft.com/edge", false),
        new("mail", L.T(K.AppMail), "mailto:", false), new("photos", L.T(K.AppPhotos), "ms-photos:", false),
        new("store", "Microsoft Store", "ms-windows-store:", false), new("notepad", L.T(K.AppNotepad), "notepad.exe", false),
        new("calc", L.T(K.AppCalculator), "calc.exe", false), new("paint", L.T(K.AppPaint), "mspaint.exe", false),
        new("terminal", "Windows Terminal", "wt.exe", false), new("code", "Visual Studio Code", "https://code.visualstudio.com", false)
    ];

    internal void ToggleFromStartButton(Native.Point point)
    {
        _startButtonPoint = new(point.X, point.Y);
        try { Toggle(); }
        finally { _startButtonPoint = null; }
    }

    /// <summary>Hides the active launcher or presents the existing window.</summary>
    public void Toggle()
    {
        if (_startupWindow != null) { CancelStartupPresentation(); return; }
        if (IsVisible && IsActive && _dialogDepth == 0 && !_dismissing) Dismiss();
        else Present();
    }

    /// <summary>Returns to the pinned home view and presents the launcher; existing owned dialogs retain focus and preview windows retain their size.</summary>
    public void Present()
    {
        if (_returningFromWindowsSearch) return;
        if (!_preview && _windowsSearchHandoff.HasHandoff) { _ = PresentAfterWindowsSearchAsync(); return; }
        PresentCore();
    }

    private void PresentCore()
    {
        RefreshWindowsUsage();
        if (!_startupComplete && _startupWindow == null && IsReady && IconsReady && ScriptTileCount == 0) _startupComplete = true;
        if (!_startupComplete) { RequestStartupPresentation(); return; }
        var dialog = OwnedWindows.Cast<Window>().LastOrDefault(w => w.IsVisible);
        if (dialog != null) { dialog.Activate(); return; }
        var wasHidden = !IsVisible;
        var wasDismissing = _dismissing;
        var previousOpacity = Opacity;
        var previousScale = ContentScale.ScaleX;
        StopMotion();
        if (_expandedAppFolders.Count > 0) { _expandedAppFolders.Clear(); RenderApps(); }
        ResetToPinnedView();
        var handle = new WindowInteropHelper(this).EnsureHandle();
        _focusOnActivation = true;
        if (!_preview) TaskbarNativeBackend.PrepareLauncher(handle, Document.Preferences.ShowTaskbarWhileOpen);
        if (wasHidden && MotionEnabled) Opacity = EntranceOpacity;
        if (UsesNativeStartBounds)
        {
            var screen = System.Windows.Forms.Screen.FromPoint(_startButtonPoint ?? System.Windows.Forms.Cursor.Position);
            _wallpaperMonitor = screen.Bounds;
            // Place the hidden HWND first so visibility observers do not acquire
            // a taskbar on the previous display during a cross-monitor opening.
            PlaceStartWindow(handle, screen, new nint(-1), 0x0010, opening: true);
        }
        if (!IsVisible) Show();
        if (!_preview || _openingWallpaperFixture != null) QueueOpeningWallpaperRefresh();
        if (!_preview || _verifyNativeActivation) ForegroundActivation.Request(handle);
        Activate();
        ExpandedRail.Visibility = Visibility.Collapsed;
        if (wasHidden || wasDismissing)
            BeginMotion(false, wasHidden ? EntranceOpacity : previousOpacity, wasHidden ? 0.9 : previousScale);
        FocusSearchInput();
        QueueTileResume();
        RequestCatalogRefresh(opening: true);
        if (_windowsSearchQueryUnavailable) { _windowsSearchQueryUnavailable = false; SetStatus(L.T(K.WindowsSearchQueryUnavailable)); }
        Dispatcher.BeginInvoke(() =>
        {
            if (IsActive && _dialogDepth == 0 && SearchBox.Text.Length == 0)
            {
                if (Keyboard.FocusedElement == null || Keyboard.FocusedElement == this || SearchBox.IsKeyboardFocused) FocusSearchInput();
                _focusOnActivation = false;
            }
        }, DispatcherPriority.Input);
    }

    private void RefreshBackgroundMenu()
    {
        var backgroundMenu = CreateContextMenu();
        Add(backgroundMenu, L.T(K.NewGroup), AddGroup);
        Add(backgroundMenu, L.T(K.PinApp), () => ShowAllApps(this, new()));
        Add(backgroundMenu, L.T(K.AddTile), () => OpenAddMenu(AddButton, new()));
        AddLiveTileMenu(backgroundMenu);
        Add(backgroundMenu, L.T(K.SettingsTitle), ShowSettings);
        AddLayoutEditingMenu(backgroundMenu);
        Root.ContextMenu = backgroundMenu;
    }

    /// <summary>Applies language, theme, contrast, wallpaper, and shared control resources from local preferences.</summary>
    public void ApplyAppearance()
    {
        foreach (var view in _htmlTiles.Values) view.InvalidateRetainedPresentation();
        var p = DisplayPreferences;
        RespectMotionPreference();
        LocalizationResources.Apply(p.Language);
        if (_scriptLanguage != L.Language)
        {
            _scriptLanguage = L.Language; PauseScripts(); _scriptFrames.Clear();
            foreach (var runtime in _scriptSessions.Values) runtime.NextUpdate = DateTimeOffset.MinValue;
        }
        Language = System.Windows.Markup.XmlLanguage.GetLanguage(L.Culture.IetfLanguageTag);
        RefreshBackgroundMenu();
        if (_preview) Title = L.T(K.PreviewTitle);
        var systemLight = ThemeWindow.SystemIsLight;
        var light = p.Theme == ReStart.Core.ThemeMode.Light || p.Theme == ReStart.Core.ThemeMode.System && systemLight;
        var palette = ThemePresets.Resolve(p, systemLight);
        var highContrast = SystemParameters.HighContrast;
        var background = highContrast ? SystemColors.WindowColor : light ? Color.FromRgb(238, 238, 238) : Color.FromRgb(31, 31, 31);
        var customBackground = !highContrast && ThemePresets.IsColor(palette.Background);
        if (customBackground) { background = Brush(palette.Background, "#1F1F1F").Color; light = ReadableInk(background) == Brushes.Black; }
        var foreground = highContrast ? SystemColors.WindowTextColor : light ? Colors.Black : Colors.White;
        var opposite = light ? Colors.White : Colors.Black;
        Application.Current.Resources["TextBrush"] = new SolidColorBrush(foreground);
        Application.Current.Resources["MutedBrush"] = new SolidColorBrush(highContrast ? foreground : customBackground ? EnsureContrast(Mix(background, foreground, .72), background, foreground, 4.5) : light ? Color.FromRgb(70, 70, 70) : Color.FromRgb(195, 195, 195));
        Application.Current.Resources["PanelBrush"] = new SolidColorBrush(background);
        Application.Current.Resources["TileBrush"] = new SolidColorBrush(highContrast ? background : customBackground ? Mix(background, opposite, .12) : light ? Color.FromRgb(218, 218, 218) : Color.FromRgb(58, 58, 58));
        var accentColor = highContrast ? SystemColors.HighlightColor : Brush(palette.Accent, "#0078D7").Color;
        Application.Current.Resources["AccentBrush"] = new SolidColorBrush(accentColor);
        Application.Current.Resources["AccentTextBrush"] = highContrast ? SystemColors.HighlightTextBrush : ReadableInk(accentColor);
        var focusAccent = highContrast ? SystemColors.HighlightColor : VisibleAccent(accentColor, background, foreground);
        Application.Current.Resources["FocusAccentBrush"] = new SolidColorBrush(focusAccent);
        Application.Current.Resources["SearchSelectionBrush"] = new SolidColorBrush(highContrast ? SystemColors.HighlightColor
            : Mix(background, focusAccent, .25));
        Application.Current.Resources["SearchSelectionTextBrush"] = new SolidColorBrush(highContrast ? SystemColors.HighlightTextColor : foreground);
        Application.Current.Resources["ControlBrush"] = new SolidColorBrush(highContrast ? background : customBackground ? Mix(background, opposite, .06) : light ? Colors.White : Color.FromRgb(43, 43, 43));
        Application.Current.Resources["BorderBrush"] = new SolidColorBrush(highContrast ? foreground : customBackground ? VisibleAccent(Mix(background, foreground, .42), background, foreground) : light ? Color.FromRgb(135, 135, 135) : Color.FromRgb(112, 112, 112));
        Application.Current.Resources["HoverBrush"] = new SolidColorBrush(highContrast ? background : customBackground ? Mix(background, opposite, .18) : light ? Color.FromRgb(225, 225, 225) : Color.FromRgb(70, 70, 70));
        Application.Current.Resources["ScrollThumbBrush"] = new SolidColorBrush(highContrast ? foreground : customBackground ? VisibleAccent(Mix(background, foreground, .45), background, foreground) : light ? Color.FromRgb(135, 135, 135) : Color.FromRgb(115, 115, 115));
        var startBackground = background;
        if (p.ShowAccentOnStart && !light && !highContrast && !customBackground)
        {
            var accent = Brush(p.Accent, "#0078D7").Color;
            // Use a darkened accent surface with uniform translucent tile plates.
            // Exact Windows 10 acrylic composition remains a documented approximation.
            startBackground = Color.FromRgb((byte)(accent.R * 0.65), (byte)(accent.G * 0.65), (byte)(accent.B * 0.65));
        }
        Application.Current.Resources["StartTileBrush"] = new SolidColorBrush(highContrast ? background
            : customBackground ? Mix(background, opposite, .12) : light ? Color.FromArgb(20, 0, 0, 0) : Color.FromArgb(32, 255, 255, 255));
        ThemeWindow.IsLight = light;
        if (highContrast || p.BackgroundBlur <= 0) Wallpaper.Effect = null;
        else if (Wallpaper.Effect is not System.Windows.Media.Effects.BlurEffect blur || Math.Abs(blur.Radius - p.BackgroundBlur) > .01)
            Wallpaper.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = p.BackgroundBlur, RenderingBias = System.Windows.Media.Effects.RenderingBias.Performance };
        foreach (Window window in Application.Current.Windows) ThemeWindow.Apply(window);
        Tint.Background = new SolidColorBrush(Color.FromArgb((byte)(highContrast ? 255 : 255 * p.BackgroundOpacity), startBackground.R, startBackground.G, startBackground.B));
        Root.Background = new SolidColorBrush(startBackground);
        UpdateSearchChrome(SearchBox.Text.Trim().Length > 0 || _searchRequested);
        RefreshWallpaper();
        RefreshNavigationShortcuts();
        RefreshPreparedUpdateNotice();
    }

    /// <summary>Reapplies preferences and rebuilds visible groups and app views without changing saved labels.</summary>
    public void RefreshLayout()
    {
        if (Document.Preferences.LayoutLocked) SetLayoutEditing(false);
        ApplyAppearance();
        RenderGroups();
        RenderApps();
        SearchChanged(SearchBox, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
        if (UsesNativeStartBounds && IsVisible && WantsCompact != _compactFrame) RefitNativeBounds();
    }

    private static SolidColorBrush Brush(string color, string fallback)
    {
        try { return new((Color)ColorConverter.ConvertFromString(color)); }
        catch (FormatException) { return new((Color)ColorConverter.ConvertFromString(fallback)); }
    }

    /// <summary>Persists the current document and reports recoverable write errors in the status area.</summary>
    public void SaveLayout()
    {
        TrySaveLayout();
    }

    private bool TrySaveLayout(bool recordHistory = true)
    {
        if (DeferLayoutSaves) return false;
        try
        {
            if (_historyReady && recordHistory)
            {
                var previous = new LayoutDocument(); _layoutHistory.RestoreCurrent(previous);
                if (!LayoutLocks.AllowsChange(previous.Groups, Document.Groups, Document.Preferences, GroupsPanel.Columns))
                {
                    _layoutHistory.RestoreCurrent(Document); Dispatcher.BeginInvoke(RenderGroups);
                    SetStatus(L.T(K.LayoutLockedStatus)); return false;
                }
            }
            _store.Save(Document);
            _selectionPlanCache = null;
            if (_historyReady && recordHistory) _layoutHistory.Record(Document);
            UpdateEditingChrome();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            if (_historyReady && recordHistory) _layoutHistory.RestoreCurrent(Document);
            Dispatcher.BeginInvoke(RenderGroups);
            SetStatus(L.T(K.ErrorSave) + ex.Message);
            return false;
        }
    }

    private void RenderGroups()
    {
        _boardRenderCount++;
        if (_editingGroupHeader?.IsEditing == true) { _groupsRenderPending = true; return; }
        var previousPositions = CapturePlacementMotion();
        ClearPlacementMotion();
        _groupsRenderPending = false;
        _scriptPresentationGeneration++;
        foreach (var runtime in _scriptSessions.Values) runtime.NextUpdate = DateTimeOffset.MinValue;
        if (_openFolder != null && (!_openFolder.IsFolder || !Document.Groups.Any(g => g.Tiles.Contains(_openFolder)))) _openFolder = null;
        GroupsPanel.ExpandedFolderId = _openFolder?.Id;
        RememberChangingHtmlPresentations();
        GroupsPanel.Children.Clear();
        _selectionViews.Clear();
        _liveLabels.Clear();
        _legacyViews.Clear();
        _artworkViews.Clear();
        _scriptViews.Clear();
        _usedHtmlTiles.Clear();
        _folderThumbnailIds.Clear();
        var activeScriptIds = Document.Groups.SelectMany(g => g.Tiles).SelectMany(t => new[] { t }.Concat(t.Children)).Where(t => t.Widget == "script").Select(t => t.Id).ToHashSet();
        foreach (var id in _scriptSessions.Keys.Where(id => !activeScriptIds.Contains(id)).ToArray()) _scriptSessions.Remove(id);
        foreach (var id in _scriptFrames.Keys.Where(id => !activeScriptIds.Contains(id)).ToArray()) _scriptFrames.Remove(id);
        foreach (var id in _slideshowSources.Keys.Where(id => !activeScriptIds.Contains(id)).ToArray()) { _slideshowSources.Remove(id); _slideshowImages.Remove(id); }
        foreach (var group in Document.Groups)
        {
            var placements = TileLayout.Pack(group.Tiles, DisplayPreferences.Columns);
            var expansion = FolderExpansion.Create(placements, DisplayPreferences.Columns, _openFolder?.Id);
            var compact = group.IsStandalone && placements.Count == 1 && expansion == null;
            var columns = compact ? placements[0].Width : expansion?.WidthInCells(placements) ?? Math.Max(1, placements.Count == 0 ? 0 : placements.Max(p => p.Column + p.Width));
            var stack = new StackPanel { Background = Brushes.Transparent, Width = columns * Cell, Tag = group };
            if (!group.IsStandalone)
            {
                var header = new GroupHeader(group.Name) { Tag = group };
                header.IsPlacementLocked = Document.Preferences.LayoutLocked || Document.Preferences.LockedGroupIds.Contains(group.Id);
                header.Handle.Tag = group;
                AutomationProperties.SetName(header.Handle, L.T(K.GroupPrefix) + group.Name);
                WireInlineName(header, name =>
                {
                    group.Name = name;
                    AutomationProperties.SetName(header.Handle, L.T(K.GroupPrefix) + name);
                }, () => GroupsPanel.Children.OfType<StackPanel>().Where(s => s.Tag == group).SelectMany(s => s.Children.OfType<GroupHeader>()).FirstOrDefault());
                header.Handle.ContextMenu = GroupMenu(group);
                WireDrag(header.Handle, "ReStart.Group", group.Id);
                stack.Children.Add(header);
                stack.ContextMenu = GroupMenu(group);
            }
            var panel = new TilePanel { Tag = group, Columns = columns, Cell = Cell, Placements = compact ? [placements[0] with { Column = 0, Row = 0 }] : placements, Expansion = expansion };
            foreach (var placement in panel.Placements) panel.Children.Add(CreateTileButton(placement.Tile));
            if (panel.Expansion != null) panel.Children.Add(CreateInlineFolder(panel));
            stack.Children.Add(panel);
            GroupsPanel.Children.Add(stack);
        }
        if (Document.Groups.Count == 0)
        {
            // Main content starts 20 DIPs below the rail: 28 + 48/2 aligns
            // this action with the 72-DIP centre of the Pinned tiles row.
            var button = new Button
            {
                Content = new TextBlock { Text = L.T(K.EmptyGroups), TextWrapping = TextWrapping.Wrap },
                Margin = new(0, 28, 0, 0),
                MinHeight = 48,
                MaxWidth = 440,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                IsEnabled = CanEditLayout
            };
            AutomationProperties.SetName(button, L.T(K.EmptyGroups));
            button.Click += (_, _) => AddGroup();
            GroupsPanel.Children.Add(button);
        }
        RetireHtmlTiles();
        RefreshTileArtwork();
        UpdateLiveTiles();
        FitPanels();
        RefreshEntranceTargets();
        RefreshTileSelection();
        ShowGroupBoundaries(_layoutEditing || _dragging);
        RestorePlacementMotion(previousPositions);
    }

    private double Cell => 52 * DisplayPreferences.TileScale;

    private void FitPanels()
    {
        if (CollapsedFolderScroll != null)
        {
            // Four top actions, three fixed footer actions and the footer/gap margins.
            var available = Math.Max(0, ContentArea.ActualHeight - (4 * 48 + 3 * 48 + 24));
            CollapsedFolderScroll.MaxHeight = available; ExpandedFolderScroll.MaxHeight = available;
        }
        if (ContentScroll.ActualWidth < 1) return;
        GroupsPanel.TileColumns = DisplayPreferences.Columns;
        GroupsPanel.Cell = Cell;
        GroupsPanel.InvalidateMeasure();
    }

    private Button CreateTileButton(Tile tile, bool studioPreview = false, bool folderThumbnail = false)
    {
        var webTile = tile.Widget == "script" && tile.Live && _scriptPackages.IsWeb(tile.ScriptId);
        var nativeLive = Document.Preferences.LegacyUwpLiveTiles && tile.Live && LegacyEligible(tile);
        var displayName = TileDisplayName(tile);
        var button = new TileSurfaceButton { MotionAllowed = MotionEnabled, HoverHighlightEnabled = DisplayPreferences.TileHoverHighlight, Tag = tile, Padding = new(0), HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch, ToolTip = displayName };
        AutomationProperties.SetName(button, displayName + (tile.IsFolder && tile.Label.Length > 0 ? L.T(K.TileFolderSuffix) : ""));
        var colorful = tile.UseCustomColor || DisplayPreferences.ColoredTiles || tile.Widget.Length > 0;
        button.Background = colorful && !SystemParameters.HighContrast
            ? (tile.UseCustomColor || tile.Widget.Length > 0) && tile.Color.Length > 0 ? Brush(tile.Color, "#0078D7") : (System.Windows.Media.Brush)FindResource("AccentBrush")
            : (System.Windows.Media.Brush)FindResource("StartTileBrush");
        if (colorful && !SystemParameters.HighContrast && button.Background is SolidColorBrush plate)
        {
            static double Linear(byte value) => value / 255d <= 0.04045 ? value / 255d / 12.92 : Math.Pow((value / 255d + 0.055) / 1.055, 2.4);
            var luminance = 0.2126 * Linear(plate.Color.R) + 0.7152 * Linear(plate.Color.G) + 0.0722 * Linear(plate.Color.B);
            button.Foreground = luminance > 0.179 ? Brushes.Black : Brushes.White;
        }
        var grid = new Grid { Margin = new(webTile ? 0 : tile.Widget == "script" ? tile.Size == TileSize.Small ? 3 * DisplayPreferences.TileScale : 5 : 7) };
        if (tile.IsFolder)
        {
            if (_openFolder == tile)
            {
                button.SetResourceReference(BorderBrushProperty, SystemParameters.HighContrast ? "TextBrush" : "AccentBrush");
                button.BorderThickness = new(2);
                grid.Children.Add(new TextBlock { Text = "\uE70E", FontFamily = new("Segoe MDL2 Assets"), FontSize = 22, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
            }
            else
            {
                grid.Children.Add(CreateFolderPreview(tile));
            }
        }
        else if (webTile)
        {
            var footprint = (tile.Size, DisplayPreferences.TileScale);
            if (!_htmlFootprints.TryGetValue(tile.Id, out var previous) || previous != footprint) _scriptFrames.Remove(tile.Id);
            _htmlFootprints[tile.Id] = footprint;
            button.BorderThickness = new(0);
            button.Background = tile.UseCustomColor ? button.Background : Brushes.Transparent;
            grid.Children.Add(HtmlView(tile, folderThumbnail));
            var body = new StackPanel { Opacity = 0, IsHitTestVisible = false, Width = 0, Height = 0 };
            grid.Children.Add(body); _scriptViews.Add((tile, body, button));
        }
        else if (tile.Widget == "script" && tile.Live)
        {
            grid.Children.Add(new SlideshowTileView { IsHitTestVisible = false, Margin = new(-5) });
            var plot = new Grid { Margin = tile.Size == TileSize.Small ? new(0) : new(5, 2, 5, DisplayPreferences.ShowLiveTileNames ? 20 : 0), ClipToBounds = true };
            plot.Children.Add(new ScriptTileArt { IsHitTestVisible = false });
            var body = new StackPanel { VerticalAlignment = VerticalAlignment.Center, ClipToBounds = true };
            plot.Children.Add(body); grid.Children.Add(plot);
            if (!studioPreview || folderThumbnail) _scriptViews.Add((tile, body, button));
            if ((!studioPreview || folderThumbnail) && _scriptFrames.TryGetValue(tile.Id, out var cached)) ApplyScriptFrame(body, button, tile, cached);
        }
        else if (tile.Widget.Length > 0 && tile.Live && !(tile.Widget == "clock" && tile.Size == TileSize.Small))
        {
            var live = new TextBlock { FontWeight = FontWeights.Light, FontSize = tile.Size == TileSize.Small ? 17 : tile.Widget == "clock" ? 31 : 21, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left, Margin = new(5, 0, 0, tile.Size == TileSize.Small || !DisplayPreferences.ShowLiveTileNames ? 0 : 16) };
            _liveLabels.Add((live, tile));
            grid.Children.Add(live);
        }
        else
        {
            var (width, height) = TileLayout.Dimensions(tile.Size);
            var available = Math.Min(width * Cell - 4, height * Cell - 4) - 14 - (tile.Size == TileSize.Small ? 0 : 8);
            var icon = CreateIcon(tile, Math.Max(1, folderThumbnail ? available : Math.Min(available, tile.Size == TileSize.Small ? 23 : tile.Size == TileSize.Large ? 60 : 40)));
            var surface = ArtworkSurface(tile, button, icon);
            grid.Children.Add(surface);
            if (nativeLive && (!studioPreview || folderThumbnail))
            {
                var native = new LegacyTileView { IsHitTestVisible = false, Focusable = false };
                grid.Children.Add(native); _legacyViews.Add((tile, native, surface, button));
            }
        }
        if (tile.Size != TileSize.Small && !tile.IsFolder && !webTile && (tile.Widget.Length == 0 && !nativeLive || DisplayPreferences.ShowLiveTileNames))
            grid.Children.Add(new TextBlock { Text = displayName, Tag = tile, FontSize = 12, VerticalAlignment = VerticalAlignment.Bottom, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new(2, 0, 1, 0) });
        button.Content = grid;
        if (studioPreview) return button;
        InstallTileSelection(button, grid, tile);
        button.ContextMenu = TileMenu(tile);
        Point? clickPosition = null;
        button.PreviewMouseLeftButtonDown += (_, e) => clickPosition = e.GetPosition(button);
        button.PreviewKeyDown += (_, _) => clickPosition = null;
        button.Click += async (_, _) =>
        {
            if (_dragging || _dismissing) return;
            if (_layoutEditing) { ToggleTileSelection(tile.Id); return; }
            if (tile.Widget == "script") { InteractWithScriptTile(tile, clickPosition); clickPosition = null; }
            else if (tile.IsFolder) OpenFolder(tile);
            else await LaunchTileAsync(tile);
        };
        WireDrag(button, "ReStart.Tile", tile.Id);
        return button;
    }

    private FrameworkElement CreateIcon(Tile tile, double size)
    {
        if (_catalog.Icons.TryGetValue(tile.AppId, out var source))
        {
            var image = new Image { Source = source, Width = size, Height = size, Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new(0, 0, 0, tile.Size == TileSize.Small ? 0 : 8) };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality); return image;
        }
        var glyph = tile.Widget == "calendar" ? "\uE787" : tile.Widget == "clock" ? "\uE823" : tile.AppId == "restart:explorer" ? "\uE8B7" : tile.AppId == "restart:settings" || tile.AppId.StartsWith("restart:setting:", StringComparison.Ordinal) || tile.Target.StartsWith("ms-settings:", StringComparison.OrdinalIgnoreCase) ? "\uE713" : "\uE71D";
        return new TextBlock { Text = glyph, FontFamily = new("Segoe MDL2 Assets"), FontSize = size * 0.8, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new(0, 0, 0, tile.Size == TileSize.Small ? 0 : 8) };
    }

    private void UpdateLiveTiles()
    {
        RefreshLegacyTiles();
        var now = _preview && _documentationNow is { } sampleTime ? sampleTime.DateTime : DateTime.Now;
        foreach (var (text, tile) in _liveLabels)
            text.Text = tile.Widget == "clock" ? now.ToString("HH:mm") : tile.Size == TileSize.Small ? now.Day.ToString(CultureInfo.CurrentCulture) : now.ToString("m", L.Culture) + "\n" + now.ToString("dddd", L.Culture);
    }

    private ContextMenu TileMenu(Tile tile)
    {
        var menu = CreateContextMenu();
        if (tile.IsFolder) Add(menu, L.T(K.OpenTileFolder), () => OpenFolder(tile));
        Add(menu, L.T(K.SelectTileForEditing), () => SelectTileForEditing(tile.Id)).IsEnabled = CanEditTile(tile.Id);
        Add(menu, L.T(K.UnpinTile), () => { RemoveTile(tile.Id); CommitLayout(); }).IsEnabled = CanEditTile(tile.Id);
        var resize = new MenuItem { Header = L.T(K.ResizeTile), IsEnabled = CanEditTile(tile.Id) };
        foreach (var (size, label) in new[] { (TileSize.Small, L.T(K.TileSmall)), (TileSize.Medium, L.T(K.TileMedium)), (TileSize.Wide, L.T(K.TileWide)), (TileSize.Large, L.T(K.TileLarge)) })
        {
            var item = new MenuItem { Header = label, IsCheckable = true, IsChecked = tile.Size == size };
            item.Click += (_, _) => { tile.Size = size; CommitLayout(); };
            resize.Items.Add(item);
        }
        menu.Items.Add(resize);
        if (!tile.IsFolder && tile.Widget.Length == 0 && tile.Target.Length > 0)
            AddAppShellItems(menu, _apps.FirstOrDefault(a => a.Id == tile.AppId) ?? new(tile.AppId, tile.Label, tile.Target, tile.IsShellApp));
        var extras = new MenuItem { Header = "ReStart" };
        if (!tile.IsFolder && tile.Widget.Length == 0 && tile.Target.Length > 0)
        {
            var launchOptions = new MenuItem { Header = L.T(K.LaunchOptions) };
            launchOptions.Click += async (_, _) => await EditLaunchOptionsAsync(tile);
            extras.Items.Add(launchOptions);
        }
        extras.Items.Add(TileColorMenu(tile));
        Add(extras, L.T(K.RenameTile), () =>
        {
            if (tile.IsFolder) { RenameFolder(tile); return; }
            var value = Prompt(L.T(K.PromptTileName), TileDisplayName(tile));
            if (value != null && value != TileDisplayName(tile)) { tile.Label = value; tile.UseLocalizedName = false; CommitLayout(); }
        });
        if (tile.Widget.Length > 0 || Document.Preferences.LegacyUwpLiveTiles && LegacyEligible(tile))
        {
            var liveMenu = new MenuItem { Header = L.T(K.MenuMore), Tag = "live-more" };
            Add(liveMenu, tile.Live ? L.T(K.DisableLiveTile) : L.T(K.EnableLiveTile), () => { tile.Live = !tile.Live; CommitLayout(); }).Tag = "toggle-live";
            menu.Items.Add(liveMenu);
        }
        if (tile.Widget == "script") AddScriptMenu(menu, tile);
        var move = new MenuItem { Header = L.T(K.MoveToGroup), IsEnabled = CanEditTile(tile.Id) };
        foreach (var group in Document.Groups.Where(g => !g.IsStandalone))
        {
            var item = new MenuItem { Header = group.Name, IsEnabled = CanEditGroup(group) };
            item.Click += (_, _) => { LayoutEditor.Move(Document, tile, group); CommitLayout(); };
            move.Items.Add(item);
        }
        extras.Items.Add(move);
        if (!tile.IsFolder)
        {
            var folders = new MenuItem { Header = L.T(K.CreateFolder) };
            foreach (var target in Document.Groups.SelectMany(g => g.Tiles).Where(t => t.Id != tile.Id && t.Widget.Length == 0))
            {
                var item = new MenuItem { Header = TileDisplayName(target), IsEnabled = CanEditTile(target.Id) };
                item.Click += (_, _) => { MergeFolder(tile, target); CommitLayout(); };
                folders.Items.Add(item);
            }
            folders.IsEnabled = folders.Items.Count > 0 && CanEditTile(tile.Id);
            extras.Items.Add(folders);
        }
        else Add(extras, L.T(K.UnpackFolder), () =>
        {
            LayoutEditor.UnpackFolder(Document, tile);
            CommitLayout();
        }).IsEnabled = CanEditTile(tile.Id);
        menu.Items.Add(extras);
        AddLayoutEditingMenu(menu);
        return menu;
    }

    private ContextMenu GroupMenu(TileGroup group)
    {
        var menu = CreateContextMenu();
        AddLiveTileMenu(menu);
        Add(menu, L.T(K.RenameGroup), () => RenameGroup(group));
        Add(menu, L.T(K.UnpinGroupFromStart), () =>
        {
            if (Confirm(L.F(K.ConfirmDeleteGroup, group.Name)))
            { Document.Groups.Remove(group); CommitLayout(); }
        }).IsEnabled = CanEditGroup(group);
        AddGroupLockMenu(menu, group);
        AddLayoutEditingMenu(menu);
        return menu;
    }

    private static MenuItem Add(ItemsControl menu, string label, Action action)
    {
        var item = new MenuItem { Header = label };
        item.Click += (_, _) => action();
        menu.Items.Add(item); return item;
    }

    private void BeginDrag(Button button, MouseEventArgs e, string format, string id)
    {
        if (_touchInput != null) { _touchInput.MouseMoves++; if (e.LeftButton == MouseButtonState.Pressed) _touchInput.PressedMouseMoves++; }
        if (e.LeftButton != MouseButtonState.Pressed || _dragging || !CanEditLayout
            || TouchesOver.Skip(1).Any()
            || format == "ReStart.Group" && Document.Groups.FirstOrDefault(g => g.Id == id) is { } dragGroup && !CanEditGroup(dragGroup)
            || format == "ReStart.Tile" && !CanEditTile(id)) return;
        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _dragOrigin.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(point.Y - _dragOrigin.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _dragging = true;
        if (_touchInput != null) _touchInput.DragStarts++;
        _freePlacementGesture = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
        _reorderGesture = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        ShowGroupBoundaries(true);
        _folderHover.Reset();
        if (format == "ReStart.Group" && _openFolder != null) { CloseFolder(this, new()); UpdateLayout(); }
        if (_layoutEditing && format == "ReStart.Tile")
        {
            if (!_selectedTileIds.Contains(id)) { _selectedTileIds.Clear(); _selectedTileIds.Add(id); RefreshTileSelection(); }
            if (_selectedTileIds.Count > 1)
            {
                UpdateLayout();
                _selectionDragPositions = CaptureTileSelection();
                format = "ReStart.Selection";
            }
        }
        PreserveDragExtent();
        _activeDragFormat = format; _activeDragId = id;
        button.ReleaseMouseCapture();
        e.Handled = true;
        var previousView = new DragViewState(_allApps, _searchRequested, SearchBox.Text, ContentScroll.VerticalOffset, ContentScroll.HorizontalOffset);
        try
        {
            var grab = TranslatePoint(_dragOrigin, Root);
            // Closing an expanded folder can move a group's header before OLE
            // begins. Keep the original handle-relative grab, not its old row.
            if (format == "ReStart.Group" && GroupsPanel.Children.OfType<StackPanel>().FirstOrDefault(view => view.Tag is TileGroup group && group.Id == id)
                ?.Children.OfType<GroupHeader>().FirstOrDefault() is { } header) grab = header.Handle.TranslatePoint(_dragOffset, Root);
            StartDragGhost(button, format, id, grab);
            MoveDragGhost(TranslatePoint(point, Root));
            previousView = PrepareDragView(format);
            // A list/search row can disappear when switching to tiles. Keep the
            // OLE drag source attached to the window for the whole operation.
            var result = DragDrop.DoDragDrop(this, new DataObject(format, id), DragDropEffects.Move);
            if (_touchInput != null) { if (result == DragDropEffects.None) _touchInput.CancelledDrags++; else _touchInput.AcceptedDrops++; }
            if (result == DragDropEffects.None) RestoreDragView(previousView);
        }
        finally
        {
            _touchPanning.Release();
            StopDragGhost();
            _dragScrolling = false; _activeDragFormat = null; _activeDragId = null;
            _freePlacementGesture = false; _reorderGesture = false;
            ShowGroupBoundaries(_layoutEditing);
            ClearDropPreview();
            _folderHover.Reset();
            StopFolderDwell();
            _selectionDragPositions = null;
            _selectionPlanCache = null;
            GroupsPanel.DragMinimumHeight = 0;
            GroupsPanel.DragMinimumWidth = 0;
            GroupsPanel.InvalidateMeasure();
            Dispatcher.BeginInvoke(() => _dragging = false, DispatcherPriority.Background);
        }
    }

    private void WireDrag(Button button, string format, string id)
    {
        WireTouchDrag(button, format, id);
        button.PreviewMouseLeftButtonDown += (_, e) =>
        {
            _dragOrigin = e.GetPosition(this);
            _dragOffset = format == "ReStart.App" ? new Point(Cell, Cell) : e.GetPosition(button);
        };
        // ButtonBase consumes bubbling mouse movement; listen in the tunneling phase, including handled events.
        button.AddHandler(Mouse.PreviewMouseMoveEvent, new MouseEventHandler((_, e) => BeginDrag(button, e, format, id)), true);
    }

    private static bool HasDrag(DragEventArgs e) => e.Data.GetDataPresent("ReStart.Tile") || e.Data.GetDataPresent("ReStart.App") || e.Data.GetDataPresent("ReStart.Group") || e.Data.GetDataPresent("ReStart.Selection");
    private Tile? DragTile(DragEventArgs e) => e.Data.GetData("ReStart.Tile") is string id ? FindTile(id) :
        e.Data.GetData("ReStart.App") is string appId ? TileForAppDrag(appId) : null;
    private Tile? TileForAppDrag(string appId) => _apps.Concat(_fileSearchResults).Concat(SettingsSearch.Find(SearchBox.Text)).FirstOrDefault(a => a.Id == appId) is AppEntry app
        ? AppPinning.Find(Document, app) ?? Tile.FromApp(app) : null;
    private void ShowDropPreview(TilePanel panel, Tile tile, int col, int row)
    {
        if (_dropAdorner?.AdornedElement != panel)
        {
            ClearDropPreview();
            _dropAdorner = new(panel) { IsHitTestVisible = false, ShowHints = Document.Preferences.ShowDropHints };
            AdornerLayer.GetAdornerLayer(panel)?.Add(_dropAdorner);
        }
        var (width, height) = TileLayout.Dimensions(tile.Size);
        var y = row * Cell + (panel.Expansion != null && row >= panel.Expansion.Row ? panel.Expansion.ExtraHeight(Cell) : 0);
        _dropAdorner.CellBounds = new(col * Cell, y, width * Cell - 4, height * Cell - 4);
        _dropAdorner.SelectionBounds = null;
        _dropAdorner.FolderTarget = false;
        _dropAdorner.HoverProgress = 0;
        _dropAdorner.GroupTarget = false;
        _dropAdorner.GroupBounds = panel.FolderOwner == null && panel.Tag is TileGroup { IsStandalone: false }
            ? new Rect(0, -GroupLayout.HeaderHeight, Math.Max(panel.ActualWidth, (col + width) * Cell), Math.Max(panel.ActualHeight, y + height * Cell) + GroupLayout.HeaderHeight) : null;
        _dropAdorner.Caption = panel.FolderOwner != null ? L.T(K.DropIntoFolder) + " · " + TileDisplayName(panel.FolderOwner) : panel.Tag is TileGroup group && !group.IsStandalone
            ? L.F(K.DropIntoGroup, group.Name.Length == 0 ? L.T(K.UnnamedGroup) : group.Name) : "";
        _dropAdorner.InvalidateVisual();
    }
    private void ClearDropPreview()
    {
        RestoreDropProjection(_dragging && MotionEnabled); _dropPlanKey = null; _dropPlan = null;
        if (GroupsPanel.DragPlacements != null) { GroupsPanel.DragPlacements = null; GroupsPanel.InvalidateMeasure(); }
        if (_dropAdorner == null) return;
        AdornerLayer.GetAdornerLayer(_dropAdorner.AdornedElement)?.Remove(_dropAdorner);
        _dropAdorner = null;
    }

    private static T? Ancestor<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source != null) { if (source is T target) return target; source = source is Visual ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source); }
        return null;
    }

    private Tile? FindTile(string id) => LayoutEditor.Find(Document, id);
    private void RemoveTile(string id)
    {
        var owner = Document.Groups.FirstOrDefault(g => g.Tiles.Any(t => t.Id == id || t.Children.Any(c => c.Id == id)));
        LayoutEditor.Remove(Document, id);
        if (owner?.Tiles.Count == 0) Document.Groups.Remove(owner);
    }
    private void MergeFolder(Tile source, Tile target) => LayoutEditor.MergeFolder(Document, source, target);

    private void CommitLayout()
    {
        SaveLayout();
        if (_openFolder != null)
        {
            var current = FindTile(_openFolder.Id);
            _openFolder = current?.IsFolder == true ? current : null;
        }
        RenderGroups();
    }

    private Button AppButton(AppEntry app, bool search = false)
    {
        var button = new Button { Tag = app, HorizontalContentAlignment = HorizontalAlignment.Stretch, Padding = new(4, 3, 4, 3), Height = 44, ToolTip = app.Name };
        AutomationProperties.SetName(button, app.Name);
        var panel = new DockPanel();
        var iconBox = new Border { Width = 32, Height = 32, Margin = new(0, 0, 12, 0), Background = DisplayPreferences.ColoredTiles ? (System.Windows.Media.Brush)FindResource("AccentBrush") : Brushes.Transparent };
        var icon = CreateIcon(Tile.FromApp(app), 25);
        icon.Margin = new(0);
        iconBox.Child = icon;
        DockPanel.SetDock(iconBox, Dock.Left);
        panel.Children.Add(iconBox);
        panel.Children.Add(new TextBlock { Text = app.Name, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center });
        button.Content = panel;
        button.Click += (_, _) => { if (!_dragging) Launch(app); };
        button.ContextMenu = CreateAppMenu(app, search);
        WireDrag(button, "ReStart.App", app.Id);
        return button;
    }

    private void SearchChanged(object sender, TextChangedEventArgs e)
    {
        if (SearchPlaceholder == null) return;
        var query = SearchBox.Text.Trim();
        SearchPlaceholder.Visibility = query.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        var searching = query.Length > 0;
        UpdateSearchChrome(searching || _searchRequested);
        ResultsPanel.Visibility = searching ? Visibility.Visible : Visibility.Collapsed;
        GroupsPanel.Visibility = !searching && !_allApps ? Visibility.Visible : Visibility.Collapsed;
        AppsPanel.Visibility = !searching && _allApps ? Visibility.Visible : Visibility.Collapsed;
        ViewTitle.Text = searching ? L.T(K.SearchTitle) : _allApps ? L.T(K.NavAllApps) : L.T(K.NavStart);
        AddButton.Visibility = searching || _allApps ? Visibility.Collapsed : Visibility.Visible;
        if (!_allApps && !searching) PinnedNav.SetResourceReference(BackgroundProperty, "TileBrush"); else PinnedNav.Background = Brushes.Transparent;
        if (_allApps && !searching) AppsNav.SetResourceReference(BackgroundProperty, "TileBrush"); else AppsNav.Background = Brushes.Transparent;
        RefreshSearchResults();
        if (!searching) return;
        ContentScroll.ScrollToTop();
        ContentScroll.ScrollToLeftEnd();
    }

    private void ResetToPinnedView()
    {
        _editingGroupHeader?.EndEdit(true);
        _allApps = false;
        _searchRequested = false;
        SearchBox.Clear();
        // Clear() raises no event for an already-empty input, so always refresh the view.
        SearchChanged(SearchBox, new(TextBox.TextChangedEvent, UndoAction.None));
        ExpandedRail.Visibility = Visibility.Collapsed;
        AlphabetOverlay.Visibility = Visibility.Collapsed;
        CloseFolder(this, new());
        ClearDropPreview();
        ContentScroll.ScrollToTop();
    }
    private void ShowPinned(object sender, RoutedEventArgs e)
    {
        // Dragging a selection over the navigation item must retain its source
        // selection. An ordinary navigation click finishes selection immediately.
        if (!_dragging) SetLayoutEditing(false);
        ResetToPinnedView();
    }
    private void ShowAllApps(object sender, RoutedEventArgs e) { SetLayoutEditing(false); _searchRequested = false; _allApps = true; SearchBox.Clear(); SearchChanged(SearchBox, new(TextBox.TextChangedEvent, UndoAction.None)); ExpandedRail.Visibility = Visibility.Collapsed; ContentScroll.ScrollToTop(); }
    private void ClearSearch(object sender, RoutedEventArgs e) { SearchBox.Clear(); SearchBox.Focus(); }
    private void ToggleRail(object sender, RoutedEventArgs e) => SetRailOpen(ExpandedRail.Visibility != Visibility.Visible, explicitToggle: true);
    private void RenameGroup(TileGroup group) => GroupsPanel.Children.OfType<StackPanel>().Where(s => s.Tag == group).SelectMany(s => s.Children.OfType<GroupHeader>()).FirstOrDefault()?.BeginEdit();
    private void AddGroup()
    {
        ShowPinned(this, new());
        var group = new TileGroup { Name = "" };
        Document.Groups.Add(group);
        CommitLayout();
        RenameGroup(group);
    }

    private void OpenAddMenu(object sender, RoutedEventArgs e)
    {
        var menu = CreateContextMenu();
        Add(menu, L.T(K.NewGroup), AddGroup);
        Add(menu, L.T(K.PinApp), () => ShowAllApps(this, new()));
        Add(menu, L.T(K.PinFile), () => ChoosePinPath(false));
        Add(menu, L.T(K.PinFolder), () => ChoosePinPath(true));
        Add(menu, L.T(K.PinWebsite), () =>
        {
            var url = Prompt(L.T(K.PromptWebsiteUrl), "https://");
            if (url == null) return;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) { SetStatus(L.T(K.ErrorInvalidUrl)); return; }
            var label = Prompt(L.T(K.PromptTileName), uri.Host);
            if (label != null) PinCustom(new() { AppId = url, Label = label, Target = url, IsShellApp = false, Size = TileSize.Wide });
        });
        Add(menu, L.T(K.CreateScriptTile), CreateScriptTile);
        AddLiveTileMenu(menu);
        Add(menu, L.T(K.ImportScriptTile), ImportScriptTile);
        Add(menu, L.T(K.AddClock), () => PinCustom(new() { AppId = "restart:clock", Label = L.T(K.AppClock), Widget = "clock", Target = "ms-clock:", IsShellApp = false }));
        Add(menu, L.T(K.AddCalendar), () => PinCustom(new() { AppId = "restart:calendar", Label = L.T(K.AppCalendar), Widget = "calendar", Target = "", IsShellApp = false, Size = TileSize.Wide }));
        OpenMenu(menu, (Button)sender);
    }

    private void AddCustomTile()
    {
        var menu = CreateContextMenu();
        Add(menu, L.T(K.ChooseFile), () =>
        {
            WithDialog(() => { var dialog = new OpenFileDialog { Title = L.T(K.PromptPinFile) }; if (dialog.ShowDialog(this) == true) PinCustom(new() { AppId = dialog.FileName, Target = dialog.FileName, Label = Path.GetFileNameWithoutExtension(dialog.FileName), IsShellApp = false }); });
        });
        Add(menu, L.T(K.ChooseFolder), () =>
        {
            WithDialog(() => { var dialog = new OpenFolderDialog { Title = L.T(K.PromptPinFolder) }; if (dialog.ShowDialog(this) == true) PinCustom(new() { AppId = dialog.FolderName, Target = dialog.FolderName, Label = Path.GetFileName(dialog.FolderName), IsShellApp = false }); });
        });
        OpenMenu(menu, AddButton);
    }

    private void PinCustom(Tile tile)
    {
        if (!CanEditLayout) { SetStatus(L.T(K.LayoutLockedStatus)); return; }
        LayoutEditor.PinOnBoard(Document, tile, GroupsPanel.Columns, Cell, ContentScroll.ViewportWidth, ContentScroll.VerticalOffset);
        CommitLayout();
    }

    private void OpenAlphabet()
    {
        AlphabetButtons.Children.Clear();
        foreach (var section in _appAlphabet)
        {
            var button = new Button
            {
                Content = section,
                IsEnabled = _sections.ContainsKey(section),
                Width = 75,
                Height = 60,
                FontSize = section.Length > 3 ? 14 : section.Length > 2 ? 16 : 22,
                Padding = new(4),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            button.Click += (_, _) =>
            {
                AlphabetOverlay.Visibility = Visibility.Collapsed;
                // Icon loading can rebuild the rows while this picker is open.
                if (_sections.TryGetValue(section, out var header)) { header.BringIntoView(); header.Focus(); }
            };
            AlphabetButtons.Children.Add(button);
        }
        AlphabetOverlay.Visibility = Visibility.Visible;
        AlphabetButtons.Children.OfType<Button>().FirstOrDefault(b => b.IsEnabled)?.Focus();
    }
    private void CloseAlphabet(object sender, RoutedEventArgs e) => AlphabetOverlay.Visibility = Visibility.Collapsed;

    private void Launch(AppEntry app)
    {
        if (_dismissing || _layoutEditing) return;
        if (_preview && _activationFixture != null) { _activationFixture(app); return; }
        if (app.Id == "restart:calendar")
        {
            WithDialog(() =>
            {
                new CalendarWindow(this).ShowDialog();
            });
            return;
        }
        try { ShellLauncher.Launch(app); Activity.RecordLaunch(app.Id, Document.Preferences.ShowMostUsed, DateTimeOffset.Now); if (Document.Preferences.ShowMostUsed) SaveActivity(); Dismiss(); }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or System.IO.FileNotFoundException) { SetStatus(L.T(K.ErrorLaunch) + ex.Message); }
    }
    private void OpenDocuments(object sender, RoutedEventArgs e) => LaunchPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
    private void OpenPictures(object sender, RoutedEventArgs e) => LaunchPath(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
    private void LaunchPath(string path)
    {
        if (_preview) { Launch(new(path, path, path, false)); return; }
        // Utility folders remain available while placement editing is active.
        try { ShellLauncher.Launch(path); Dismiss(immediately: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception) { SetStatus(ex.Message); }
    }
    private void OpenAccount(object sender, RoutedEventArgs e)
    {
        OpenMenu(CreateAccountMenu(), (Button)sender);
    }
    private void OpenPower(object sender, RoutedEventArgs e)
    {
        OpenMenu(CreatePowerMenu(), (Button)sender);
    }

    private static void OpenMenu(ContextMenu menu, Button target) { menu.PlacementTarget = target; menu.IsOpen = true; }
    private SettingsWindow? _activeSettings;
    private void OpenSettings(object sender, RoutedEventArgs e) => ShowSettings();
    /// <summary>Shows the settings dialog with close-on-deactivate suspended, then reapplies saved preferences.</summary>
    public void ShowSettings() => ShowSettings(null);

    private void ShowSettings(TextKey? section)
    {
        if (_activeSettings != null)
        {
            _activeSettings.Activate();
            if (section is { } selected) _activeSettings.SelectSection(L.T(selected));
            return;
        }
        WithDialog(() =>
        {
            _activeSettings = new SettingsWindow(this, _store) { Owner = _preview ? null : this };
            if (section is { } selected) _activeSettings.SelectSection(L.T(selected));
            try { _activeSettings.ShowDialog(); } finally { _activeSettings = null; }
        });
        if (_exit) return;
        RefreshLayout();
        PreferencesChanged?.Invoke();
    }
    /// <summary>Displays a transient status or recovery message.</summary>
    private System.Windows.Threading.DispatcherTimer? _statusTimer;

    /// <summary>Displays a transient operation message, restarting its five-second lifetime on replacement.</summary>
    /// <param name="message">Localized transient status text; empty clears the current message and its expiry timer.</param>
    public void SetStatus(string message)
    {
        _statusTimer ??= CreateStatusTimer();
        _statusTimer.Stop();
        StatusText.Text = message;
        if (message.Length > 0) _statusTimer.Start();
    }

    private System.Windows.Threading.DispatcherTimer CreateStatusTimer()
    {
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timer.Tick += (_, _) => { timer.Stop(); StatusText.Text = ""; };
        Closed += (_, _) => timer.Stop();
        return timer;
    }
    /// <summary>Prepares a query for rendering verification only; this does not simulate physical keyboard input.</summary>
    /// <param name="query">Fictional preview search query applied through the normal search UI path.</param>
    public void SetSmokeSearch(string query) { SearchBox.Text = query; FocusSearchInput(); }
    /// <summary>Gets rendered app-result names for smoke diagnostics.</summary>
    public string[] SearchResultNames => SearchResults.Items.OfType<Button>().Select(AutomationProperties.GetName).ToArray();
    /// <summary>Gets whether the native text input remains in the visible WPF tree.</summary>
    public bool SearchInputInVisibleTree => SearchBox.IsVisible;
    private void WithDialog(Action action) { _dialogDepth++; try { action(); } finally { _dialogDepth--; } }
    private bool Confirm(string text)
    {
        var result = false;
        WithDialog(() => result = ConfirmationWindow.Ask(this, text));
        return result;
    }
    private string? Prompt(string title, string value)
    {
        string? answer = null;
        WithDialog(() => answer = TextPrompt.Show(this, title, value));
        return answer;
    }
    /// <summary>Atomically saves an imported/reset layout before replacing the visible document and recording one undo step.</summary>
    /// <param name="document">Replacement layout to normalize and save; failure restores the previous document and history state.</param>
    public void ReplaceDocument(LayoutDocument document)
    {
        if (!LayoutLocks.AllowsChange(Document.Groups, document.Groups, Document.Preferences, GroupsPanel.Columns)) throw new InvalidOperationException(L.T(K.LayoutLockedStatus));
        var grants = Document.Groups.SelectMany(group => group.Tiles).SelectMany(tile => new[] { tile }.Concat(tile.Children))
            .Where(tile => tile.AllowWebNetwork).Select(tile => (tile.Id, tile.ScriptId)).ToHashSet();
        foreach (var tile in document.Groups.SelectMany(group => group.Tiles).SelectMany(tile => new[] { tile }.Concat(tile.Children)))
            tile.AllowWebNetwork = grants.Contains((tile.Id, tile.ScriptId));
        _store.Save(document);
        Document = document;
        _selectionPlanCache = null;
        if (_historyReady) _layoutHistory.Record(document);
        RefreshLayout();
    }

    private void FocusSearchInput()
    {
        // Keep the native TextBox in the visible tree while its idle row is clipped to zero.
        // Native input handles IME composition, the first character, selection and backspace.
        FocusManager.SetFocusedElement(this, SearchBox);
        Keyboard.Focus(SearchBox);
    }
    private void UpdateSearchChrome(bool visible)
    {
        TopBar.Height = visible ? 52 : 0; TopBar.Opacity = visible ? 1 : 0; TopBar.IsHitTestVisible = visible;
    }
    /// <summary>Closes the menu after a taskbar click unless a dialog or drag currently owns the interaction.</summary>
    public void DismissFromTaskbar()
    {
        if (_startupWindow != null && Document.Preferences.CloseOnDeactivate) { CancelStartupPresentation(); return; }
        if (IsVisible && _dialogDepth == 0 && !_dragging && !_compactResizeStart.HasValue && Document.Preferences.CloseOnDeactivate) Dismiss();
    }
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_dismissing) { e.Handled = true; return; }
        if (_dragging) return;
        CompleteEntrance();
        if (e.Key == Key.F1 && Keyboard.Modifiers == ModifierKeys.None)
        { e.Handled = true; ShowKeyboardShortcuts(this); return; }
        if (_editingGroupHeader?.IsEditing == true && e.OriginalSource is TextBox) return;
        if (HandleLayoutEditingKey(e)) return;
        if (HandleSearchSelectionKey(e)) return;
        if (HandleWebTileScrollKey(e)) return;
        if (e.Key == Key.Tab && SearchBox.IsKeyboardFocused && SearchBox.Text.Length == 0 && !_searchRequested)
        { PinnedNav.Focus(); e.Handled = true; return; }

        if (e.Key == Key.Escape)
        {
            if (AlphabetOverlay.Visibility == Visibility.Visible) AlphabetOverlay.Visibility = Visibility.Collapsed;
            else if (_openFolder != null) CloseFolder(this, e);
            else if (ExpandedRail.Visibility == Visibility.Visible) ExpandedRail.Visibility = Visibility.Collapsed;
            else if (SearchBox.Text.Length > 0 || _searchRequested) { _searchRequested = false; SearchBox.Clear(); UpdateSearchChrome(false); FocusSearchInput(); }
            else Dismiss();
            e.Handled = true;
        }
        else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control) { _searchRequested = true; UpdateSearchChrome(true); SearchBox.Focus(); SearchBox.SelectAll(); e.Handled = true; }
        else if (e.Key == Key.Down && SearchBox.IsKeyboardFocused)
        {
            var first = _allApps ? AppsPanel.Children.OfType<Button>().FirstOrDefault(b => b.Tag is AppEntry || b is AppFolderButton)
                : GroupsPanel.Children.OfType<StackPanel>().SelectMany(p => p.Children.OfType<TilePanel>())
                    .SelectMany(p => p.Children.OfType<Button>()).FirstOrDefault();
            if (first != null) { first.Focus(); first.BringIntoView(); e.Handled = true; }
        }
    }
    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (_dismissing) { e.Handled = true; return; }
        if (_dragging) return;
        CompleteEntrance();
        if (_layoutEditing && _editingGroupHeader?.IsEditing != true) { e.Handled = true; return; }
        if (e.OriginalSource is TextBox || Ancestor<TextBox>(e.OriginalSource as DependencyObject) != null || Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || e.Text.Length == 0 || e.Text.All(char.IsControl)) return;
        if (FocusAppInitial(e.Text)) { e.Handled = true; return; }
        UpdateSearchChrome(true); SearchBox.Focus(); SearchBox.AppendText(e.Text); SearchBox.CaretIndex = SearchBox.Text.Length; e.Handled = true;
    }
    private void OnDeactivated(object? sender, EventArgs e)
    {
        _focusOnActivation = false;
        if (_compactResizeStart.HasValue) return;
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsActive && _dialogDepth == 0 && !_dragging && !_compactResizeStart.HasValue && Document.Preferences.CloseOnDeactivate && (!_preview || _verifyNativeActivation) && !IsAnyMenuOpen()) Dismiss();
        }, DispatcherPriority.Background);
    }
    private static bool IsAnyMenuOpen() => Mouse.Captured is ContextMenu || Mouse.Captured is System.Windows.Controls.Primitives.MenuBase;
    private void OnClosing(object? sender, CancelEventArgs e) { if (!_exit) { e.Cancel = true; Dismiss(); } }
    /// <summary>Stops timers and isolated script workers before permanently closing the launcher.</summary>
    public void Exit() { _exit = true; _windowsSearchHandoff.Dispose(); StopLegacyTiles(); StopWindowsUsage(); StopFileShare(); CancelStartupPresentation(); StopMotion(); StopFolderDwell(); StopDragGhost(); StopWatchingDesktopAppearance(); _clock.Stop(); _nameRefreshTimer?.Stop(); _railHoverTimer?.Stop(); _dragScrolling = false; _fileSearchCancellation?.Cancel(); _updateCancellation.Cancel(); _releaseHttp.Dispose(); PauseScripts(); RetireHtmlTiles(all: true); _liveSessions.Dispose(); Close(); }

    /// <summary>Renders only the launcher root to a PNG for reproducible UI verification.</summary>
    /// <param name="path">Destination PNG path for the current preview visual; missing parent directories are created.</param>
    public void SaveScreenshot(string path)
    {
        SaveBitmap(path, RenderScreenshot());
    }

    private BitmapSource RenderScreenshot()
    {
        UpdateLayout();
        var explicitPreviewSize = _preview && double.IsFinite(Root.Width) && double.IsFinite(Root.Height);
        if (explicitPreviewSize)
        {
            // A fixture can exceed the CI desktop. Arrange it at its own size so
            // WPF does not retain the smaller native window's layout clip.
            Root.Measure(new(Root.Width, Root.Height));
            Root.Arrange(new Rect(0, 0, Root.Width, Root.Height));
        }
        var bitmap = new RenderTargetBitmap((int)Root.ActualWidth, (int)Root.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(Root);
        if (explicitPreviewSize)
        {
            var pixel = new byte[4];
            bitmap.CopyPixels(new Int32Rect(bitmap.PixelWidth - 1, bitmap.PixelHeight - 1, 1, 1), pixel, 4, 0);
            if (pixel[3] != 255) throw new InvalidOperationException("The preview render was clipped before its bottom-right corner.");
        }
        return bitmap;
    }

    private static void SaveBitmap(string path, BitmapSource bitmap)
    {
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var stream = File.Create(path); png.Save(stream);
    }
}

// Keeps the regular WPF UniformGrid type out of generated XAML namespace ambiguities.
internal sealed class UniformGridProxy : System.Windows.Controls.Primitives.UniformGrid;
