using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ReStart.App.Services;
using ReStart.Core;

namespace ReStart.App;

public partial class MainWindow
{
    /// <summary>Verifies version, licenses, migration and both-language settings views without network or production credentials.</summary>
    internal async Task<ReleaseInfoSmokeResult> VerifyReleaseInfoAsync(string snapshotPath)
    {
        if (!_preview) throw new InvalidOperationException("Release-info verification requires preview mode.");
        var directory = Path.Combine(Path.GetDirectoryName(snapshotPath)!, "fixture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "layout.json");
        const string legacy = "{\"Version\":2,\"SavedWithAppVersion\":\"0.4.0\",\"Preferences\":{\"Language\":\"en\",\"CaptureWindowsKey\":false},\"Groups\":[]}";
        File.WriteAllText(path, legacy);
        var store = new LayoutStore(path, AppBuildInfo.Version);
        var document = store.Load()!; store.Save(document);
        Require(document.Version == LayoutDocument.CurrentVersion && !document.Preferences.CaptureWindowsKey
            && document.LastUpgrade?.FromSchema == 2, "Migration changed preferences or failed to record its source schema.");
        Require(File.ReadAllText(store.UpgradeBackupPath!) == legacy, "The upgrade backup does not preserve the original settings.");
        var fixture = new MainWindow(store, true, () => Task.FromResult(new List<AppEntry> { new("fixture", "Fixture", "", false) }))
        { Owner = this, Width = 1100, Height = 800, Topmost = false };
        var originalLanguage = L.Language;
        try
        {
            fixture.Present(); await fixture.InitializeAsync();
            fixture.Document.Preferences.Theme = ReStart.Core.ThemeMode.Dark;
            fixture.Document.Preferences.ShowAccentOnStart = true;
            fixture.Document.Preferences.Accent = "#0078D7";
            fixture.ApplyAppearance();
            var blue = ((SolidColorBrush)fixture.Root.Background).Color;
            fixture.Document.Preferences.Accent = "#E81123";
            fixture.ApplyAppearance();
            var red = ((SolidColorBrush)fixture.Root.Background).Color;
            Require(SystemParameters.HighContrast || blue != red && red.R > red.B, "The Start surface did not follow the selected accent color.");
            fixture.Document.Preferences.ColoredTiles = true;
            var first = fixture.CreateTileButton(new() { AppId = "first", Color = "#107C10" });
            var second = fixture.CreateTileButton(new() { AppId = "second", Color = "#744DA9" });
            Require(SystemParameters.HighContrast || ((SolidColorBrush)first.Background).Color == ((SolidColorBrush)second.Background).Color
                && ((SolidColorBrush)first.Background).Color == ((SolidColorBrush)fixture.FindResource("AccentBrush")).Color,
                "Classic app tiles retained per-tile colors instead of using the global accent.");
            fixture.Document.Preferences.ColoredTiles = false;
            Require(SystemParameters.HighContrast || ((SolidColorBrush)fixture.CreateTileButton(new()).Background).Color.A < 255,
                "Modern app tiles do not use uniform translucent backplates.");
            fixture.Document.Groups = [new() { Name = "Accent sample", Tiles = [new() { Label = "Sample A" }, new() { Label = "Sample B", Column = 2, Row = 0 }] }];
            fixture.RefreshLayout();
            fixture.SaveScreenshot(Path.Combine(Path.GetDirectoryName(snapshotPath)!, "start-accent.png"));
            var customTile = fixture.Document.Groups[0].Tiles[0];
            var neighbor = fixture.Document.Groups[0].Tiles[1];
            fixture.SetTileColor(customTile, "#FFFFFF");
            Button TileButton(Tile tile) => VisualChildren<Button>(fixture.GroupsPanel).Single(b => b.Tag == tile);
            Require(SystemParameters.HighContrast || TileButton(customTile).Foreground == Brushes.Black, "A pale custom tile does not choose readable dark text.");
            var extras = fixture.TileMenu(customTile).Items.OfType<MenuItem>().Single(i => i.Header is "ReStart");
            var colorMenu = extras.Items.OfType<MenuItem>().Single(i => (string)i.Header == L.T(K.TileBackgroundColor));
            var swatch = colorMenu.Items.OfType<MenuItem>().Single(i => i.Header is "#107C10");
            swatch.Style = (Style)fixture.FindResource(typeof(MenuItem)); swatch.ApplyTemplate();
            Require(swatch.Template.FindName("Icon", swatch) is ContentPresenter icon && icon.Content == swatch.Icon, "The palette color swatch is ignored by the menu template.");
            swatch.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Require(customTile.UseCustomColor && customTile.Color == "#107C10" && !neighbor.UseCustomColor
                && (SystemParameters.HighContrast || ((SolidColorBrush)TileButton(customTile).Background).Color != ((SolidColorBrush)TileButton(neighbor).Background).Color),
                "Changing one tile's color affected another tile or did not render.");
            Require(LayoutStore.Read(path).Groups[0].Tiles[0].UseCustomColor, "The selected individual color was not persisted.");
            colorMenu = fixture.TileColorMenu(customTile);
            colorMenu.Items.OfType<MenuItem>().First().RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Require(!customTile.UseCustomColor && (SystemParameters.HighContrast || ((SolidColorBrush)TileButton(customTile).Background).Color.A < 255),
                "Resetting a tile did not restore its theme color.");
            SelectTileColor(customTile, K.TileThemeAccent);
            SelectTileColor(neighbor, K.TileThemeColor);
            var savedColors = LayoutStore.Read(path).Groups[0].Tiles;
            Require(savedColors[0].ThemeColor == TileThemeColor.Accent && savedColors[1].ThemeColor == TileThemeColor.Background &&
                !savedColors[0].UseCustomColor && !savedColors[1].UseCustomColor,
                "Selecting a theme swatch stored a fixed color or changed the other tile.");
            foreach (var theme in new[] { "forest", "rose" })
            {
                ThemePresets.Apply(fixture.Document.Preferences, theme);
                fixture.RefreshLayout();
                var palette = ThemePresets.Resolve(fixture.Document.Preferences, false);
                Require(SystemParameters.HighContrast ||
                    ((SolidColorBrush)TileButton(customTile).Background).Color == (Color)ColorConverter.ConvertFromString(palette.Accent) &&
                    ((SolidColorBrush)TileButton(neighbor).Background).Color == (Color)ColorConverter.ConvertFromString(palette.Background),
                    "Theme tiles did not follow the palette after changing themes.");
            }
            SelectTileColor(neighbor, K.TileAutomaticColor);
            Require(neighbor.ThemeColor == TileThemeColor.Automatic && customTile.ThemeColor == TileThemeColor.Accent,
                "Returning one tile to automatic changed the other tile's theme selection.");
            fixture.SetTileColor(customTile, "#107C10");
            fixture.SaveScreenshot(Path.Combine(Path.GetDirectoryName(snapshotPath)!, "individual-tile-color.png"));
            fixture.Document.Preferences.ShowAccentOnStart = false;

            void SelectTileColor(Tile tile, TextKey key) => fixture.TileColorMenu(tile).Items.OfType<MenuItem>()
                .Single(item => item.Header is string label && label == L.T(key)).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

            foreach (var language in TextCatalog.SupportedLanguages)
                foreach (var theme in new[] { ReStart.Core.ThemeMode.Dark, ReStart.Core.ThemeMode.Light })
                {
                    fixture.Document.Preferences.Language = language; fixture.Document.Preferences.Theme = theme; fixture.ApplyAppearance();
                    var settings = new SettingsWindow(fixture, store) { Owner = fixture };
                    try
                    {
                        settings.Show(); settings.SelectSection(L.T(K.SettingsAbout)); settings.UpdateLayout();
                        Require(VisualChildren<TextBlock>(settings).Any(t => t.Text.Contains(AppBuildInfo.Version, StringComparison.Ordinal)), "About does not display the current app version.");
                        string? copied = null;
                        var clipboardBusy = false;
                        var diagnostics = new DiagnosticsWindow(settings, fixture, text =>
                        {
                            if (clipboardBusy) throw new System.Runtime.InteropServices.ExternalException();
                            copied = text;
                        });
                        try
                        {
                            diagnostics.Show(); diagnostics.UpdateLayout();
                            Require(diagnostics.Report.IsReadOnly && diagnostics.Report.Text.Contains("reportVersion", StringComparison.Ordinal), "The diagnostic preview is missing or editable.");
                            diagnostics.Copy.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                            Require(copied == diagnostics.Report.Text && !copied.Contains("Fixture", StringComparison.Ordinal)
                                && !copied.Contains(directory, StringComparison.OrdinalIgnoreCase), "Copy did not match the preview or included fixture names/paths.");
                            clipboardBusy = true; diagnostics.Width = 440;
                            diagnostics.Copy.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); diagnostics.UpdateLayout();
                            var feedback = VisualChildren<TextBlock>(diagnostics).Single(t => t.Text == L.T(K.DiagnosticsClipboardBusy));
                            Require(feedback.ActualHeight > 0 && diagnostics.Copy.TranslatePoint(new(), diagnostics).X + diagnostics.Copy.ActualWidth <= diagnostics.ActualWidth,
                                "Clipboard-busy feedback pushed the copy action outside a narrow dialog.");
                            diagnostics.Width = 650; diagnostics.UpdateLayout();
                            if (language is "en" or "ja") SaveFixtureElement(diagnostics, Path.Combine(Path.GetDirectoryName(snapshotPath)!, $"diagnostics-{language}-{theme}.png"));
                        }
                        finally { diagnostics.Close(); }
                        SaveFixtureElement(settings, Path.Combine(Path.GetDirectoryName(snapshotPath)!, $"about-{language}-{theme}.png"));
                        settings.SelectSection(L.T(K.SettingsUpdates)); settings.UpdateLayout();
                        var updateButton = VisualChildren<Button>(settings).Single(b => b.Content is string label && label == L.T(K.UpdateCheckNow));
                        Require(!updateButton.IsEnabled && !VisualChildren<PasswordBox>(settings).Any(),
                            "Preview can make network requests or the retired credential input is still present.");
                        Require(!VisualChildren<Button>(settings).Single(b => b.Content is string label && label == L.T(K.UpdateDownload)).IsEnabled,
                            "Preview enabled update package downloads.");
                        foreach (var state in Enum.GetValues<PreparationState>())
                        {
                            fixture.PreparedUpdate = new("fictional", directory, "2.0.0"); fixture.UpdatePreparation = state; fixture.UpdateBytes = 5 * 1024 * 1024;
                            fixture.UpdateCheckChanged?.Invoke(); settings.UpdateLayout();
                            var restart = VisualChildren<Button>(settings).Single(b => b.Content is string label && label == L.T(K.UpdateRestart));
                            Require((restart.Visibility == Visibility.Visible) == (state == PreparationState.Ready) && !restart.IsEnabled,
                                "A preparation state exposed the wrong restart action or allowed preview installation.");
                            if (state == PreparationState.Ready && language is "en" or "ja")
                                SaveFixtureElement(settings, Path.Combine(Path.GetDirectoryName(snapshotPath)!, $"update-ready-{language}-{theme}.png"));
                        }
                        fixture.UpdatePreparation = PreparationState.Idle; fixture.PreparedUpdate = null; fixture.UpdateCheckChanged?.Invoke();
                        SaveFixtureElement(settings, Path.Combine(Path.GetDirectoryName(snapshotPath)!, $"updates-{language}-{theme}.png"));
                        settings.SelectSection(L.T(K.SettingsLayout)); settings.UpdateLayout();
                        SaveFixtureElement(settings, Path.Combine(Path.GetDirectoryName(snapshotPath)!, $"migration-{language}-{theme}.png"));
                        if (language == "en" && theme == ReStart.Core.ThemeMode.Dark)
                        {
                            var license = new LicenseWindow(settings);
                            try { license.Show(); license.UpdateLayout(); SaveFixtureElement(license, Path.Combine(Path.GetDirectoryName(snapshotPath)!, "licenses.png")); }
                            finally { license.Close(); }
                        }
                    }
                    finally { settings.DiscardAndClose(); }
                }
            var wallpaperChecks = VerifyWallpaperRefresh(fixture, directory);
            var beforeDeferredSave = File.ReadAllText(path); var savedName = fixture.Document.Groups[0].Name;
            fixture.DeferLayoutSaves = true; fixture.Document.Groups[0].Name = "Deferred update sentinel"; fixture.SaveLayout();
            Require(File.ReadAllText(path) == beforeDeferredSave, "An unacknowledged replacement wrote newer settings.");
            fixture.Document.Groups[0].Name = savedName; fixture.DeferLayoutSaves = false;
            var licenses = AppBuildInfo.Licenses();
            Require(licenses.TryGetValue("ReStart (MIT)", out var own) && own.Contains("MIT License", StringComparison.Ordinal) && licenses.Count >= 4,
                "The embedded app and dependency license texts are missing.");
            Require(LayoutStore.Read(path).LastUpgrade?.BackupFile == document.LastUpgrade!.BackupFile, "Reopening lost the durable migration record.");
            return new(AppBuildInfo.Version, LayoutDocument.CurrentVersion, licenses.Count, TextCatalog.SupportedLanguages.Count * 2, 3, 3, 9, wallpaperChecks);
        }
        finally { fixture.Exit(); LocalizationResources.Apply(originalLanguage); ApplyAppearance(); Activate(); }
    }

