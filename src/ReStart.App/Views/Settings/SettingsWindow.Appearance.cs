using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using ReStart.Core;

namespace ReStart.App;

internal sealed partial class SettingsWindow
{
    private TextBox _backgroundColor = null!, _accentColor = null!;
    private ComboBox _baseTheme = null!;
    private CheckBox _startAccent = null!;
    private bool _updatingColors;
    private readonly Dictionary<string, Button> _presetButtons = [];

    private void BuildAppearance(Panel content)
    {
        Select(content, L.T(K.SettingsStartMenuMode), new[] { L.T(K.StartMenuFullscreen), L.T(K.StartMenuCompact) },
            (int)_draft.StartMenuMode, i => _draft.StartMenuMode = (StartMenuMode)i);
        Check(content, L.T(K.SettingsTransparentCompactBackground), _draft.TransparentCompactBackground,
            value => _draft.TransparentCompactBackground = value);
        Slide(content, L.T(K.SettingsCompactTint), 0, 1, _draft.CompactBackgroundOpacity,
            value => _draft.CompactBackgroundOpacity = value, false);
        _baseTheme = Select(content, L.T(K.SettingsBaseTheme), new[] { L.T(K.ThemeSystem), L.T(K.ThemeDark), L.T(K.ThemeLight) }, (int)_draft.Theme, i =>
        {
            _draft.Theme = (ReStart.Core.ThemeMode)i;
            if (_backgroundColor != null)
            {
                if (!string.IsNullOrEmpty(_draft.ColorThemeId)) ApplyColorPreset(_draft.ColorThemeId);
                RefreshPresetPreviews();
            }
        });
        content.Children.Add(new TextBlock { Text = L.T(K.SettingsColorThemes), Margin = new(0, 12, 0, 8) });
        var presets = new WrapPanel();
        foreach (var preset in ThemePresets.All)
        {
            var palette = preset.Palette(_draft.Theme, ThemeWindow.SystemIsLight);
            var label = new StackPanel();
            label.Children.Add(new Border { Height = 4, Width = 28, Background = ColorBrush(palette.Accent), HorizontalAlignment = HorizontalAlignment.Left, Margin = new(0, 0, 0, 8) });
            label.Children.Add(new TextBlock { Text = L.T(preset.Name), TextWrapping = TextWrapping.Wrap });
            var button = new Button
            {
                Content = label,
                Width = 100,
                Height = 72,
                Margin = new(0, 0, 6, 6),
                Background = ColorBrush(palette.Background),
                Foreground = MainWindow.ReadableInk(ColorBrush(palette.Background).Color),
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            AutomationProperties.SetName(button, L.T(preset.Name));
            if (SystemParameters.HighContrast) { button.Background = SystemColors.WindowBrush; button.Foreground = SystemColors.WindowTextBrush; }
            button.Click += (_, _) => ApplyColorPreset(preset.Id);
            _presetButtons[preset.Id] = button; presets.Children.Add(button);
        }
        content.Children.Add(presets);
        _backgroundColor = ColorField(content, L.T(K.SettingsBackgroundColor), _draft.BackgroundColor, value =>
        { _draft.BackgroundColor = value; if (!_updatingColors) _draft.ColorThemeId = ""; if (value.Length > 0 && _startAccent != null) _startAccent.IsChecked = false; });
        _accentColor = ColorField(content, L.T(K.SettingsAccent), _draft.Accent, value => { _draft.Accent = value; if (!_updatingColors) _draft.ColorThemeId = ""; });
        Note(content, L.T(K.SettingsCustomColorsHelp));
        DataButton(content, L.T(K.SettingsResetColors), () => { _draft.ColorThemeId = ""; _backgroundColor.Text = ""; _accentColor.Text = "#0078D7"; _startAccent.IsChecked = false; });
        _startAccent = Check(content, L.T(K.SettingsStartAccent), _draft.ShowAccentOnStart, value =>
        { _draft.ShowAccentOnStart = value; if (value) { _draft.ColorThemeId = ""; _backgroundColor.Text = ""; } });
        Note(content, L.T(K.SettingsStartAccentHelp));
        BuildBackgroundImageSettings(content);
        Check(content, L.T(K.SettingsColoredTiles), _draft.ColoredTiles, v => _draft.ColoredTiles = v);
        Check(content, L.T(K.SettingsAppTileImages), _draft.UseAppTileImages, v => _draft.UseAppTileImages = v);
        Note(content, L.T(K.AppTileImagesHelp));
        Check(content, L.T(K.SettingsAnimation), _draft.Animate, v => _draft.Animate = v);
        Select(content, L.T(K.SettingsGroupWidth), new[] { L.T(K.ColumnsThree), L.T(K.ColumnsFour) }, _draft.Columns == 8 ? 1 : 0, i => _draft.Columns = i == 1 ? 8 : 6);
        Slide(content, L.T(K.SettingsTileSize), 0.7, 1.5, _draft.TileScale, v => _draft.TileScale = v, true);
        Slide(content, L.T(K.SettingsOpacity), 0.1, 1, _draft.BackgroundOpacity, v => _draft.BackgroundOpacity = v, false);
        Slide(content, L.T(K.SettingsBlur), 0, 50, _draft.BackgroundBlur, v => _draft.BackgroundBlur = v, true, percentage: false);
        Note(content, L.T(K.SettingsBlurHelp));
        RefreshPresetPreviews();
    }

    /// <summary>Selects a color family in the draft while retaining the user's base mode.</summary>
    /// <param name="id">A built-in family identity; its current light/dark palette is copied into the RGB fields.</param>
    private void ApplyColorPreset(string id)
    {
        ThemePresets.Apply(_draft, id, ThemeWindow.SystemIsLight);
        _updatingColors = true;
        try { _backgroundColor.Text = _draft.BackgroundColor; _accentColor.Text = _draft.Accent; _startAccent.IsChecked = false; }
        finally { _updatingColors = false; }
        RefreshPresetPreviews();
    }

    /// <summary>Updates family swatches to match the draft base mode without changing custom RGB input.</summary>
    private void RefreshPresetPreviews()
    {
        var systemLight = ThemeWindow.SystemIsLight;
        foreach (var preset in ThemePresets.All)
        {
            var palette = preset.Palette(_draft.Theme, systemLight); var button = _presetButtons[preset.Id];
            if (!SystemParameters.HighContrast) { button.Background = ColorBrush(palette.Background); button.Foreground = MainWindow.ReadableInk(ColorBrush(palette.Background).Color); }
            ((Border)((StackPanel)button.Content).Children[0]).Background = SystemParameters.HighContrast ? SystemColors.HighlightBrush : ColorBrush(palette.Accent);
            button.ToolTip = L.T(preset.Name) + " · " + L.T(_draft.Theme == ReStart.Core.ThemeMode.System ? K.ThemeSystem : _draft.Theme == ReStart.Core.ThemeMode.Light ? K.ThemeLight : K.ThemeDark);
        }
    }

    /// <summary>Builds a labelled RGB editor with a swatch; invalid intermediate input does not replace the draft value.</summary>
    /// <param name="panel">Parent section receiving the row.</param>
    /// <param name="label">Localized visual and accessible field label.</param>
    /// <param name="initial">Saved RGB text, or empty for the default background.</param>
    /// <param name="changed">Callback for subsequent valid/empty edits, receiving uppercase RGB text.</param>
    /// <returns>The editor used for validation and preset updates.</returns>
    private TextBox ColorField(Panel panel, string label, string initial, Action<string> changed)
    {
        var row = new DockPanel { Margin = new(0, 10, 0, 7) };
        row.Children.Add(new TextBlock { Text = label, Width = 170, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center });
        var preview = new Border { Width = 32, Height = 32, Margin = new(8, 0, 0, 0) };
        DockPanel.SetDock(preview, Dock.Right); row.Children.Add(preview);
        var text = new TextBox { Text = initial, MaxLength = 7, MinWidth = 130, HorizontalAlignment = HorizontalAlignment.Left };
        AutomationProperties.SetName(text, label);
        void Refresh(bool notify)
        {
            var color = text.Text.Trim();
            preview.Background = ThemePresets.IsColor(color) ? ColorBrush(color) : Brushes.Transparent;
            if (notify) DraftChanged();
            if (notify && (color.Length == 0 || ThemePresets.IsColor(color))) changed(color.ToUpperInvariant());
            if (_backgroundColor != null && _accentColor != null && (_backgroundColor.Text.Trim().Length == 0 || ThemePresets.IsColor(_backgroundColor.Text.Trim()))
                && ThemePresets.IsColor(_accentColor.Text.Trim()) && _feedback.Text == L.T(K.ErrorInvalidColor)) _feedback.Text = "";
        }
        text.TextChanged += (_, _) => Refresh(true); Refresh(false);
        row.Children.Add(text); panel.Children.Add(row); return text;
    }
    private bool ValidateColors()
    {
        var invalid = _backgroundColor.Text.Trim().Length > 0 && !ThemePresets.IsColor(_backgroundColor.Text.Trim()) ? _backgroundColor
            : !ThemePresets.IsColor(_accentColor.Text.Trim()) ? _accentColor : null;
        if (invalid == null) return true;
        SelectSection(L.T(K.SettingsAppearance)); _feedback.Text = L.T(K.ErrorInvalidColor); invalid.Focus(); return false;
    }
    private static SolidColorBrush ColorBrush(string color) => new((Color)ColorConverter.ConvertFromString(color));
}
