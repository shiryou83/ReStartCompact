using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using ReStart.App.Services;
using ReStart.Core;
using Drawing = System.Drawing;

namespace ReStart.App;

public partial class App
{
    private static async Task<int> RunCompactCheckAsync(string snapshotPath)
    {
        var snapshot = Path.GetFullPath(snapshotPath);
        Directory.CreateDirectory(Path.GetDirectoryName(snapshot)!);
        var report = Path.ChangeExtension(snapshot, ".json");
        try
        {
            var hits = await ReStart.App.MainWindow.VerifyCompactAsync(snapshot);
            File.WriteAllText(report, JsonSerializer.Serialize(new { success = true, compactHitTests = hits }));
            return 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText(report, JsonSerializer.Serialize(new { success = false, error = ex.ToString() }));
            return 1;
        }
    }
}

public partial class MainWindow
{
    /// <summary>Exercises Compact on an owned HWND with a fictional layout and native sizing messages.</summary>
    internal static async Task<int> VerifyCompactAsync(string snapshotPath)
    {
        var store = new LayoutStore(Path.Combine(Path.GetDirectoryName(snapshotPath)!, "compact-" + Guid.NewGuid().ToString("N"), "layout.json"));
        store.Save(new()
        {
            Preferences = new() { StartMenuMode = StartMenuMode.Compact, CompactMenuWidth = 640, CompactMenuHeight = 480, Animate = false },
            Groups =
            [
                new() { Name = "Clock group", Column = 0, Row = 0, Tiles = [new() { Label = "Clock", Widget = "clock", Live = true }] },
                new() { Name = "Distant group", Column = 12, Row = 50, Tiles = [new() { Label = "App folder", Children = [new() { Label = "Sample" }] }] }
            ]
        });
        var dispatcher = Application.Current.Dispatcher;
        var screen = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);
        var area = screen.WorkingArea;
        var fixture = new MainWindow(store, true, () => Task.FromResult(new List<AppEntry> { new("sample", "Sample app", "", false) }))
        {
            Topmost = false,
            _verifyCompactPresentation = true,
            _startButtonPoint = screen.Bounds.Location,
            NativeWorkingArea = _ => area
        };
        try
        {
            await fixture.InitializeAsync();
            fixture.Present();
            var handle = new WindowInteropHelper(fixture).Handle;
            await dispatcher.InvokeAsync(fixture.UpdateLayout, DispatcherPriority.ApplicationIdle);
            var initial = ReadCompactBounds(handle);
            var dpi = CompactDpi(handle);
            Require(fixture.IsVisible && !fixture.ShowInTaskbar && fixture.WindowStyle == WindowStyle.None &&
                initial == CompactMenuBounds.Place(area, fixture._compactDock, new(640, 480), dpi), "Compact did not present its requested window.");
            Require(GetClientRect(handle, out var client) && client.Right == initial.Width && client.Bottom == initial.Height,
                "A native frame consumes Compact's client area.");
            var middleX = initial.Left + initial.Width / 2;
            var middleY = initial.Top + initial.Height / 2;
            (int X, int Y, int Hit)[] hits =
            [
                (initial.Left, middleY, 10), (initial.Right - 1, middleY, 11), (middleX, initial.Top, 12), (middleX, initial.Bottom - 1, 15),
                (initial.Left, initial.Top, 13), (initial.Right - 1, initial.Top, 14),
                (initial.Left, initial.Bottom - 1, 16), (initial.Right - 1, initial.Bottom - 1, 17), (middleX, middleY, 1)
            ];
            foreach (var (x, y, hit) in hits)
            {
                var point = (nint)unchecked((int)((uint)(ushort)y << 16 | (ushort)x));
                Require(SendMessage(handle, WmNcHitTest, 0, point) == hit, $"Unexpected Compact hit test at {x},{y}.");
            }

            var proposed = Drawing.Rectangle.Inflate(initial, 44, 44);
            var edge = fixture._compactDock switch
            {
                TaskbarEdge.Right => SizingEdge.BottomLeft,
                TaskbarEdge.Bottom => SizingEdge.TopRight,
                _ => SizingEdge.BottomRight
            };
            var expected = CompactMenuBounds.Resize(area, fixture._compactDock, initial, proposed, edge, dpi);
            var memory = Marshal.AllocHGlobal(Marshal.SizeOf<Native.Rect>());
            try
            {
                SendMessage(handle, WmEnterSizeMove, 0, 0);
                fixture.DismissFromTaskbar();
                Require(fixture.IsVisible, "An outside dismissal closed Compact during sizing.");
                Marshal.StructureToPtr(new Native.Rect { Left = proposed.Left, Top = proposed.Top, Right = proposed.Right, Bottom = proposed.Bottom }, memory, false);
                Require(SendMessage(handle, WmSizing, (int)edge, memory) == 1, "Compact did not handle native sizing.");
                var sized = Marshal.PtrToStructure<Native.Rect>(memory);
                Native.SetWindowPos(handle, 0, sized.Left, sized.Top, sized.Right - sized.Left, sized.Bottom - sized.Top, 0x0014);
                SendMessage(handle, WmExitSizeMove, 0, 0);
            }
            finally { Marshal.FreeHGlobal(memory); }
            Require(ReadCompactBounds(handle) == expected && area.Contains(expected), "The new bounds moved off the monitor or lost the dock.");
            var saved = store.Load()!;
            Require(saved.Preferences.CompactMenuWidth == (expected.Width == initial.Width ? 640 : CompactMenuBounds.ToDips(expected.Width, dpi)) &&
                saved.Preferences.CompactMenuHeight == (expected.Height == initial.Height ? 480 : CompactMenuBounds.ToDips(expected.Height, dpi)),
                "The final user resize was not saved in logical units.");
            Require(saved.Groups.Select(group => group.Name).SequenceEqual(["Clock group", "Distant group"]) &&
                saved.Groups[1].Tiles[0].IsFolder && fixture._liveLabels.Single().Text.Text.Length > 0, "The resize changed the groups, folder, or live clock.");

            fixture.DismissFromTaskbar();
            Require(!fixture.IsVisible, "Compact did not dismiss after sizing ended.");
            fixture.Present();
            await dispatcher.InvokeAsync(fixture.UpdateLayout, DispatcherPriority.ApplicationIdle);
            var preferred = new Drawing.Size(saved.Preferences.CompactMenuWidth, saved.Preferences.CompactMenuHeight);
            Require(ReadCompactBounds(handle) == CompactMenuBounds.Place(area, fixture._compactDock, preferred, dpi),
                "Reopening did not restore the resized Compact size.");
            fixture.NativeWorkingArea = _ => initial;
            fixture.RefitNativeBounds();
            Require(ReadCompactBounds(handle) == initial && fixture.Document.Preferences.CompactMenuWidth == preferred.Width &&
                fixture.Document.Preferences.CompactMenuHeight == preferred.Height, "A smaller work area overwrote the preferred size or failed to constrain the window.");
            fixture.NativeWorkingArea = _ => area;
            fixture.RefitNativeBounds();
            Require(ReadCompactBounds(handle) == CompactMenuBounds.Place(area, fixture._compactDock, preferred, dpi),
                "Restoring the work area did not recover the preferred size.");
            fixture.SetSmokeSearch("Sample");
            Require(fixture.SearchInputInVisibleTree && fixture.SearchResultNames.Contains("Sample app"), "The Compact viewport lost search.");
            fixture.ResetToPinnedView();
            await dispatcher.InvokeAsync(fixture.UpdateLayout, DispatcherPriority.ApplicationIdle);
            var scroll = fixture.ContentScroll;
            scroll.ScrollToVerticalOffset(scroll.ScrollableHeight);
            scroll.ScrollToHorizontalOffset(scroll.ScrollableWidth);
            await dispatcher.InvokeAsync(fixture.UpdateLayout, DispatcherPriority.ApplicationIdle);
            Require(scroll.HorizontalOffset > 0 && scroll.VerticalOffset > 0,
                $"The distant group cannot be reached on both axes. Offset {scroll.HorizontalOffset},{scroll.VerticalOffset}. Extent {scroll.ScrollableWidth},{scroll.ScrollableHeight}.");
            scroll.ScrollToHome();
            await dispatcher.InvokeAsync(fixture.UpdateLayout, DispatcherPriority.ApplicationIdle);
            fixture.SaveScreenshot(snapshotPath);

            fixture.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(fixture), Environment.TickCount, Key.Escape)
            { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            Require(!fixture.IsVisible, "Escape did not dismiss Compact.");
            fixture.Present();

            fixture.Document.Preferences.StartMenuMode = StartMenuMode.Fullscreen;
            fixture.RefreshLayout();
            Require(!fixture._compactFrame && fixture.ResizeMode == ResizeMode.NoResize && ReadCompactBounds(handle) == area,
                "Switching back to Fullscreen did not restore the original window bounds.");
            return hits.Length;
        }
        finally { fixture.Exit(); }
    }

    [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetClientRect(nint window, out Native.Rect rect);
}