    private static int VerifyWallpaperRefresh(MainWindow fixture, string directory)
    {
        var path = Path.Combine(directory, "wallpaper.png");
        static void WriteImage(string path, byte blue, byte red)
        {
            var pixels = Enumerable.Range(0, 32 * 32).SelectMany(_ => new byte[] { blue, 0, red, 255 }).ToArray();
            var image = BitmapSource.Create(32, 32, 96, 96, PixelFormats.Bgra32, null, pixels, 32 * 4);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
            using var stream = File.Create(path); encoder.Save(stream);
        }
        fixture.Document.Preferences.ShowWallpaper = true;
        WriteImage(path, 0, 255); fixture.RefreshWallpaper(path);
        if (SystemParameters.HighContrast) { Require(fixture.Wallpaper.Source == null, "High contrast did not disable wallpaper."); return 1; }
        var original = fixture.Wallpaper.Source;
        Require(original is BitmapSource, "Wallpaper did not load without saving settings.");
        using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        WriteImage(path, 255, 0); File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(2));
        fixture.RefreshWallpaper(path);
        var pixels = new byte[4];
        ((BitmapSource)fixture.Wallpaper.Source).CopyPixels(new Int32Rect(0, 0, 1, 1), pixels, 4, 0);
        Require(fixture.Wallpaper.Source != original && pixels[0] == 255 && pixels[2] == 0, "A wallpaper replaced at the same path is still cached.");
        fixture.Document.Preferences.ShowWallpaper = false; fixture.RefreshWallpaper(path);
        Require(fixture.Wallpaper.Source == null, "Disabling wallpaper retained the old image.");
        var images = new BackgroundImageStore(directory);
        Require(images.Resolve("../wallpaper") == null, "Custom wallpaper accepted a path instead of an image identity.");
        var id = new BackgroundImageStore(Path.GetDirectoryName(fixture._store.FilePath)!).Import(path);
        fixture.Document.Preferences.CustomBackgroundId = id;
        fixture.Document.Preferences.UseCustomBackground = fixture.Document.Preferences.ShowWallpaper = true;
        File.Delete(path);
        fixture.RefreshWallpaper();
        Require(fixture.Wallpaper.Source is BitmapSource, "The custom image still depends on its original desktop file.");
        var normalized = new LayoutDocument { Preferences = new() { CustomBackgroundId = "../wallpaper", UseCustomBackground = true } };
        normalized.Normalize(); Require(normalized.Preferences.CustomBackgroundId == "", "A malformed custom image identity survived normalization.");
        fixture.Document.Preferences.ShowWallpaper = false; fixture.Document.Preferences.UseCustomBackground = false;
        fixture.RefreshWallpaper();
        return 7;
    }

    private static IEnumerable<T> VisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var descendant in VisualChildren<T>(child)) yield return descendant;
        }
    }
    private static void SaveFixtureElement(FrameworkElement element, string path)
    {
        element.UpdateLayout();
        var drawing = new DrawingVisual();
        var client = element is Window { Content: FrameworkElement content } ? content : element;
        var margin = client.Margin;
        var size = new Size(client.ActualWidth + margin.Left + margin.Right, client.ActualHeight + margin.Top + margin.Bottom);
        using (var dc = drawing.RenderOpen())
        {
            dc.DrawRectangle(element is Window window ? window.Background : Brushes.Transparent, null, new Rect(size));
            dc.DrawRectangle(new VisualBrush(client), null, new Rect(margin.Left, margin.Top, client.ActualWidth, client.ActualHeight));
        }
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(drawing);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); png.Save(stream);
    }
}

internal sealed record ReleaseInfoSmokeResult(string Version, int Schema, int Licenses, int LanguageThemeCases, int MigrationChecks, int AccentChecks, int TileColorChecks, int WallpaperChecks);
