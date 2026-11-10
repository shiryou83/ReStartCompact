using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ReStart.App.Services;
using ReStart.Core;
using Drawing = System.Drawing;

namespace ReStart.App;

public partial class MainWindow
{
    private const int WmGetMinMaxInfo = 0x0024, WmNcCalcSize = 0x0083, WmNcHitTest = 0x0084,
        WmSysCommand = 0x0112, WmSizing = 0x0214, WmEnterSizeMove = 0x0231, WmExitSizeMove = 0x0232;
    private const uint SwpFrameChanged = 0x0020;
    private bool _compactFrame, _compactRefitPending, _compactRefitAfterResize, _verifyCompactPresentation;
    private Drawing.Rectangle _compactArea, _compactBounds;
    private Drawing.Rectangle? _compactResizeStart;
    private TaskbarEdge _compactDock;
    private uint _compactDpi;
    private bool UsesNativeStartBounds => !_preview || _verifyCompactPresentation;
    private bool WantsCompact => Document.Preferences.StartMenuMode == StartMenuMode.Compact;

    private void InitializeCompactFrame(nint handle)
    {
        if (UsesNativeStartBounds) HwndSource.FromHwnd(handle).AddHook(CompactWindowProc);
    }

    private uint UpdateCompactFrame(nint handle, bool enabled)
    {
        if (_compactFrame == enabled) return 0;
        if (enabled) MonitorPlacement.Release(handle);
        _compactFrame = enabled;
        _compactArea = Drawing.Rectangle.Empty;
        ResizeMode = enabled ? ResizeMode.CanResize : ResizeMode.NoResize;
        return SwpFrameChanged;
    }

    private Drawing.Rectangle FitCompactBounds(nint handle, nint after, uint flags,
        System.Windows.Forms.Screen screen, Drawing.Rectangle area, bool opening)
    {
        var current = ReadCompactBounds(handle);
        if (_compactResizeStart.HasValue)
        {
            _compactRefitAfterResize = true;
            return current;
        }

        var dock = TaskbarNativeBackend.DockedEdge(screen.Bounds, screen.WorkingArea);
        var dpi = CompactDpi(handle);
        flags |= UpdateCompactFrame(handle, true);
        if (!opening && area == _compactArea && dock == _compactDock && dpi == _compactDpi && current == _compactBounds)
            return current;

        _compactArea = area;
        _compactDock = dock;
        _compactDpi = dpi;
        var p = Document.Preferences;
        var target = CompactMenuBounds.Place(area, dock, new(p.CompactMenuWidth, p.CompactMenuHeight), dpi);
        Native.SetWindowPos(handle, after, target.X, target.Y, target.Width, target.Height, flags);
        // The first move can cross a DPI boundary while the HWND is still hidden.
        var newDpi = CompactDpi(handle);
        if (newDpi != dpi)
        {
            _compactDpi = newDpi;
            target = CompactMenuBounds.Place(area, dock, new(p.CompactMenuWidth, p.CompactMenuHeight), newDpi);
            Native.SetWindowPos(handle, after, target.X, target.Y, target.Width, target.Height, flags);
        }
        return _compactBounds = target;
    }

    private void QueueCompactRefit()
    {
        if (!_compactFrame || _compactRefitPending) return;
        _compactRefitPending = true;
        Dispatcher.BeginInvoke(() =>
        {
            _compactRefitPending = false;
            if (!_exit && _compactFrame && IsVisible) RefitNativeBounds();
        }, DispatcherPriority.Loaded);
    }

