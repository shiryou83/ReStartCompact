using System.Text.Json;
using ReStart.Core;

namespace ReStart.App;

public partial class MainWindow
{
    private SettingsWindow? _preferencesPreviewOwner;
    private Preferences? _preferencesPreview;
    /// <summary>Gets visual draft values while Settings is open; persistence, hooks and startup keep using Document.Preferences.</summary>
    internal Preferences DisplayPreferences => _preferencesPreview ?? Document.Preferences;

    /// <summary>Renders a detached settings draft without changing saved preferences, layout models or integration callbacks.</summary>
    /// <param name="owner">Settings window owning this transient preview.</param>
    /// <param name="draft">Current draft, copied before rendering so subsequent edits cannot mutate the active presentation.</param>
    internal void PreviewPreferences(SettingsWindow owner, Preferences draft)
    {
        if (_preferencesPreviewOwner != null && _preferencesPreviewOwner != owner) throw new InvalidOperationException("Another settings preview is active.");
        var before = VisualPreferenceSignature(DisplayPreferences);
        _preferencesPreviewOwner = owner;
        _preferencesPreview = JsonSerializer.Deserialize<Preferences>(JsonSerializer.Serialize(draft))!;
        RefreshSettingsPresentation(before != VisualPreferenceSignature(DisplayPreferences));
    }

    /// <summary>Ends only the owner's preview and renders the latest committed preferences, including any successful save.</summary>
    /// <param name="owner">Window that began the preview; other windows cannot clear it.</param>
    internal void EndPreferencesPreview(SettingsWindow owner)
    {
        if (_preferencesPreviewOwner != owner) return;
        _preferencesPreviewOwner = null; _preferencesPreview = null;
        if (!_exit) RefreshSettingsPresentation(true);
    }

    private void RefreshSettingsPresentation(bool rebuildViews)
    {
        ApplyAppearance(); FitPanels();
        if (!rebuildViews) return;
        RenderGroups(); RenderApps(); RefreshSearchResults();
    }

    private static string VisualPreferenceSignature(Preferences p) => JsonSerializer.Serialize(new
    {
        p.Language,
        p.Theme,
        p.ColorThemeId,
        p.Accent,
        p.BackgroundColor,
        p.ShowAccentOnStart,
        p.TransparentCompactBackground,
        p.ColoredTiles,
        p.UseAppTileImages,
        p.Columns,
        p.TileScale,
        p.ShowLiveTileNames,
        p.TileHoverHighlight,
        p.NavigationFolders,
        p.ShowRecentlyAdded,
        p.ShowMostUsed
    });
}
