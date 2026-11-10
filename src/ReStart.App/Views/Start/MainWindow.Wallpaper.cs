using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using ReStart.App.Services;

namespace ReStart.App;

public partial class MainWindow
{
    private DesktopWallpaperSource.Snapshot _wallpaperSnapshot = DesktopWallpaperSource.Empty;
    private int _wallpaperRevision;
    private Task? _openingWallpaperRefresh;
    private bool _openingWallpaperRequested;
    private Func<bool, DesktopWallpaperSource.Snapshot>? _openingWallpaperFixture;
    private System.Drawing.Rectangle? _wallpaperMonitor;
    private DispatcherTimer? _appearanceRefresh;
    private bool _refreshWebPresentations;

    private void WatchDesktopAppearance()
    {
        if (_preview) return;
        InitializeAppearanceRefresh();
        SystemEvents.UserPreferenceChanged += OnDesktopAppearanceChanged;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        SystemEvents.TimeChanged += OnSystemTimeChanged;
    }

    private void OnDesktopAppearanceChanged(object sender, UserPreferenceChangedEventArgs args) => QueueAppearanceRefresh();
    private void OnDisplaySettingsChanged(object? sender, EventArgs args) => QueueAppearanceRefresh(recreateWebPresentations: true);

    /// <summary>Invalidates browser composition surfaces after the window changes its actual display scale.</summary>
    /// <param name="oldDpi">Previous device-independent-to-physical pixel scale.</param>
    /// <param name="newDpi">New device-independent-to-physical pixel scale supplied by WPF.</param>
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        QueueCompactRefit();
        QueueAppearanceRefresh(recreateWebPresentations: true);
    }

    private void InitializeAppearanceRefresh()
    {
        if (_appearanceRefresh != null) return;
        _appearanceRefresh = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(200) };
        _appearanceRefresh.Tick += (_, _) =>
        {
            // Finish the user's current placement/name transaction before
            // replacing any browser that belongs to its retained visual tree.
            if (_refreshWebPresentations && (_dragging || _editingGroupHeader?.IsEditing == true)) return;
            _appearanceRefresh.Stop();
            if (_exit) return;
            if (_refreshWebPresentations) { _refreshWebPresentations = false; ResetHtmlPresentations(); }
            _wallpaperSnapshot = DesktopWallpaperSource.Empty; RefreshLayout();
        };
    }
    private void OnSystemTimeChanged(object? sender, EventArgs args) => Dispatcher.BeginInvoke(async () =>
    {
        if (_exit) return;
        TimeZoneInfo.ClearCachedData();
        // Workers cache their process-local time zone too. Retire them while
        // retaining tile JSON state so Date uses the newly selected PC zone.
        PauseScripts();
        foreach (var runtime in _scriptSessions.Values) runtime.NextUpdate = DateTimeOffset.MinValue;
        UpdateLiveTiles(); await UpdateScriptTilesAsync();
    });
    private void QueueAppearanceRefresh(bool recreateWebPresentations = false) => Dispatcher.BeginInvoke(() =>
    {
        if (_exit || _appearanceRefresh == null) return;
        _refreshWebPresentations |= recreateWebPresentations;
        _appearanceRefresh.Stop(); _appearanceRefresh.Start();
    });

    private void StopWatchingDesktopAppearance()
    {
        SystemEvents.UserPreferenceChanged -= OnDesktopAppearanceChanged;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        SystemEvents.TimeChanged -= OnSystemTimeChanged;
        _appearanceRefresh?.Stop();
    }

    private void RefreshWallpaper(string? fixturePath = null)
    {
        _wallpaperRevision++;
        var read = WallpaperRead(fixturePath);
        ApplyWallpaper(DesktopWallpaperSource.Read(read.Enabled, read.Path, read.Monitor, _wallpaperSnapshot));
    }

    private (bool Enabled, string? Path, System.Drawing.Rectangle Monitor) WallpaperRead(string? fixturePath = null)
    {
        var monitor = _wallpaperMonitor ?? System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
        if (HasTransparentCompactBackground) return (false, null, monitor);
        var selected = fixturePath ?? (DisplayPreferences.UseCustomBackground ? new BackgroundImageStore(Path.GetDirectoryName(_store.FilePath)!).Resolve(DisplayPreferences.CustomBackgroundId) : null);
        return (DisplayPreferences.ShowWallpaper && !SystemParameters.HighContrast && (!_preview || _openingWallpaperFixture != null || fixturePath != null || DisplayPreferences.UseCustomBackground)
            && (!DisplayPreferences.UseCustomBackground || selected != null), selected, monitor);
    }

    private void ApplyWallpaper(DesktopWallpaperSource.Snapshot snapshot)
    {
        _wallpaperSnapshot = snapshot; Wallpaper.Source = snapshot.Image;
    }

    private void QueueOpeningWallpaperRefresh()
    {
        _wallpaperRevision++;
        _openingWallpaperRequested = true;
        _openingWallpaperRefresh ??= RefreshOpeningWallpaperAsync();
    }

    private async Task RefreshOpeningWallpaperAsync()
    {
        await Dispatcher.Yield(DispatcherPriority.Background);
        try
        {
            while (_openingWallpaperRequested && !_exit)
            {
                _openingWallpaperRequested = false;
                var revision = _wallpaperRevision;
                var read = WallpaperRead(); var previous = _wallpaperSnapshot; var fixture = _openingWallpaperFixture;
                // Shell COM, file metadata and image decoding can stall after a
                // long idle period. Keep the previous image and accept input
                // while one coalesced background read refreshes it.
                var snapshot = await Task.Run(() => fixture != null ? fixture(read.Enabled) : DesktopWallpaperSource.Read(read.Enabled, read.Path, read.Monitor, previous));
                if (!_exit && revision == _wallpaperRevision) ApplyWallpaper(snapshot);
            }
        }
        finally { _openingWallpaperRefresh = null; }
    }
}
