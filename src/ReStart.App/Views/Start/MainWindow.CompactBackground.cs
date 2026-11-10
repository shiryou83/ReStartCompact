using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using ReStart.App.Services;

namespace ReStart.App;

public partial class MainWindow
{
    private const uint BlurBehindEnable = 0x1, BlurBehindRegion = 0x2;
    private const double CompactSurfaceOpacity = .45;
    private Color? _opaqueCompositionColor;
    private SolidColorBrush? _compactTextSurface;
    private bool HasTransparentCompactBackground => _opaqueCompositionColor.HasValue;

    private bool ApplyCompactTransparency(bool requested)
    {
        if (requested == HasTransparentCompactBackground) return requested;
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == 0 || HwndSource.FromHwnd(handle)?.CompositionTarget is not { } target) return false;

        if (requested)
        {
            // DWM composites client alpha outside the blur region. Place the
            // region outside the client area so the backdrop stays unblurred.
            var region = CreateRectRgn(-1, -1, 0, 0);
            if (region == 0) return false;
            try
            {
                var blur = new BlurBehind { Flags = BlurBehindEnable | BlurBehindRegion, Enabled = 1, Region = region };
                if (DwmEnableBlurBehindWindow(handle, ref blur) < 0) return false;
            }
            finally { Native.DeleteObject(region); }
            _opaqueCompositionColor = target.BackgroundColor;
            target.BackgroundColor = Colors.Transparent;
            Background = Brushes.Transparent;
        }
        else
        {
            var blur = new BlurBehind { Flags = BlurBehindEnable };
            DwmEnableBlurBehindWindow(handle, ref blur);
            target.BackgroundColor = _opaqueCompositionColor.GetValueOrDefault();
            _opaqueCompositionColor = null;
            ClearValue(BackgroundProperty);
        }
        return requested;
    }

    private void ApplyStartBackground(Color color, byte opacity, bool transparent)
    {
        var surface = new SolidColorBrush(color);
        Root.Background = transparent ? Brushes.Transparent : surface;
        Tint.Background = new SolidColorBrush(Color.FromArgb(opacity, color.R, color.G, color.B));
        Wallpaper.Visibility = transparent ? Visibility.Collapsed : Visibility.Visible;
        _compactTextSurface = transparent ? new SolidColorBrush(color) { Opacity = CompactSurfaceOpacity } : null;
        NavigationRail.Background = transparent ? surface : null;
        TopBar.Background = AppsPanel.Background = ResultsPanel.Background = _compactTextSurface;
        Resources["StartOverlayBrush"] = _compactTextSurface ?? FindResource("PanelBrush");
    }

    private SolidColorBrush ReadableTileInk(Color plate, double plateOpacity) => ReadableInk(
        plateOpacity < 1 && _compactTextSurface is { } surface ? Mix(surface.Color, plate, plateOpacity) : plate);

    [StructLayout(LayoutKind.Sequential)]
    private struct BlurBehind
    {
        internal uint Flags;
        internal int Enabled;
        internal nint Region;
        internal int TransitionOnMaximized;
    }

    [DllImport("dwmapi.dll")] private static extern int DwmEnableBlurBehindWindow(nint window, ref BlurBehind blur);
    [DllImport("gdi32.dll")] private static extern nint CreateRectRgn(int left, int top, int right, int bottom);
}
