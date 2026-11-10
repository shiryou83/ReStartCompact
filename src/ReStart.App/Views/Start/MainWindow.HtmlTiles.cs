using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ReStart.App.Controls;
using ReStart.App.Services;
using ReStart.Core;

namespace ReStart.App;

public partial class MainWindow
{
    private readonly Dictionary<string, HtmlTileView> _htmlTiles = [];
    private readonly HashSet<string> _usedHtmlTiles = [];
    private readonly HashSet<string> _folderThumbnailIds = [];
    private readonly Dictionary<string, (TileSize Size, double Scale)> _htmlFootprints = [];
    private bool _documentationHtmlRendering;
    private const string StandardWebCardBackground = "background: linear-gradient(125deg, var(--start), var(--end));";
    private static readonly string CompactWebCardStyle = FormattableString.Invariant($$"""

        @supports (color: color-mix(in srgb, black, transparent)) {
            body > .card {
                background-image: linear-gradient(125deg,
                    color-mix(in srgb, var(--start) {{CompactSurfaceOpacity * 100:0}}%, transparent),
                    color-mix(in srgb, var(--end) {{CompactSurfaceOpacity * 100:0}}%, transparent));
            }
        }
        """);

    private async void OnContentMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var horizontal = Keyboard.Modifiers == ModifierKeys.Shift;
        var button = Ancestor<Button>(e.OriginalSource as DependencyObject);
        if (!_layoutEditing && !_dragging && button?.Tag is Tile tile && _htmlTiles.TryGetValue(tile.Id, out var view) && view.BrowserReady)
        {
            // Capture the source and consume the routed event before awaiting:
            // neither a later hover target nor WPF's parent scrolling may take it.
            e.Handled = true;
            await RouteWebTileScrollAsync(view, e.GetPosition(button), e.Delta, horizontal);
        }
        else if (horizontal && ContentScroll.ScrollableWidth > 0)
        { ContentScroll.ScrollToHorizontalOffset(ContentScroll.HorizontalOffset - e.Delta); e.Handled = true; }
    }

    private bool HandleWebTileScrollKey(KeyEventArgs e)
    {
        if (_layoutEditing || Keyboard.Modifiers != ModifierKeys.None || e.Key is not (Key.PageUp or Key.PageDown)) return false;
        var button = Ancestor<Button>(e.OriginalSource as DependencyObject);
        if (button?.Tag is not Tile tile || !_htmlTiles.TryGetValue(tile.Id, out var view) || !view.BrowserReady) return false;
        e.Handled = true;
        _ = RouteWebTileScrollAsync(view, new(button.ActualWidth / 2, button.ActualHeight / 2), e.Key == Key.PageUp ? 120 : -120, page: true);
        return true;
    }

    private async Task RouteWebTileScrollAsync(HtmlTileView view, Point point, int delta, bool horizontal = false, bool page = false)
    {
        if (await view.ScrollAsync(point, delta, horizontal, page)) return;
        if (_dismissing || _exit || _dragging || _layoutEditing || (!_preview && !IsVisible) || !_htmlTiles.Values.Contains(view)) return;
        ScrollBoard(delta, horizontal, page);
    }

    private void ScrollBoard(int delta, bool horizontal, bool page)
    {
        if (horizontal) ContentScroll.ScrollToHorizontalOffset(ContentScroll.HorizontalOffset - delta);
        else
        {
            var distance = page || SystemParameters.WheelScrollLines < 0 ? ContentScroll.ViewportHeight : SystemParameters.WheelScrollLines * 16;
            ContentScroll.ScrollToVerticalOffset(ContentScroll.VerticalOffset - delta / 120d * distance);
        }
    }

    private HtmlTileView HtmlView(Tile tile, bool thumbnail = false)
    {
        _usedHtmlTiles.Add(tile.Id);
        var offscreen = thumbnail || _preview && _documentationHtmlRendering;
        ImageSource? retained = null;
        if (_htmlTiles.TryGetValue(tile.Id, out var previous) && previous.OffscreenOnly != offscreen)
        {
            retained = previous.RetainedPresentation;
            _htmlTiles.Remove(tile.Id); previous.Dispose(); _scriptFrames.Remove(tile.Id);
            // Replacing the browser invalidates in-flight replies, but opening a
            // folder must retain its timer state and queued local interactions.
            if (_scriptSessions.TryGetValue(tile.Id, out var pending))
                _scriptSessions[tile.Id] = new() { State = pending.State, Inputs = new(pending.Inputs), Attempts = pending.Attempts };
        }
        if (!_htmlTiles.TryGetValue(tile.Id, out var view))
        {
            _scriptFrames.Remove(tile.Id);
            _htmlTiles[tile.Id] = view = new(_scriptPackages.BrowserDataRoot, tile.Id, offscreen);
            var ownedView = view;
            view.RecoveryRequired += () =>
            {
                if (_exit || !_htmlTiles.TryGetValue(tile.Id, out var active) || active != ownedView) return;
                _liveSessions.Budget(tile.Id).Fail(Environment.TickCount64, Document.Preferences.AutoRecoverLiveTiles);
                if (_scriptSessions.TryGetValue(tile.Id, out var runtime)) runtime.NextUpdate = DateTimeOffset.MinValue;
            };
            view.ShowTransitionPreview(retained);
            if (_scriptSessions.TryGetValue(tile.Id, out var session)) session.NextUpdate = DateTimeOffset.MinValue;
        }
        if (view.Parent is Panel panel) panel.Children.Remove(view);
        return view;
    }

    private void RememberChangingHtmlPresentations()
    {
        var miniatures = Document.Groups.SelectMany(group => group.Tiles).Where(tile => tile.IsFolder && tile != _openFolder)
            .SelectMany(folder => folder.Children.Take(4)).Select(tile => tile.Id).ToHashSet();
        foreach (var (id, view) in _htmlTiles)
            if (view.OffscreenOnly != (miniatures.Contains(id) || _preview && _documentationHtmlRendering)) view.RememberPresentation();
    }

    private void RetireHtmlTiles(bool all = false)
    {
        foreach (var id in _htmlTiles.Keys.Where(id => all || !_usedHtmlTiles.Contains(id)).ToArray())
        { _htmlTiles.Remove(id, out var view); _htmlFootprints.Remove(id); view!.Dispose(); }
    }

    private void ResetHtmlPresentations()
    {
        // WPF layout refreshes ordinarily keep browser instances to avoid
        // flicker. A display/DPI reset can invalidate their capture surfaces,
        // so this path must replace the controls as the manual Reload does.
        // Keep host-owned state and queued input, unlike an explicit Reload.
        _scriptPresentationGeneration++;
        foreach (var id in _htmlTiles.Keys)
        {
            _scriptFrames.Remove(id); _scriptAttempts.Remove(id);
            if (_scriptSessions.TryGetValue(id, out var session))
                _scriptSessions[id] = new()
                {
                    State = session.State,
                    Inputs = new(session.Inputs),
                    Attempts = session.Attempts,
                    NextUpdate = DateTimeOffset.MinValue
                };
        }
        RetireHtmlTiles(all: true);
    }

    private async Task<(ScriptTileFrame? Frame, string? Error)> RenderHtmlTileAsync(Tile tile, Button button, object context, ScriptTilePackages.RenderSource package)
    {
        if (!_htmlTiles.TryGetValue(tile.Id, out var view)) return (null, null);
        view.SetImage(_slideshowImages.GetValueOrDefault(tile.Id));
        view.SetAssets(package.Assets);
        var css = package.Css;
        if (HasTransparentCompactBackground && css.Contains(StandardWebCardBackground, StringComparison.Ordinal))
        {
            // Apply the standard card treatment to installed copies as well as new ones.
            css += CompactWebCardStyle;
            button.Background = Brushes.Transparent;
        }
        var result = await view.RenderAsync(package.Html, css, package.Script, tile.AllowWebNetwork && package.Manifest.Capabilities.Contains("web.network"), context);
        if (result.Frame != null && _folderThumbnailIds.Contains(tile.Id)) await view.FreezeForDocumentationAsync();
        return result;
    }

    private void AddWebTileMenu(ContextMenu menu, Tile tile)
    {
        if (!_scriptPackages.IsWeb(tile.ScriptId)) return;
        var network = new MenuItem
        {
            Header = L.T(K.AllowWebTileNetwork),
            IsCheckable = true,
            IsChecked = tile.AllowWebNetwork,
            IsEnabled = _scriptPackages.Read(tile.ScriptId).Manifest.Capabilities.Contains("web.network")
        };
        network.Click += (_, _) =>
        {
            tile.AllowWebNetwork = network.IsChecked;
            if (_htmlTiles.Remove(tile.Id, out var view)) view.Dispose();
            _scriptSessions.Remove(tile.Id); CommitLayout();
        };
        menu.Items.Add(network);
    }
}
