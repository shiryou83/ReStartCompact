using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using ReStart.Core;

namespace ReStart.App;

public partial class MainWindow
{
    private bool _startupComplete, _startupPreparing, _startupWanted;
    private Task? _startupPresentation;
    private StartupWindow? _startupWindow;
    private System.Drawing.Point _startupPoint;
    private TextKey _startupStage = K.StartupApps;
    private double _startupFraction = .08;
    private readonly HashSet<string> _scriptAttempts = [];
    private Task _startupWorkerWarmup = Task.CompletedTask;
    private bool _startupSettings;
    private TextKey? _startupSettingsSection;
    internal Action? PresentationVisibilityChanged { get; set; }
    internal bool IsPresenting => IsVisible || _startupWindow?.IsVisible == true;
    internal nint PresentationHandle => new WindowInteropHelper(_startupWindow?.IsVisible == true ? _startupWindow : this).Handle;

    private void ReportStartup(TextKey stage, double fraction)
    {
        _startupStage = stage; _startupFraction = fraction;
        _startupWindow?.UpdateProgress(stage, fraction);
    }

    private void RequestStartupPresentation()
    {
        if (_startupWindow != null) { _startupWindow.Activate(); _startupWindow.Query.Focus(); return; }
        _startupWanted = true;
        _startupPoint = _startButtonPoint ?? System.Windows.Forms.Cursor.Position;
        // Loading preferences runs synchronously before the first catalog await,
        // so the splash and the Windows-key hook immediately use the saved theme.
        _ = InitializeAsync();
        _startupWindow = new(() => Document.Preferences.CloseOnDeactivate);
        var created = _startupWindow;
        // WPF's Owner property requires an already shown owner. Native ownership
        // keeps foreground dismissal correct without flashing the hidden launcher.
        new WindowInteropHelper(created).Owner = new WindowInteropHelper(this).EnsureHandle();
        created.Closed += (_, _) => { if (_startupWindow == created) { _startupWindow = null; CancelStartupPresentation(); } };
        _startupWindow.CancelRequested += CancelStartupPresentation;
        _startupWindow.UpdateProgress(_startupStage, _startupFraction);
        _startupWindow.ShowAt(_startupPoint, !_preview);
        PresentationVisibilityChanged?.Invoke();
        _startupPresentation ??= CompleteStartupPresentationAsync();
    }

    private void CancelStartupPresentation()
    {
        _startupWanted = false;
        _startupSettings = false;
        _startupSettingsSection = null;
        var window = _startupWindow; _startupWindow = null; window?.Close();
        PauseScripts(); PresentationVisibilityChanged?.Invoke();
    }

    private async Task CompleteStartupPresentationAsync()
    {
        // Always yield before assigning this task to its field, including when
        // background initialization has already completed without live tiles.
        await Dispatcher.Yield(DispatcherPriority.Background);
        try
        {
            await InitializeAsync();
            if (_exit || !_startupWanted) return;
            ReportStartup(K.StartupTiles, .8);
            _startupPreparing = true;
            SizeStartupContent();
            await _startupWorkerWarmup;
            if (!_startupWanted || _exit) return;
            var watch = Stopwatch.StartNew();
            var retryDeadline = Environment.TickCount64 + 9000;
            // Every tile gets either an initial frame or a visible error. External
            // sources retain their bounded loading/error state instead of holding
            // the entire launcher hostage to an unavailable service.
            do
            {
                await UpdateScriptTilesAsync().WaitAsync(TimeSpan.FromSeconds(12));
                if (!_startupWanted || _exit) return;
                // Initial retries share the normal backoff budget. Do not wait
                // for offscreen tiles or a retry beyond this preparation window.
                var missing = _scriptViews.Where(v => v.Tile.Live && TileInViewport(v.Button) && !_scriptFrames.ContainsKey(v.Tile.Id)
                    && !_liveSessions.Budget(v.Tile.Id).Paused && _liveSessions.Budget(v.Tile.Id).RetryAt <= retryDeadline).ToArray();
                foreach (var view in missing)
                    if (_scriptSessions.TryGetValue(view.Tile.Id, out var session) && _liveSessions.Budget(view.Tile.Id).CanRun(Environment.TickCount64))
                        session.NextUpdate = DateTimeOffset.MinValue;
                var localPending = missing.Length > 0 || _scriptViews.Any(v => v.Tile.Live && TileInViewport(v.Button) && !_scriptAttempts.Contains(v.Tile.Id));
                var samplePending = _systemMetrics != null && _systemMetrics.Latest?.NetworkReceiveBytesPerSecond == null;
                var imagesPending = _slideshowSources.Values.Any(v => !v.Source.Initialized);
                var dataPending = _tileData.Values.Any(v => v.Runtime.IsLoading);
                if (!localPending && !samplePending && !imagesPending && !dataPending) break;
                await Task.Delay(100);
            } while (watch.Elapsed < TimeSpan.FromSeconds(9));
            // Providers can finish while the first JS batch is in flight. Render
            // their settled snapshot once before exposing the prepared board.
            foreach (var session in _scriptSessions.Values) session.NextUpdate = DateTimeOffset.MinValue;
            await UpdateScriptTilesAsync().WaitAsync(TimeSpan.FromSeconds(12));
            if (!_startupWanted || _exit) return;
            _startupComplete = true;
            MarkStartup("prepared");
            ReportStartup(K.StartupReady, 1);
        }
        catch (TimeoutException)
        {
            PauseScripts();
            foreach (var view in _scriptViews.Where(v => v.Body.Children.Count == 0))
                view.Body.Children.Add(new System.Windows.Controls.TextBlock { Text = L.T(K.ErrorScript), TextWrapping = TextWrapping.Wrap });
            _startupComplete = true;
        }
        finally
        {
            _startupPreparing = false; _startupPresentation = null;
            if (!_startupWanted || _exit) PauseScripts();
        }
        if (!_startupWanted || _exit) return;
        var query = _startupWindow?.Query.Text ?? "";
        var launch = _startupWindow?.LaunchRequested == true;
        _startupWanted = false;
        var splash = _startupWindow; _startupWindow = null;
        _startButtonPoint = _startupPoint;
        try
        {
            splash?.Close();
            Present();
            SearchBox.Text = query; SearchBox.CaretIndex = query.Length;
            FocusSearchInput();
            PresentationVisibilityChanged?.Invoke();
            if (_startupSettings)
            {
                var section = _startupSettingsSection;
                _startupSettings = false; _startupSettingsSection = null; ShowSettings(section);
            }
            else if (launch && SearchResults.SelectedItem is System.Windows.Controls.Button selected) selected.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        }
        finally { _startButtonPoint = null; }
    }

