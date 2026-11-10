using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ReStart.App.Services;
using ReStart.Core;

namespace ReStart.App;

/// <summary>Owns the single-instance lease, tray, shell integration, and application shutdown.</summary>
public partial class App : Application
{
    private SingleInstanceGate? _instance;
    private RegisteredWaitHandle? _wait;
    private StartIntegration? _integration;
    private TabletIntegration? _tabletIntegration;
    private System.Windows.Forms.NotifyIcon? _tray;
    private MainWindow? _window;
    private PinRequestChannel? _pinRequests;
    private UpdateRelease? _updateNotificationRelease;

    /// <summary>Acquires the instance lease before creating UI or registering shell hooks.</summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args is ["--taskbar-live-check", var monitorReport, var monitorText] && int.TryParse(monitorText, out var monitorIndex))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Shutdown(await TaskbarLiveCheck.RunAsync(monitorReport, monitorIndex)); return;
        }
        if (e.Args is ["--taskbar-live-check", var taskbarReport])
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Shutdown(await TaskbarLiveCheck.RunAsync(taskbarReport)); return;
        }
        if (e.Args.Contains("--taskbar-live-check")) { Shutdown(1); return; }
        if (e.Args is ["--taskbar-watchdog-fixture", var fixtureParent, var fixtureStarted, var fixtureNonce, var fixtureDirectory])
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Shutdown(await TaskbarWatchdogFixture.RecoverAsync(fixtureParent, fixtureStarted, fixtureNonce, fixtureDirectory)); return;
        }
        if (e.Args is ["--taskbar-watchdog-owner-fixture", var ownerDirectory])
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Shutdown(await TaskbarWatchdogFixture.OwnAsync(ownerDirectory)); return;
        }
        if (e.Args.Contains("--taskbar-watchdog-fixture") || e.Args.Contains("--taskbar-watchdog-owner-fixture")) { Shutdown(1); return; }
        if (e.Args is ["--taskbar-watchdog", var parentId, var parentStarted, var nonce])
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Shutdown(await TaskbarWatchdog.RunAsync(parentId, parentStarted, nonce));
            return;
        }
        if (e.Args.Contains("--taskbar-watchdog")) { Shutdown(1); return; }
        if (e.Args is ["--observe-keyboard", var keyboardReport])
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Shutdown(await KeyboardObservationWindow.RunAsync(keyboardReport));
            return;
        }
        if (e.Args.Contains("--observe-keyboard")) { Shutdown(1); return; }
        if (e.Args.Contains("--compact-test"))
        {
            Shutdown(e.Args is ["--compact-test", var compactSnapshot] ? await RunCompactCheckAsync(compactSnapshot) : 1);
            return;
        }
        if (e.Args is ["--jump-list-worker"])
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Shutdown(await NativeJumpBroker.RunWorkerAsync());
            return;
        }
        if (await HandleUpdateEntryAsync(e.Args)) return;
        if (e.Args.Contains("--render-docs") || e.Args.Contains("--render-promo") || e.Args.Contains("--render-studio") || e.Args.Contains("--verify-platform")) { await RenderDocumentationAsync(e.Args); return; }
        var preview = e.Args.Contains("--preview") || e.Args.Contains("--smoke-test");
        var smoke = e.Args.Contains("--smoke-test");
        if (preview) DispatcherUnhandledException += (_, error) =>
        {
            Console.Error.WriteLine(error.Exception);
            var index = Array.IndexOf(e.Args, "--snapshot");
            if (index >= 0 && index + 1 < e.Args.Length)
                try { File.WriteAllText(Path.ChangeExtension(e.Args[index + 1], ".json"), JsonSerializer.Serialize(new { success = false, error = error.Exception.ToString() })); }
                catch (IOException) { }
            error.Handled = true; Shutdown(1);
        };
        var sid = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        var pinIndex = Array.IndexOf(e.Args, "--pin");
        var pinPath = pinIndex >= 0 && pinIndex + 1 < e.Args.Length ? e.Args[pinIndex + 1] : null;
        if (pinIndex >= 0 && pinPath == null) { MessageBox.Show(L.T(K.ErrorPinPath), "ReStart"); Shutdown(1); return; }
        using var process = Process.GetCurrentProcess();
        var channelIdentity = sid + "." + process.SessionId + (preview ? ".Preview" : "");
        _instance = new(sid, preview);
        if (!_instance.IsPrimary)
        {
            if (pinPath != null)
            {
                try
                {
                    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var reply = await PinRequestChannel.SendAsync(channelIdentity, pinPath, deadline.Token);
                    if (!reply.Success && !preview) MessageBox.Show(reply.Message, "ReStart", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown(reply.Success ? 0 : 1);
                }
                catch (Exception ex) when (ex is IOException or OperationCanceledException or UnauthorizedAccessException)
                { if (!preview) MessageBox.Show(L.T(K.ErrorPinConnection), "ReStart", MessageBoxButton.OK, MessageBoxImage.Error); Shutdown(1); }
                return;
            }
            if (!smoke && !e.Args.Contains("--background")) _instance.NotifyExisting();
            Shutdown(smoke ? 2 : 0); return;
        }
        var data = preview ? Path.Combine(AppContext.BaseDirectory, "preview-data") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReStart");
        var store = new LayoutStore(Path.Combine(data, "layout.json"), AppBuildInfo.Version);
        if (!preview) SafeErrorLog.Prepare(data);
        _window = new MainWindow(store, preview);
        _window.DeferLayoutSaves = e.Args.Contains("--complete-update");
        MainWindow = _window;
        _pinRequests = new(channelIdentity, path => Dispatcher.InvokeAsync(() => _window.PinPathAsync(path)).Task.Unwrap());
        _wait = ThreadPool.RegisterWaitForSingleObject(_instance.Activation, (_, _) => Dispatcher.BeginInvoke(() => _window?.Present()), null, Timeout.Infinite, false);
        if (preview)
        {
            _window.Title = L.T(K.PreviewTitle);
            _window.Width = 1280; _window.Height = 800; _window.Topmost = false;
            _window.ShowInTaskbar = true; _window.WindowStyle = WindowStyle.SingleBorderWindow;
            _window.ResizeMode = ResizeMode.CanResize; _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            if (e.Args.Contains("--background") && !smoke) _ = _window.InitializeAsync();
            else _window.Show();
            if (pinPath != null) await _window.PinPathAsync(pinPath);
            if (smoke) RunSmoke(e.Args);
            return;
        }
        var handle = new WindowInteropHelper(_window).EnsureHandle();
        LegacySettingsCleanup.Run();
        _integration = new StartIntegration(handle, Dispatcher, _window.Toggle, _window.DismissFromTaskbar, _window.ToggleFromStartButton);
        _tabletIntegration = new TabletIntegration(_window, HwndSource.FromHwnd(handle));
        _window.IsVisibleChanged += (_, _) => { _integration.SetMenuOpen(_window.IsPresenting); _tabletIntegration.Refresh(); };
        _window.PresentationVisibilityChanged = () => { _integration.SetMenuOpen(_window.IsPresenting); _tabletIntegration.Refresh(); };
        _window.PreferencesChanged = () =>
        {
            _integration.Apply(_window.Document.Preferences);
            _tabletIntegration.Refresh();
            try { ExplorerPinRegistration.Current.SetEnabled(_window.Document.Preferences.ExplorerContextMenu); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
            { _window.SetStatus(L.T(K.ErrorExplorerRegistration) + ex.Message); }
            RefreshTrayLanguage();
            if (_integration.Status is string status) _window.SetStatus(status);
        };
        CreateTray();
        _window.StartupUpdateAvailable += release =>
        {
            if (_tray == null || _window.IsPresenting) return;
            _updateNotificationRelease = release;
            _tray.ShowBalloonTip(10000, "ReStart", L.F(K.UpdateAvailable, release.Version), System.Windows.Forms.ToolTipIcon.Info);
        };
        _window.PreparedUpdateAvailable += () =>
        {
            if (_tray == null || _window.IsPresenting) return;
            _updateNotificationRelease = null;
            _tray.ShowBalloonTip(10000, "ReStart", L.F(K.UpdateReadyBody, _window.PreparedUpdate!.Version), System.Windows.Forms.ToolTipIcon.Info);
        };
        if (e.Args.Contains("--complete-update"))
        {
            if (!await FinishUpdatedStartupAsync(e.Args, _window)) return;
        }
        if (e.Args.Contains("--update-result"))
        {
            var id = e.Args[Array.IndexOf(e.Args, "--update-result") + 1];
            PortableUpdates.Record(PortableUpdates.Outcome(id) == "rolled-back" ? DiagnosticCode.UpdateRolledBack : DiagnosticCode.UpdatePreparationFailed);
            _ = ShowRestoredUpdateAsync(_window);
        }
        if (pinPath != null) await _window.PinPathAsync(pinPath);
        else if (e.Args.Contains("--background")) _ = _window.InitializeAsync();
        else _window.Present();
        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
            PortableUpdates.Record(DiagnosticCode.UnhandledError);
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReStart");
            SafeErrorLog.Write(directory, args.Exception);
            MessageBox.Show(L.T(K.ErrorUnhandled), "ReStart", MessageBoxButton.OK, MessageBoxImage.Error);
            ExitApplication();
        };
    }

    private void CreateTray()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add(L.T(K.AppOpen), null, (_, _) => Dispatcher.Invoke(() => _window!.Present()));
        menu.Items.Add(L.T(K.NavSettings), null, (_, _) => Dispatcher.Invoke(() => _window!.PresentSettings()));
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add(L.T(K.CommonExit), null, (_, _) => Dispatcher.Invoke(ExitApplication));
        _tray = new System.Windows.Forms.NotifyIcon
        {
            Text = L.T(K.TrayTooltip),
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };
        _tray.DoubleClick += (_, _) => Dispatcher.Invoke(() => _window!.Toggle());
        _tray.BalloonTipClicked += (_, _) => Dispatcher.Invoke(() => { if (_window?.UpdatePreparation == ReStart.App.MainWindow.PreparationState.Ready) _window.RequestPreparedUpdate(); else if (_updateNotificationRelease != null) _window?.ReviewAvailableUpdate(); });
    }

    private void RefreshTrayLanguage()
    {
        if (_tray?.ContextMenuStrip is not { } menu) return;
        menu.Items[0].Text = L.T(K.AppOpen); menu.Items[1].Text = L.T(K.NavSettings); menu.Items[3].Text = L.T(K.CommonExit);
        _tray.Text = L.T(K.TrayTooltip);
    }

    private void RunSmoke(string[] args)
    {
        var watch = Stopwatch.StartNew();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        var settled = false;
        var searchIndex = Array.IndexOf(args, "--search-query");
        var query = searchIndex >= 0 && searchIndex + 1 < args.Length ? args[searchIndex + 1] : null;
        var searchPrepared = false;
        timer.Tick += async (_, _) =>
        {
            if (_window == null) return;
            if ((!_window.IsReady || !_window.IconsReady || !_window.ScriptsReady) && watch.Elapsed < TimeSpan.FromSeconds(45)) return;
            if (query != null && !searchPrepared) { _window.SetSmokeSearch(query); searchPrepared = true; return; }
            if (!settled) { settled = true; return; }
            timer.Stop();
            var pathIndex = Array.IndexOf(args, "--snapshot");
            var path = pathIndex >= 0 && pathIndex + 1 < args.Length ? Path.GetFullPath(args[pathIndex + 1]) : Path.Combine(AppContext.BaseDirectory, "smoke.png");
            var success = _window.IsReady && _window.IconsReady && _window.ScriptsReady && _window.CatalogCount > 0 && _window.IconCount > 0;
            NavigationSmokeResult? navigation = null;
            PlacementSmokeResult? placement = null;
            AppFolderSmokeResult? appFolders = null;
            FirstInputSmokeResult? firstInput = null;
            AppMenuSmokeResult? appMenus = null;
            NativeJumpSmokeResult? nativeJumps = null;
            ReleaseInfoSmokeResult? releaseInfo = null;
            ScrollSmokeResult? scrolling = null;
            BundledTilesSmokeResult? bundledTiles = null;
            IconSmokeResult? pinIcons = null;
            MotionSmokeResult? motion = null;
            EditingSmokeResult? editing = null;
            FolderDropSmokeResult? folderDrops = null;
            TileDataSmokeResult? tileData = null;
            SearchSelectionSmokeResult? searchSelection = null;
            StartupSmokeResult? startupPreparation = null;
            WarmResumeSmokeResult? warmResume = null;
            AppearanceSmokeResult? appearance = null;
            SettingsPreviewSmokeResult? settingsPreview = null;
            FidelitySmokeResult? fidelity = null;
            int? tileArtwork = null;
            string? error = null;
            try
            {
                if (args.Contains("--navigation-test")) navigation = _window.VerifyNavigation(path);
                if (args.Contains("--placement-test")) placement = _window.VerifyPlacement(path);
                if (args.Contains("--app-tree-test")) appFolders = _window.VerifyAppFolders(path);
                if (args.Contains("--first-input-test")) firstInput = await _window.VerifyFirstInputAsync(path);
                if (args.Contains("--app-menu-test")) appMenus = await _window.VerifyAppMenusAsync(path);
                if (args.Contains("--jump-list-test")) nativeJumps = await _window.VerifyNativeJumpListsAsync(path);
                if (args.Contains("--release-info-test")) releaseInfo = await _window.VerifyReleaseInfoAsync(path);
                if (args.Contains("--scroll-test")) scrolling = await _window.VerifyScrollingAsync(path);
                if (args.Contains("--bundled-tiles-test")) bundledTiles = await _window.VerifyBundledTilesAsync(path);
                if (args.Contains("--icon-test")) pinIcons = await _window.VerifyIconsAsync(path);
                if (args.Contains("--motion-test")) motion = await _window.VerifyMotionAsync(path);
                if (args.Contains("--editing-test")) editing = await _window.VerifyEditingAsync(path);
                if (args.Contains("--folder-drop-test")) folderDrops = await _window.VerifyFolderDropsAsync(path);
                if (args.Contains("--tile-data-test")) tileData = await _window.VerifyTileDataAsync(path);
                if (args.Contains("--search-selection-test")) searchSelection = await _window.VerifySearchSelectionAsync(path);
                if (args.Contains("--startup-test")) startupPreparation = await _window.VerifyStartupAsync(path);
                if (args.Contains("--warm-resume-test"))
                {
                    var idleIndex = Array.IndexOf(args, "--resume-idle-seconds");
                    var idleSeconds = idleIndex >= 0 && int.TryParse(args.ElementAtOrDefault(idleIndex + 1), out var seconds) ? seconds : 3;
                    warmResume = await _window.VerifyWarmResumeAsync(path, idleSeconds);
                }
                if (args.Contains("--settings-preview-test")) settingsPreview = await _window.VerifySettingsPreviewAsync(path);
                if (args.Contains("--appearance-test")) appearance = await _window.VerifyAppearanceAsync(path);
                if (args.Contains("--fidelity-test")) fidelity = await _window.VerifyFidelityAsync(path);
                if (args.Contains("--tile-artwork-test")) tileArtwork = await _window.VerifyTileArtworkOnlyAsync(path);
                _window.SaveScreenshot(path);
            }
            catch (Exception ex) { success = false; error = ex.ToString(); Console.Error.WriteLine(ex); }
            try
            {
                File.WriteAllText(Path.ChangeExtension(path, ".json"), JsonSerializer.Serialize(new { success, apps = _window.CatalogCount, icons = _window.IconCount, catalogFolders = _window.CatalogFolderCount, groups = _window.Document.Groups.Count, scriptTiles = _window.ScriptTileCount, scriptFrames = _window.ScriptFrameCount, query, searchResults = _window.SearchResultNames, searchInputVisible = _window.SearchInputInVisibleTree, navigation, placement, appFolders, firstInput, appMenus, nativeJumps, releaseInfo, scrolling, bundledTiles, pinIcons, motion, editing, folderDrops, tileData, searchSelection, startupPreparation, warmResume, appearance, settingsPreview, fidelity, tileArtwork, startup = _window.StartupMetrics, error, elapsedMs = watch.ElapsedMilliseconds }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (IOException) { success = false; }
            _window.Exit(); Shutdown(success ? 0 : 1);
        };
        timer.Start();
    }

    /// <summary>Saves the current layout and disposes the launcher before shutting down the application.</summary>
    public void ExitApplication() { _window?.SaveLayout(); _window?.Exit(); Shutdown(); }
    /// <summary>Releases hooks, tray resources, activation waits, and the instance lease.</summary>
    protected override void OnExit(ExitEventArgs e)
    {
        _integration?.Dispose();
        _tabletIntegration?.Dispose();
        _pinRequests?.Dispose();
        _tray?.Dispose();
        _wait?.Unregister(null);
        _instance?.Dispose();
        base.OnExit(e);
    }
}
