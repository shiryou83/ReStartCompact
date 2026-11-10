using System.Windows.Interop;
using System.Windows;
using System.Windows.Media;
using ReStart.App.Services;

namespace ReStart.App;

public partial class MainWindow
{
    /// <summary>Resolves this launcher's physical available area after a scoped temporary taskbar effect; null follows Windows.</summary>
    internal Func<System.Windows.Forms.Screen, System.Drawing.Rectangle>? NativeWorkingArea { get; set; }
    /// <summary>Resolves the interactive content area in physical screen pixels; the native window and wallpaper keep their existing bounds.</summary>
    internal Func<System.Windows.Forms.Screen, System.Drawing.Rectangle>? NativeContentArea { get; set; }

    /// <summary>Refits a presented native launcher without activating it or changing other monitors.</summary>
    internal void RefitNativeBounds()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == 0) return;
        var screen = System.Windows.Forms.Screen.FromHandle(handle);
        if (UsesNativeStartBounds && IsPresenting)
        {
            PlaceStartWindow(handle, screen, 0, 0x0014);
            return;
        }
        var area = NativeWorkingArea?.Invoke(screen) ?? screen.WorkingArea;
        ApplyTaskbarContentInsets(area, NativeContentArea?.Invoke(screen) ?? area, VisualTreeHelper.GetDpi(this));
    }

    private System.Drawing.Rectangle PlaceStartWindow(nint handle, System.Windows.Forms.Screen screen, nint after, uint flags, bool opening = false)
    {
        var area = NativeWorkingArea?.Invoke(screen) ?? screen.WorkingArea;
        var safe = NativeContentArea?.Invoke(screen) ?? area;
        if (WantsCompact)
        {
            ClearTaskbarContentInsets();
            return FitCompactBounds(handle, after, flags, screen, safe.Width > 0 && safe.Height > 0 ? safe : screen.Bounds, opening);
        }
        MonitorPlacement.FitWorkingArea(handle, after, area, flags | UpdateCompactFrame(handle, false), NativeWorkingArea);
        ApplyTaskbarContentInsets(area, safe, VisualTreeHelper.GetDpi(this));
        return area;
    }

    /// <summary>Offsets interactive content using DPI-correct taskbar overlap while leaving native/background geometry unchanged.</summary>
    /// <param name="area">Actual launcher rectangle in physical screen pixels.</param>
    /// <param name="safe">Content-safe subrectangle in the same physical coordinate system.</param>
    /// <param name="dpi">Current WPF scale; physical overlap is converted to device-independent margins.</param>
    internal void ApplyTaskbarContentInsets(System.Drawing.Rectangle area, System.Drawing.Rectangle safe, DpiScale dpi)
    {
        ContentArea.Margin = new Thickness(Math.Max(0, safe.Left - area.Left) / dpi.DpiScaleX,
            Math.Max(0, safe.Top - area.Top) / dpi.DpiScaleY, Math.Max(0, area.Right - safe.Right) / dpi.DpiScaleX,
            Math.Max(0, area.Bottom - safe.Bottom) / dpi.DpiScaleY);
    }

    /// <summary>Removes only temporary content overlap reservations when taskbar integration is disposed.</summary>
    internal void ClearTaskbarContentInsets() => ContentArea.Margin = new Thickness(0);
}
