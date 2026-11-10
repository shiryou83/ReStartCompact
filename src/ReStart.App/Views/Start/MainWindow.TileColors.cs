using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ReStart.Core;

namespace ReStart.App;

public partial class MainWindow
{
    private MenuItem TileColorMenu(Tile tile)
    {
        var colors = new MenuItem { Header = L.T(K.TileBackgroundColor) };
        var reset = new MenuItem { Header = L.T(K.TileAutomaticColor), IsCheckable = true, IsChecked = !HasTileColorOverride(tile) };
        reset.Click += (_, _) => SetTileColor(tile, null);
        colors.Items.Add(reset);
        AddThemeSwatch(K.TileThemeColor, TileThemeColor.Background);
        AddThemeSwatch(K.TileThemeAccent, TileThemeColor.Accent);
        colors.Items.Add(new Separator());
        foreach (var color in new[] { "#0078D7", "#0063B1", "#008272", "#107C10", "#744DA9", "#B146C2", "#D83B01", "#E81123" })
        {
            var item = new MenuItem
            {
                Header = color,
                IsCheckable = true,
                IsChecked = tile.UseCustomColor && string.Equals(tile.Color, color, StringComparison.OrdinalIgnoreCase),
                Icon = ColorSwatch(Brush(color, "#0078D7"))
            };
            item.Click += (_, _) => SetTileColor(tile, color);
            colors.Items.Add(item);
        }
        Add(colors, L.T(K.TileCustomColor), () =>
        {
            var selected = (TileOverrideBrush(tile) ?? ThemeTileBrush(TileThemeColor.Accent)).Color;
            var value = Prompt(L.T(K.PromptTileColor), TileColorHex(selected));
            if (value == null) return;
            if (!ThemePresets.IsColor(value)) { SetStatus(L.T(K.ErrorInvalidColor)); return; }
            SetTileColor(tile, value);
        });
        return colors;

        void AddThemeSwatch(TextKey label, TileThemeColor theme)
        {
            var item = new MenuItem
            {
                Header = L.T(label),
                IsCheckable = true,
                IsChecked = !tile.UseCustomColor && tile.ThemeColor == theme,
                Icon = ColorSwatch(ThemeTileBrush(theme))
            };
            item.Click += (_, _) => SetTileColor(tile, null, theme);
            colors.Items.Add(item);
        }
    }

    private Border ColorSwatch(Brush color) => new()
    {
        Width = 18,
        Height = 18,
        Background = color,
        BorderThickness = new(1),
        BorderBrush = (Brush)FindResource("BorderBrush")
    };

    private static bool HasTileColorOverride(Tile tile) => tile.UseCustomColor || tile.ThemeColor != TileThemeColor.Automatic;

    private SolidColorBrush ThemeTileBrush(TileThemeColor theme) =>
        (SolidColorBrush)FindResource(theme == TileThemeColor.Accent ? "AccentBrush" : "PanelBrush");

    private SolidColorBrush? TileOverrideBrush(Tile tile) => tile.UseCustomColor ? Brush(tile.Color, "#0078D7")
        : tile.ThemeColor != TileThemeColor.Automatic ? ThemeTileBrush(tile.ThemeColor) : null;

    private static string TileColorHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private void SetTileColor(Tile tile, string? color, TileThemeColor theme = TileThemeColor.Automatic)
    {
        if (color != null && !ThemePresets.IsColor(color)) throw new ArgumentException("Invalid RGB color.", nameof(color));
        tile.UseCustomColor = color != null;
        tile.Color = color?.ToUpperInvariant() ?? "";
        tile.ThemeColor = color == null ? theme : TileThemeColor.Automatic;
        CommitLayout();
    }
}
