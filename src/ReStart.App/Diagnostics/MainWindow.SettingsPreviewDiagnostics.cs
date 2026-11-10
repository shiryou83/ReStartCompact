using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using ReStart.App.Controls;
using ReStart.Core;

namespace ReStart.App;

public partial class MainWindow
{
    /// <summary>Exercises native draft preview, close decisions and persistent ready notices using fictional settings only.</summary>
    /// <param name="snapshotPath">Owned smoke output path; related images use its directory.</param>
    /// <returns>Counts of assertions and language/theme/decision cases; no production hooks, startup registration or clipboard are touched.</returns>
    internal async Task<SettingsPreviewSmokeResult> VerifySettingsPreviewAsync(string snapshotPath)
    {
        if (!_preview) throw new InvalidOperationException("Settings verification requires preview mode.");
        var directory = Path.Combine(Path.GetDirectoryName(snapshotPath)!, "fixture-" + Guid.NewGuid().ToString("N"));
        var store = new LayoutStore(Path.Combine(directory, "layout.json"));
        store.Save(new()
        {
            Preferences = new() { Language = "en", ShowWallpaper = false, Animate = false, ColorThemeId = "" },
            Groups = [new() { Name = "Sample apps", Tiles = [new() { Label = "Sample editor", AppId = "editor" }] }]
        });
        var fixture = new MainWindow(store, true, () => Task.FromResult(new List<AppEntry> { new("editor", "Sample editor", "") }))
        { Owner = this, Width = 1280, Height = 800, Topmost = false };
        var checks = 0; var cases = 0; var language = L.Language;
        void Check(bool value, string message) { Require(value, message); checks++; }
        T Named<T>(DependencyObject root, TextKey key) where T : DependencyObject
        {
            if (root is FrameworkElement element) element.UpdateLayout();
            return VisualChildren<T>(root).SingleOrDefault(item => AutomationProperties.GetName(item) == L.T(key))
                ?? throw new InvalidOperationException("Missing settings control: " + key);
        }
        try
        {
            fixture.Present(); await fixture.InitializeAsync(); fixture._clock.Stop();
            foreach (var tag in L.SupportedLanguages)
                foreach (var mode in new[] { ReStart.Core.ThemeMode.Dark, ReStart.Core.ThemeMode.Light })
                    foreach (var decision in new[] { SettingsCloseChoice.Discard, SettingsCloseChoice.Save })
                    {
                        fixture.Document.Preferences = new()
                        {
                            Language = tag,
                            Theme = mode == ReStart.Core.ThemeMode.Dark ? ReStart.Core.ThemeMode.Light : ReStart.Core.ThemeMode.Dark,
                            ShowWallpaper = false,
                            Animate = false,
                            ColorThemeId = "",
                            TabletHideTaskbar = true
                        };
                        fixture.RefreshLayout(); store.Save(fixture.Document);
                        var original = JsonSerializer.Serialize(fixture.Document.Preferences); var originalGroups = JsonSerializer.Serialize(fixture.Document.Groups);
                        var persisted = File.ReadAllBytes(store.FilePath); var integrations = 0; fixture.PreferencesChanged = () => integrations++;
                        var editor = new SettingsWindow(fixture, store) { Owner = fixture };
                        Exception? error = null;
                        editor.Loaded += (_, _) => editor.Dispatcher.BeginInvoke(() =>
                        {
                            try
                            {
                                Check(!editor.HasPendingChanges, "An untouched settings editor has unsaved changes.");
                                editor.SelectSection(L.T(K.SettingsStartup));
                                Named<CheckBox>(editor, K.SettingsShowTaskbar).IsChecked = false;
                                Check(!VisualChildren<CheckBox>(editor).Any(item => AutomationProperties.GetName(item) == L.T(K.SettingsTabletHide)),
                                    "The removed independent taskbar hide setting is still offered.");
                                editor.SelectSection(L.T(K.SettingsAppearance));
                                Named<CheckBox>(editor, K.SettingsAppTileImages).IsChecked = false;
                                Named<ComboBox>(editor, K.SettingsBaseTheme).SelectedIndex = (int)mode;
                                Named<ComboBox>(editor, K.SettingsGroupWidth).SelectedIndex = 1;
                                Named<Slider>(editor, K.SettingsTileSize).Value = 1.25;
                                Named<Slider>(editor, K.SettingsOpacity).Value = .1;
                                Named<Slider>(editor, K.SettingsCompactTint).Value = .4;
                                Named<Slider>(editor, K.SettingsBlur).Value = 0;
                                editor.ApplyDraftPreview(); fixture.UpdateLayout();
                                Check(!fixture.DisplayPreferences.UseAppTileImages && fixture.Document.Preferences.UseAppTileImages,
                                    "The app artwork preview changed committed preferences or failed to apply.");
                                Check(fixture.DisplayPreferences.Theme == mode && fixture.GroupsPanel.TileColumns == 8 && Math.Abs(fixture.Cell - 65) < .01
                                    && fixture.Wallpaper.Effect == null, "Draft appearance and tile geometry were not rendered before saving.");
                                Check(((SolidColorBrush)fixture.Tint.Background).Color.A == (SystemParameters.HighContrast ? 255 : 25), "Draft tint was not applied.");
                                Check(fixture.DisplayPreferences.CompactBackgroundOpacity == .4 && fixture.Document.Preferences.CompactBackgroundOpacity == .65,
                                    "Compact tint did not follow the draft independently of wallpaper opacity.");
                                editor.SelectSection(L.T(K.SettingsLanguage));
                                Named<ComboBox>(editor, K.SettingsDisplayLanguage).SelectedIndex = tag == "en" ? 2 : 1;
                                editor.ApplyDraftPreview();
                                Check(editor.Title == L.T(K.SettingsTitle) && L.Language == (tag == "en" ? "ja" : "en"), "Changing language did not rebuild the open editor.");
                                Named<ComboBox>(editor, K.SettingsDisplayLanguage).SelectedIndex = 1 + TextCatalog.SupportedLanguages.ToList().IndexOf(tag);
                                editor.ApplyDraftPreview();
                                Check(L.Language == tag && editor.Title == L.T(K.SettingsTitle) && editor.HasPendingChanges, "Language preview lost the draft or the active language section.");
                                editor.SelectSection(L.T(K.SettingsStartup)); editor.UpdateLayout();
                                Check(Named<CheckBox>(editor, K.SettingsShowTaskbar).IsChecked == false, "Rebuilding settings lost the independent taskbar choice.");
                                Check(!VisualChildren<CheckBox>(editor).Any(item => AutomationProperties.GetName(item) == L.T(K.SettingsTabletHide)),
                                    "Rebuilding translated settings restored the removed taskbar hide setting.");
                                if (tag == "en" && decision == SettingsCloseChoice.Discard)
                                    SaveStartupScreenshot(editor, Path.Combine(directory, $"tablet-settings-{mode}.png"));
                                Check(JsonSerializer.Serialize(fixture.Document.Preferences) == original && File.ReadAllBytes(store.FilePath).SequenceEqual(persisted)
                                    && JsonSerializer.Serialize(fixture.Document.Groups) == originalGroups && integrations == 0, "Preview changed saved models, disk or integration callbacks.");
                                fixture.SaveLayout();
                                Check(JsonSerializer.Serialize(store.Load()!.Preferences) == original, "An unrelated layout save persisted visual draft preferences.");
                                QueueDecision(SettingsCloseChoice.KeepEditing, closeWithX: true); editor.Close();
                                Check(editor.IsVisible && editor.HasPendingChanges && fixture.DisplayPreferences.TileScale == 1.25, "Cancelling close discarded the draft or closed the editor.");
                                QueueDecision(decision); editor.Close();
                            }
                            catch (Exception ex) { error = ex; editor.DiscardAndClose(); }
                        });
                        editor.ShowDialog(); if (error != null) throw new InvalidOperationException("Settings draft fixture failed.", error);
                        Check(fixture._preferencesPreview == null, "A closed editor retained its display overlay.");
                        Check(fixture.Document.Preferences.UseAppTileImages == (decision != SettingsCloseChoice.Save)
                            && store.Load()!.Preferences.UseAppTileImages == (decision != SettingsCloseChoice.Save),
                            "The app artwork switch did not follow Settings save/discard.");
                        Check(store.Load()!.Preferences.CompactBackgroundOpacity == (decision == SettingsCloseChoice.Save ? .4 : .65),
                            "Compact tint did not follow Settings save/discard.");
                        Check(decision == SettingsCloseChoice.Save ? fixture.Document.Preferences.TileScale == 1.25 && store.Load()!.Preferences.Columns == 8
                            && store.Load()!.Preferences.TabletHideTaskbar && !store.Load()!.Preferences.ShowTaskbarWhileOpen
                            : JsonSerializer.Serialize(fixture.Document.Preferences) == original && JsonSerializer.Serialize(store.Load()!.Preferences) == original,
                            "Save/discard returned the wrong committed settings.");
                        Check(JsonSerializer.Serialize(fixture.Document.Groups) == originalGroups, "Settings preview altered tile coordinates or membership.");
                        cases++;

                        void QueueDecision(SettingsCloseChoice choice, bool closeWithX = false)
                        {
                            editor.Dispatcher.InvokeAsync(() =>
                            {
                                var prompt = Application.Current.Windows.OfType<UnsavedSettingsWindow>().Single();
                                try
                                {
                                    prompt.UpdateLayout();
                                    Check(prompt.KeepButton.IsDefault && prompt.KeepButton.IsCancel && prompt.KeepButton.IsKeyboardFocused, "Unsaved confirmation does not default to continued editing.");
                                    Check(((SolidColorBrush)prompt.Background).Color == ((SolidColorBrush)fixture.FindResource("PanelBrush")).Color
                                        && prompt.Title == L.T(K.SettingsUnsavedTitle), "Unsaved confirmation lost preview language or palette.");
                                    Check(new[] { prompt.KeepButton, prompt.DiscardButton, prompt.SaveButton }.All(button =>
                                        button.TranslatePoint(new(button.ActualWidth, button.ActualHeight), prompt).X <= prompt.ActualWidth + 1), "A translated close decision is clipped.");
                                    if (tag == "en" && decision == SettingsCloseChoice.Discard && choice == SettingsCloseChoice.KeepEditing)
                                        SaveStartupScreenshot(prompt, Path.Combine(directory, $"unsaved-{mode}.png"));
                                    if (closeWithX) prompt.Close();
                                    else (choice == SettingsCloseChoice.Save ? prompt.SaveButton : choice == SettingsCloseChoice.Discard ? prompt.DiscardButton : prompt.KeepButton)
                                        .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                                }
                                catch (Exception ex) { error = ex; prompt.Close(); }
                            }, System.Windows.Threading.DispatcherPriority.ContextIdle);
                        }
                    }
            fixture.Document.Preferences = new() { Language = "en", ShowWallpaper = false, Animate = false, ColorThemeId = "" }; fixture.RefreshLayout(); store.Save(fixture.Document);
            var failed = new SettingsWindow(fixture, store) { Owner = fixture }; Exception? failure = null;
            failed.Loaded += (_, _) => failed.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    failed.SelectSection(L.T(K.SettingsAppearance)); Named<TextBox>(failed, K.SettingsBackgroundColor).Text = "invalid";
                    Check(failed.HasPendingChanges, "Invalid in-progress color input was not treated as an unsaved edit.");
                    Named<TextBox>(failed, K.SettingsBackgroundColor).Text = "#123456"; failed.ApplyDraftPreview();
                    using var locked = new FileStream(store.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    failed.Dispatcher.InvokeAsync(() => Application.Current.Windows.OfType<UnsavedSettingsWindow>().Single().SaveButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)), System.Windows.Threading.DispatcherPriority.ContextIdle);
                    failed.Close();
                    Check(failed.IsVisible && failed.HasPendingChanges && fixture.Document.Preferences.BackgroundColor == "", "A failed save closed the editor or committed the preview.");
                }
                catch (Exception ex) { failure = ex; }
                finally { failed.DiscardAndClose(); }
            });
            failed.ShowDialog(); if (failure != null) throw failure;
            checks += VerifyAvailableUpdateNavigation(fixture, store, directory);
            checks += VerifyProfileBackupSettings(fixture, store, directory);
            var readyEvents = 0; fixture.PreparedUpdateAvailable += () => readyEvents++;
            fixture.PreparedUpdate = new(Guid.NewGuid().ToString("N"), directory, "9.0.0"); fixture.UpdatePreparation = PreparationState.Ready; fixture.PresentPreparedUpdate();
            Check(readyEvents == 1 && fixture._updateNotice != null, "Finishing preparation did not announce a persistent ready notice.");
            fixture._updateNotice!.LaterButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(fixture._updateNotice.LaterButton.Visibility == Visibility.Collapsed && fixture.ContentArea.Children.Contains(fixture._updateNotice), "Not now removed the ready indicator.");
            fixture.UpdateLayout(); fixture.SaveScreenshot(Path.Combine(directory, "update-ready-compact.png"));
            var readySettings = new SettingsWindow(fixture, store) { Owner = fixture }; Exception? readyError = null;
            readySettings.Loaded += (_, _) => readySettings.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    Check(VisualChildren<Button>(readySettings).Any(button => AutomationProperties.GetName(button).Contains(L.T(K.UpdateReadyTitle), StringComparison.Ordinal)),
                        "Settings navigation did not expose prepared-update readiness.");
                    readySettings.SelectSection(L.T(K.SettingsUpdates)); readySettings.UpdateLayout();
                    Check(VisualChildren<TextBlock>(readySettings).Any(text => text.Text == L.F(K.UpdatePrepared, "9.0.0")), "Updates settings lost the prepared version after reopening.");
                }
                catch (Exception ex) { readyError = ex; }
                finally { readySettings.DiscardAndClose(); }
            });
            readySettings.ShowDialog(); if (readyError != null) throw readyError;
            foreach (var tag in L.SupportedLanguages)
            {
                fixture.Document.Preferences.Language = tag; fixture.ApplyAppearance();
                Check(Equals(fixture._updateNotice!.ActionButton.Content, L.T(K.UpdateRestart)) && fixture._updateNotice.LaterButton.Visibility == Visibility.Collapsed,
                    "Changing language lost the ready action or expanded its compact state.");
            }
            fixture.Document.Preferences.Language = "en"; fixture.ApplyAppearance();
            fixture._readyNoticeExpanded = true; fixture.RefreshPreparedUpdateNotice(); fixture.UpdateLayout();
            fixture.SaveScreenshot(Path.Combine(directory, "update-ready.png"));
            fixture.PreparedUpdate = null; fixture.UpdatePreparation = PreparationState.Idle;
            return new(checks, cases);
        }
        finally { fixture.Exit(); L.SetLanguage(language); ApplyAppearance(); Activate(); }
    }
}

internal sealed record SettingsPreviewSmokeResult(int Checks, int LanguageThemeDecisionCases);
