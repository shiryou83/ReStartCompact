using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ReStart.App.Controls;
using ReStart.App.Services;
using ReStart.Core;

namespace ReStart.App;

public partial class MainWindow
{
    private sealed record ArtworkView(Tile Tile, TileSurfaceButton Button, Grid Surface, FrameworkElement Icon, Brush Background, Brush Foreground)
    {
        internal TileArtworkLoader.Frame? Current { get; set; }
    }
    private readonly List<ArtworkView> _artworkViews = [];
    private IReadOnlyDictionary<(string App, TileSize Size), TileArtworkLoader.Frame> _artworkFrames = new Dictionary<(string, TileSize), TileArtworkLoader.Frame>();
    private CancellationTokenSource? _artworkCancellation;
    private Task? _artworkReadTask;
    private string _artworkRequested = "", _artworkContext = "";
    private bool? _artworkHighContrastFixture;
    private bool ArtworkHighContrast => _artworkHighContrastFixture ?? SystemParameters.HighContrast;
    private Func<IReadOnlyList<TileArtworkLoader.Request>, CancellationToken, Task<IReadOnlyDictionary<(string App, TileSize Size), TileArtworkLoader.Frame>>>? _artworkFixture;

    private FrameworkElement ArtworkSurface(Tile tile, TileSurfaceButton button, FrameworkElement icon)
    {
        var surface = new Grid { ClipToBounds = true }; surface.Children.Add(icon);
        // Full-tile artwork can paint over the selected theme color. Keep the app icon on those tiles.
        if (tile.Widget.Length == 0 && !tile.IsFolder && tile.ThemeColor == TileThemeColor.Automatic)
            _artworkViews.Add(new(tile, button, surface, icon, button.Background, button.Foreground));
        return surface;
    }

    private void RefreshTileArtwork()
    {
        if (_exit || !DisplayPreferences.UseAppTileImages || ArtworkHighContrast || _preview && _artworkFixture == null)
        {
            _artworkCancellation?.Cancel(); _artworkRequested = ""; _artworkContext = "";
            _artworkFrames = new Dictionary<(string, TileSize), TileArtworkLoader.Frame>(); ApplyTileArtworkViews(); return;
        }
        var dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
        var context = JsonSerializer.Serialize(new { ThemeWindow.IsLight, Dpi = dpi, DisplayPreferences.TileScale, Language = L.Culture.Name });
        var requests = _artworkViews.Select(v =>
        {
            var app = _apps.FirstOrDefault(a => a.Id.Equals(v.Tile.AppId, StringComparison.OrdinalIgnoreCase))
                ?? new(v.Tile.AppId, v.Tile.Label, v.Tile.Target, v.Tile.IsShellApp);
            app = app with { Id = v.Tile.AppId };
            return new TileArtworkLoader.Request(app, v.Tile.Size,
                (int)Math.Ceiling((TileLayout.Dimensions(v.Tile.Size).Width * Cell - 4) * dpi), ThemeWindow.IsLight, L.Culture.Name);
        }).DistinctBy(r => (r.App.Id, r.Size)).OrderBy(r => r.App.Id, StringComparer.Ordinal).ThenBy(r => r.Size).Take(256).ToArray();
        var signature = context + JsonSerializer.Serialize(requests.Select(r => new { r.App.Id, r.App.LaunchTarget, r.App.ExecutablePath, r.Size, r.Pixels }));
        if (_artworkContext != context) _artworkFrames = new Dictionary<(string, TileSize), TileArtworkLoader.Frame>();
        _artworkContext = context; ApplyTileArtworkViews();
        if (_artworkRequested == signature) return;
        _artworkRequested = signature; _artworkCancellation?.Cancel();
        var cancellation = new CancellationTokenSource(); _artworkCancellation = cancellation;
        _artworkReadTask = ReadTileArtworkAsync(requests, signature, cancellation);
    }

    private async Task ReadTileArtworkAsync(TileArtworkLoader.Request[] requests, string signature, CancellationTokenSource cancellation)
    {
        try
        {
            var frames = await (_artworkFixture?.Invoke(requests, cancellation.Token) ?? TileArtworkLoader.ReadAsync(requests, cancellation.Token));
            if (_exit || cancellation.IsCancellationRequested || _artworkRequested != signature || !DisplayPreferences.UseAppTileImages) return;
            _artworkFrames = frames; ApplyTileArtworkViews(); ApplyLegacyFrames();
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (_artworkCancellation == cancellation) _artworkCancellation = null;
            cancellation.Dispose();
        }
    }

    private void ApplyTileArtworkViews()
    {
        foreach (var view in _artworkViews)
        {
            var frame = DisplayPreferences.UseAppTileImages && !ArtworkHighContrast ? _artworkFrames.GetValueOrDefault((view.Tile.AppId, view.Tile.Size)) : null;
            if (ReferenceEquals(view.Current, frame)) continue;
            if (!ReferenceEquals(view.Current, frame))
            {
                view.Surface.Children.Clear(); view.Surface.Margin = new(frame == null ? 0 : -7);
                if (frame == null) view.Surface.Children.Add(view.Icon);
                else
                {
                    var image = new Image { Source = frame.Image, Stretch = Stretch.Uniform, IsHitTestVisible = false };
                    RenderOptions.SetBitmapScalingMode(image, System.Windows.Media.BitmapScalingMode.HighQuality); view.Surface.Children.Add(image);
                }
                view.Current = frame;
            }
            view.Button.Background = view.Background; view.Button.Foreground = view.Foreground;
            if (frame != null && !view.Tile.UseCustomColor)
            {
                try
                {
                    if (frame.Background.Length is > 0 and <= 32 && ColorConverter.ConvertFromString(frame.Background) is Color color && color.A > 0)
                    { view.Button.Background = new SolidColorBrush(color); view.Button.Foreground = ReadableTileInk(color, view.Button.PlateOpacity); }
                }
                catch (Exception ex) when (ex is FormatException or NotSupportedException) { }
                if (frame.Foreground.Equals("light", StringComparison.OrdinalIgnoreCase)) view.Button.Foreground = Brushes.White;
                else if (frame.Foreground.Equals("dark", StringComparison.OrdinalIgnoreCase)) view.Button.Foreground = Brushes.Black;
            }
            if (view.Button.Content is Grid grid && grid.Children.OfType<TextBlock>().FirstOrDefault(label => ReferenceEquals(label.Tag, view.Tile)) is { } name)
            {
                name.Visibility = frame == null || frame.ShowName ? Visibility.Visible : Visibility.Collapsed;
                name.Foreground = view.Button.Foreground;
            }
        }
    }
}
