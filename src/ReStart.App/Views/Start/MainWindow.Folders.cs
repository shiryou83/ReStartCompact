using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ReStart.App.Controls;
using ReStart.Core;

namespace ReStart.App;

public partial class MainWindow
{
    private FrameworkElement CreateFolderPreview(Tile folder)
    {
        var (width, height) = TileLayout.Dimensions(folder.Size);
        var available = Math.Min(width * Cell - 4, height * Cell - 4) - 14;
        var extent = Math.Min(available, (folder.Size == TileSize.Small ? 30 : folder.Size == TileSize.Large ? 132 : 70) * DisplayPreferences.TileScale);
        var mini = new UniformGridProxy
        {
            Columns = 2,
            Rows = 2,
            Width = extent,
            Height = extent,
            IsHitTestVisible = false,
            UseLayoutRounding = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        foreach (var child in folder.Children.Take(4))
        {
            // A uniform small-size preview prevents the saved child size from
            // changing miniature alignment. The persistent child is untouched.
            var copy = System.Text.Json.JsonSerializer.Deserialize<Tile>(System.Text.Json.JsonSerializer.Serialize(child, LayoutStore.JsonOptions), LayoutStore.JsonOptions)!;
            copy.Size = TileSize.Small;
            if (copy.Widget.Length == 0 && !DisplayPreferences.UseAppTileImages && !(Document.Preferences.LegacyUwpLiveTiles && copy.Live && LegacyEligible(copy)))
            {
                var icon = CreateIcon(copy, extent / 3); icon.Margin = new(0);
                mini.Children.Add(new Border { Child = icon, Margin = new(1) });
            }
            else
            {
                _folderThumbnailIds.Add(copy.Id);
                var tile = CreateTileButton(copy, studioPreview: true, folderThumbnail: true);
                tile.Width = tile.Height = Cell - 4;
                mini.Children.Add(new Viewbox { Child = tile, Stretch = System.Windows.Media.Stretch.Uniform, Margin = new(1) });
            }
        }
        return mini;
    }

    private DispatcherTimer? _nameRefreshTimer;
    private bool _animateFolderOnRender;
    private string TileDisplayName(Tile tile)
    {
        if (tile.IsFolder && string.IsNullOrEmpty(tile.Label)) return L.T(K.TileFolder);
        if (tile.Widget == "script") return _scriptPackages.DisplayName(tile, L.Language);
        var key = tile.Widget switch { "clock" => K.AppClock, "calendar" => K.AppCalendar, _ => (K?)null };
        if (key == null) return tile.Label;
        tile.UseLocalizedName ??= TextCatalog.SupportedLanguages.Any(language => tile.Label == TextCatalog.Translate(key.Value, language));
        return tile.UseLocalizedName == true ? L.T(key.Value) : tile.Label;
    }

    private InlineFolderView CreateInlineFolder(TilePanel parent)
    {
        var expansion = parent.Expansion!;
        var folder = expansion.Folder;
        var name = new GroupHeader(folder.Label, K.NameFolder, K.NameFolder) { Background = _compactTextSurface };
        name.Handle.Click += (_, _) => name.BeginEdit();
        WireInlineName(name, value =>
        {
            folder.Label = value;
            foreach (var button in GroupsPanel.Children.OfType<StackPanel>().SelectMany(s => s.Children.OfType<TilePanel>()).SelectMany(p => p.Children.OfType<Button>()).Where(b => b.Tag == folder))
            {
                button.ToolTip = TileDisplayName(folder);
                System.Windows.Automation.AutomationProperties.SetName(button, TileDisplayName(folder));
            }
        }, () => InlineFolderHeader(folder));
        var children = new TilePanel { Columns = parent.Columns, Cell = Cell, Placements = expansion.Children, FolderOwner = folder };
        foreach (var child in expansion.Children) children.Children.Add(CreateTileButton(child.Tile));
        var view = new InlineFolderView(name, children);
        var animate = _animateFolderOnRender && MotionEnabled;
        _animateFolderOnRender = false;
        if (animate)
        {
            _animateFolderOnRender = false;
            view.BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140)));
            var translation = new System.Windows.Media.TranslateTransform(0, -8);
            view.RenderTransform = translation;
            translation.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty,
                new System.Windows.Media.Animation.DoubleAnimation(-8, 0, TimeSpan.FromMilliseconds(140))
                { EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut } });
        }
        return view;
    }

    private GroupHeader? InlineFolderHeader(Tile folder) => GroupsPanel.Children.OfType<StackPanel>().SelectMany(s => s.Children.OfType<TilePanel>())
        .Where(p => p.Expansion?.Folder == folder).SelectMany(p => p.Children.OfType<InlineFolderView>())
        .Select(v => v.Child).OfType<StackPanel>().SelectMany(p => p.Children.OfType<GroupHeader>()).FirstOrDefault();

    private void WireInlineName(GroupHeader header, Action<string> update, Func<GroupHeader?> current)
    {
        header.EditStarted += () =>
        {
            if (_editingGroupHeader != header) _editingGroupHeader?.EndEdit(true);
            _editingGroupHeader = header;
        };
        header.EditEnded += (value, returnFocus) =>
        {
            if (_editingGroupHeader == header) _editingGroupHeader = null;
            if (value != null) { update(value); SaveLayout(); }
            if (!_groupsRenderPending) return;
            // Naming itself updates the existing controls. Pending icon refreshes
            // wait until the mouse is released so a following Click keeps its target.
            _nameRefreshTimer?.Stop();
            var refresh = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(50) };
            _nameRefreshTimer = refresh;
            refresh.Tick += (_, _) =>
            {
                if (_nameRefreshTimer != refresh) { refresh.Stop(); return; }
                if (_editingGroupHeader?.IsEditing == true || System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed) return;
                refresh.Stop();
                RenderGroups();
                if (returnFocus && IsActive && _editingGroupHeader == null) current()?.Handle.Focus();
            };
            refresh.Start();
        };
    }

    private void RenameFolder(Tile folder)
    {
        if (_openFolder != folder) OpenFolder(folder);
        InlineFolderHeader(folder)?.BeginEdit();
    }

    private void OpenFolder(Tile tile)
    {
        if (!tile.IsFolder || !Document.Groups.Any(g => g.Tiles.Contains(tile))) return;
        if (_openFolder == tile) { CloseFolder(this, new()); return; }
        _editingGroupHeader?.EndEdit(true);
        _openFolder = tile;
        _animateFolderOnRender = true;
        RenderGroups();
        Dispatcher.BeginInvoke(() =>
        {
            if (_openFolder != tile) return;
            var panel = GroupsPanel.Children.OfType<StackPanel>().SelectMany(s => s.Children.OfType<TilePanel>()).FirstOrDefault(p => p.Expansion?.Folder == tile);
            if (panel?.Children.OfType<InlineFolderView>().FirstOrDefault() is InlineFolderView view)
                view.BringIntoView(new Rect(0, 0, view.ActualWidth, Math.Min(view.ActualHeight, ContentScroll.ViewportHeight / 2)));
        }, DispatcherPriority.Loaded);
    }

    private void CloseFolder(object sender, RoutedEventArgs e)
    {
        if (_openFolder == null) return;
        foreach (var child in _openFolder.Children)
            if (_scriptSessions.TryGetValue(child.Id, out var session)) session.NextUpdate = DateTimeOffset.MinValue;
        _openFolder = null;
        _animateFolderOnRender = false;
        RenderGroups();
    }

    private void MoveTileIntoOpenFolder(Tile tile, Tile folder, int column, int row)
    {
        var source = Document.Groups.FirstOrDefault(g => g.Tiles.Any(t => t == tile || t.Children.Contains(tile)));
        var destination = Document.Groups.First(g => g.Tiles.Contains(folder));
        LayoutEditor.MoveIntoFolder(Document, tile, folder, column, row);
        if (source != destination && source?.Tiles.Count == 0) Document.Groups.Remove(source);
        CommitLayout();
    }
}