    /// <summary>Opens settings after startup preparation, including when requested from the tray during loading.</summary>
    public void PresentSettings() => PresentSettings(null);

    /// <summary>Retains the requested settings section through cold preparation or activates the existing draft editor.</summary>
    /// <param name="section">Section to select, or null to retain an open editor's current section and use the default for a new editor.</param>
    private void PresentSettings(TextKey? section)
    {
        if (_activeSettings != null) { ShowSettings(section); return; }
        _startupSettings = true;
        _startupSettingsSection = section;
        Present();
        if (_startupComplete && _startupWindow == null)
        { _startupSettings = false; _startupSettingsSection = null; ShowSettings(section); }
    }

    private void SizeStartupContent()
    {
        var handle = new WindowInteropHelper(this).EnsureHandle();
        var size = new Point(Width, Height);
        if (UsesNativeStartBounds)
        {
            var screen = System.Windows.Forms.Screen.FromPoint(_startupPoint);
            _wallpaperMonitor = screen.Bounds; RefreshWallpaper();
            // Size the hidden HWND without showing it or taking focus from the splash.
            var area = PlaceStartWindow(handle, screen, 0, 0x0014, opening: true);
            var transform = HwndSource.FromHwnd(handle).CompositionTarget.TransformFromDevice;
            size = transform.Transform(new Point(area.Width, area.Height));
        }
        Root.Measure(new Size(size.X, size.Y)); Root.Arrange(new Rect(0, 0, size.X, size.Y)); Root.UpdateLayout();
        FitPanels(); Root.UpdateLayout();
    }

    private void StartStartupTileWarmup()
    {
        if (!_startupWanted || ScriptTileCount == 0) return;
        var nativeCount = 0;
        foreach (var tile in Document.Groups.SelectMany(g => g.Tiles).SelectMany(t => new[] { t }.Concat(t.Children)).Where(t => t.Widget == "script" && t.Live))
        {
            try
            {
                var manifest = _scriptPackages.Read(tile.ScriptId).Manifest;
                if (manifest.Renderer == "native") nativeCount++;
                if (manifest.Capabilities.Contains("system.metrics") && (!_preview || _verifyHostMetrics))
                    _systemMetrics ??= new();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException) { }
        }
        // HTML tiles run in WebView2. Do not start four otherwise unused Jint
        // processes merely to display an entirely browser-based home layout.
        if (nativeCount == 0) { _startupWorkerWarmup = Task.CompletedTask; return; }
        var pool = _scriptWorkerPool ??= new();
        _startupWorkerWarmup = Task.Run(() => Task.WhenAll(Enumerable.Range(0, Math.Min(4, nativeCount))
            .Select(_ => pool.RenderAsync("function render() { return { text: 'Ready' }; }", new { }))));
    }
}