    private nint CompactWindowProc(nint handle, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (!_compactFrame) return 0;
        switch (message)
        {
            case WmNcCalcSize:
                // Keep WS_THICKFRAME for the native sizing loop and give its
                // entire rectangle to WPF so the original chrome stays visible.
                handled = true;
                break;
            case WmNcHitTest when IsVisible && !_dismissing && !_dragging:
                var hit = CompactResizeHit(handle, lParam);
                if (hit != 0) { handled = true; return hit; }
                break;
            case WmSysCommand when ((int)wParam & 0xfff0) is 0xf010 or 0xf020 or 0xf030:
                // The system menu and shortcuts cannot move, minimize or maximize Start.
                handled = true;
                break;
            case WmGetMinMaxInfo when !_compactArea.IsEmpty:
                var limits = Marshal.PtrToStructure<MinMaxInfo>(lParam);
                var minimum = CompactMenuBounds.Minimum(_compactArea, _compactDpi);
                limits.MinTrackSize = new() { X = minimum.Width, Y = minimum.Height };
                limits.MaxTrackSize = new() { X = _compactArea.Width, Y = _compactArea.Height };
                Marshal.StructureToPtr(limits, lParam, false);
                handled = true;
                break;
            case WmEnterSizeMove when IsVisible && !_compactArea.IsEmpty:
                var bounds = ReadCompactBounds(handle);
                if (bounds.IsEmpty) break;
                _compactResizeStart = bounds;
                CompleteEntrance();
                break;
            case WmSizing when _compactResizeStart is { } start:
                var raw = Marshal.PtrToStructure<Native.Rect>(lParam);
                var proposed = Drawing.Rectangle.FromLTRB(raw.Left, raw.Top, raw.Right, raw.Bottom);
                var sized = CompactMenuBounds.Resize(_compactArea, _compactDock, start, proposed, (SizingEdge)(int)wParam, _compactDpi);
                Marshal.StructureToPtr(new Native.Rect { Left = sized.Left, Top = sized.Top, Right = sized.Right, Bottom = sized.Bottom }, lParam, false);
                handled = true;
                return 1;
            case WmExitSizeMove when _compactResizeStart is { } previous:
                _compactResizeStart = null;
                _compactBounds = ReadCompactBounds(handle);
                if (CompactDpi(handle) == _compactDpi) SaveCompactSize(previous, _compactBounds);
                else _compactRefitAfterResize = true;
                if (_compactRefitAfterResize) { _compactRefitAfterResize = false; QueueCompactRefit(); }
                break;
            case 0x001A or 0x007E: // WM_SETTINGCHANGE, WM_DISPLAYCHANGE
                QueueCompactRefit();
                break;
        }
        return 0;
    }

    private static int CompactResizeHit(nint handle, nint screenPoint)
    {
        var bounds = ReadCompactBounds(handle);
        var x = unchecked((short)(long)screenPoint);
        var y = unchecked((short)((long)screenPoint >> 16));
        if (!bounds.Contains(x, y)) return 0;
        var grip = Math.Max(1, (int)Math.Round(7 * CompactDpi(handle) / 96d));
        var horizontal = x - bounds.Left < grip ? -1 : bounds.Right - x <= grip ? 1 : 0;
        var vertical = y - bounds.Top < grip ? -1 : bounds.Bottom - y <= grip ? 1 : 0;
        return (horizontal, vertical) switch
        {
            (-1, -1) => 13,
            (0, -1) => 12,
            (1, -1) => 14,
            (-1, 0) => 10,
            (1, 0) => 11,
            (-1, 1) => 16,
            (0, 1) => 15,
            (1, 1) => 17,
            _ => 0
        };
    }

    private void SaveCompactSize(Drawing.Rectangle previous, Drawing.Rectangle current)
    {
        if (current.IsEmpty || current.Size == previous.Size) return;
        var p = Document.Preferences;
        // An axis limited by the current monitor keeps its preferred size
        // when only the other axis was deliberately changed.
        if (current.Width != previous.Width) p.CompactMenuWidth = CompactMenuBounds.ToDips(current.Width, _compactDpi);
        if (current.Height != previous.Height) p.CompactMenuHeight = CompactMenuBounds.ToDips(current.Height, _compactDpi);
        TrySaveLayout(recordHistory: false);
    }

    private static Drawing.Rectangle ReadCompactBounds(nint handle) => Native.GetWindowRect(handle, out var rect)
        ? Drawing.Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom) : Drawing.Rectangle.Empty;

    private static uint CompactDpi(nint handle) => GetDpiForWindow(handle) is > 0 and var dpi ? dpi : 96;

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        internal Native.Point Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize;
    }

    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint window);
}
