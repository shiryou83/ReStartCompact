namespace ReStart.Core;

/// <summary>Stable, language-independent identifiers for all application-owned UI text.</summary>
public enum TextKey
{
    /// <summary>Save and approve</summary>
    TileDataApprove,
    /// <summary>Approval applies only to this tile and its enabled read-only connections. Disable a connection to revoke access. Changed package requests require approval again.</summary>
    TilePermissionHelp,
    /// <summary>Show an automatically hidden taskbar while ReStart is open</summary>
    SettingsShowTaskbar,
    /// <summary>Only the taskbar on ReStart's monitor is affected. Windows auto-hide settings stay unchanged.</summary>
    SettingsTaskbarHelp,
    /// <summary>Tablet integration</summary>
    SettingsTablet,
    /// <summary>Enable optional tablet integration</summary>
    SettingsTabletEnable,
    /// <summary>Pause tablet integration when a USB or Bluetooth keyboard is connected</summary>
    SettingsTabletKeyboard,
    /// <summary>Hide the taskbar while ReStart is open in tablet mode</summary>
    SettingsTabletHide,
    /// <summary>Uses Windows posture detection after you save. Your tiles and desktop Start shortcuts are preserved. Unknown posture does not activate tablet effects.</summary>
    SettingsTabletHelp,
    /// <summary>Wi-Fi</summary>
    WindowsSettingNetworkWifi,
    /// <summary>Ethernet</summary>
    WindowsSettingNetworkEthernet,
    /// <summary>VPN</summary>
    WindowsSettingNetworkVpn,
    /// <summary>Proxy</summary>
    WindowsSettingNetworkProxy,
    /// <summary>Airplane mode</summary>
    WindowsSettingNetworkAirplanemode,
    /// <summary>Mobile hotspot</summary>
    WindowsSettingNetworkMobilehotspot,
    /// <summary>Mouse</summary>
    WindowsSettingMousetouchpad,
    /// <summary>Typing</summary>
    WindowsSettingTyping,
    /// <summary>Printers and scanners</summary>
    WindowsSettingPrinters,
    /// <summary>USB</summary>
    WindowsSettingUsb,
    /// <summary>AutoPlay</summary>
    WindowsSettingAutoplay,
    /// <summary>Default apps</summary>
    WindowsSettingDefaultapps,
    /// <summary>Optional features</summary>
    WindowsSettingOptionalfeatures,
    /// <summary>Startup apps</summary>
    WindowsSettingStartupapps,
    /// <summary>Sign-in options</summary>
    WindowsSettingSigninoptions,
    /// <summary>Your info</summary>
    WindowsSettingYourinfo,
    /// <summary>Other users</summary>
    WindowsSettingOtherusers,
    /// <summary>Access work or school</summary>
    WindowsSettingWorkplace,
    /// <summary>Activation</summary>
    WindowsSettingActivation,
    /// <summary>About your PC</summary>
    WindowsSettingAbout,
    /// <summary>Notifications</summary>
    WindowsSettingNotifications,
    /// <summary>Multitasking</summary>
    WindowsSettingMultitasking,
    /// <summary>Clipboard</summary>
    WindowsSettingClipboard,
    /// <summary>Remote Desktop</summary>
    WindowsSettingRemotedesktop,
    /// <summary>Recovery</summary>
    WindowsSettingRecovery,
    /// <summary>Troubleshoot</summary>
    WindowsSettingTroubleshoot,
    /// <summary>Taskbar</summary>
    WindowsSettingTaskbar,
    /// <summary>Fonts</summary>
    WindowsSettingFonts,
    /// <summary>Lock screen</summary>
    WindowsSettingLockscreen,
    /// <summary>Themes</summary>
    WindowsSettingThemes,
    /// <summary>Start menu settings</summary>
    WindowsSettingPersonalizationStart,
    /// <summary>Region</summary>
    WindowsSettingRegionformatting,
    /// <summary>Speech</summary>
    WindowsSettingSpeech,
    /// <summary>Magnifier</summary>
    WindowsSettingEaseofaccessMagnifier,
    /// <summary>Narrator</summary>
    WindowsSettingEaseofaccessNarrator,
    /// <summary>Accessibility keyboard</summary>
    WindowsSettingEaseofaccessKeyboard,
    /// <summary>Accessibility mouse</summary>
    WindowsSettingEaseofaccessMouse,
    /// <summary>Captions</summary>
    WindowsSettingEaseofaccessClosedcaptioning,
    /// <summary>High contrast</summary>
    WindowsSettingEaseofaccessHighcontrast,
    /// <summary>Location privacy</summary>
    WindowsSettingPrivacyLocation,
    /// <summary>Camera privacy</summary>
    WindowsSettingPrivacyWebcam,
    /// <summary>Microphone privacy</summary>
    WindowsSettingPrivacyMicrophone,
    /// <summary>Diagnostics and feedback</summary>
    WindowsSettingPrivacyFeedback,
    /// <summary>Windows Security</summary>
    WindowsSettingWindowsdefender,
    /// <summary>Windows update history</summary>
    WindowsSettingWindowsupdateHistory,
    /// <summary>Windows Update advanced options</summary>
    WindowsSettingWindowsupdateOptions,
    /// <summary>Game Bar</summary>
    WindowsSettingGamingGamebar,
    /// <summary>Game Mode</summary>
    WindowsSettingGamingGamemode,

    /// <summary>Hibernate</summary>
    PowerHibernate,
    /// <summary>Update and shut down</summary>
    PowerUpdateShutdown,
    /// <summary>Update and restart</summary>
    PowerUpdateRestart,
    /// <summary>Switch user</summary>
    AccountSwitchUser,
    /// <summary>Search indexed file contents</summary>
    SearchFileContentsSetting,
    /// <summary>Show application-defined jump-list categories</summary>
    SettingsCustomJumpLists,
    /// <summary>Create full backup</summary>
    BackupCreate,
    /// <summary>Restore full backup</summary>
    BackupRestore,
    /// <summary>Includes layout, appearance, installed tiles, library drafts and background images. Connections, credentials, photo-folder paths, history and caches are excluded.</summary>
    BackupHelp,
    /// <summary>This private backup contains app names, launch paths and your custom tile source/assets. Keep it private. Saved data connections and credentials are excluded. Continue?</summary>
    BackupPrivacy,
    /// <summary>Backup created.</summary>
    BackupCreated,
    /// <summary>Backup restored. Reconfigure tile data connections and permissions as needed.</summary>
    BackupRestored,
    /// <summary>Restore {0} groups and {1} root tiles? This replaces your layout and appearance. Shell integration settings stay local. Data connections and web network permissions require configuration again.</summary>
    BackupConfirm,
    /// <summary>Backup operation failed: {0}</summary>
    BackupFailed,
    /// <summary>Pause live tiles outside the visible area</summary>
    PauseOffscreenLiveTiles,
    /// <summary>Automatically recover failed live tiles</summary>
    AutoRecoverLiveTiles,
    /// <summary>This tile was paused after repeated failures. Choose Reload to retry.</summary>
    TileRecoveryPaused,
    /// <summary>Opens the local data-source connection editor.</summary>
    ConfigureTileData,
    /// <summary>Requests an explicit data refresh.</summary>
    RefreshTileData,
    /// <summary>Explains host-managed requests and local-only credentials.</summary>
    TileDataHelp,
    /// <summary>Enables a chosen connection.</summary>
    TileDataEnable,
    /// <summary>Labels the connection type.</summary>
    TileDataAccess,
    /// <summary>Names a Web API or RSS connection.</summary>
    TileDataWeb,
    /// <summary>Names a local-file connection.</summary>
    TileDataFile,
    /// <summary>Labels the user-selected address or path.</summary>
    TileDataLocation,
    /// <summary>Chooses a local JSON or feed file.</summary>
    TileDataChooseFile,
    /// <summary>Labels an optional JSON Pointer response selector.</summary>
    TileDataSelector,
    /// <summary>Labels the credential's request header.</summary>
    TileDataHeader,
    /// <summary>Labels a password-protected credential input.</summary>
    TileDataSecret,
    /// <summary>Explains credential retention and destination changes.</summary>
    TileDataSecretHint,
    /// <summary>Removes a previously saved credential.</summary>
    TileDataClearSecret,
    /// <summary>Reports invalid or unavailable local connection settings.</summary>
    TileDataInvalid,
    /// <summary>Confirms local connection settings were saved.</summary>
    TileDataSaved,
    /// <summary>Explains the pending overlap that collects independent tiles into an ordinary group.</summary>
    HoldToCreateGroup,
    /// <summary>Identifies an armed independent-tile grouping destination.</summary>
    DropCreateGroup,
    /// <summary>Explains the pending tile-over-tile folder dwell before it is armed.</summary>
    HoldToCreateFolder,
    /// <summary>Select tiles</summary>
    EditLayout,
    /// <summary>Explains multi-selection and its shortcut while making direct dragging discoverable.</summary>
    SelectTilesHelp,
    /// <summary>Undo</summary>
    UndoLayout,
    /// <summary>Redo</summary>
    RedoLayout,
    /// <summary>Select all</summary>
    SelectAllTiles,
    /// <summary>Clear selection</summary>
    ClearTileSelection,
    /// <summary>Move to group…</summary>
    MoveSelectedTiles,
    /// <summary>Unpin selected</summary>
    UnpinSelectedTiles,
    /// <summary>Finish selecting</summary>
    FinishLayoutEditing,
    /// <summary>{0} selected</summary>
    SelectedTileCount,
    /// <summary>Click tiles to select; drag a selection to move it. Apps stay closed.</summary>
    LayoutEditingHint,
    /// <summary>Selected</summary>
    TileSelected,
    /// <summary>Select tile</summary>
    SelectTileForEditing,
    /// <summary>Open folder</summary>
    OpenTileFolder,
    /// <summary>The selection does not fit here. Drop it on open space or move it using the group menu.</summary>
    SelectionDoesNotFit,
    /// <summary>Move {0} tiles</summary>
    MoveTileCount,
    /// <summary>Adds a bundled or library live tile directly from a context menu.</summary>
    AddLiveTile,
    /// <summary>Names the user-owned package library.</summary>
    TileLibrary,
    /// <summary>Explains automatic package discovery.</summary>
    TileLibraryHelp,
    /// <summary>Opens the package library directory.</summary>
    OpenTileLibrary,
    /// <summary>Refreshes available packages.</summary>
    RefreshTiles,
    /// <summary>Explains an empty package library.</summary>
    TileLibraryEmpty,
    /// <summary>Chooses the explicit host folder binding for an image slideshow.</summary>
    ChooseImageFolder,
    /// <summary>Confirms the slideshow folder selection.</summary>
    SlideshowFolderSaved,
    /// <summary>Labels the installed tile gallery.</summary>
    BundledTiles,
    /// <summary>Names the gallery action for one sample.</summary>
    AddBundledTile,
    /// <summary>Explains the optional read-only system metrics API.</summary>
    SystemMetricsHelp,
    /// <summary>Opens ReStart when an identified Windows Start button is clicked.</summary>
    SettingsStartButton,
    /// <summary>Names the Explorer pin verb distinctly from the native Windows Start command.</summary>
    PinToReStart,
    /// <summary>Opens a filesystem file picker.</summary>
    PinFile,
    /// <summary>Opens a filesystem directory picker.</summary>
    PinFolder,
    /// <summary>Labels the file picker's unrestricted filter.</summary>
    AllFilesFilter,
    /// <summary>Enables the per-user Explorer pin command.</summary>
    ExplorerPinSetting,
    /// <summary>Explains the Explorer menu location and registration lifetime.</summary>
    ExplorerPinHelp,
    /// <summary>Reports an invalid pin path.</summary>
    ErrorPinPath,
    /// <summary>Reports a missing filesystem pin target.</summary>
    ErrorPinMissing,
    /// <summary>Reports a failed pin operation.</summary>
    ErrorPinFailed,
    /// <summary>Reports failure to reach the primary instance.</summary>
    ErrorPinConnection,
    /// <summary>Reports failure to register the per-user Explorer menu.</summary>
    ErrorExplorerRegistration,
    /// <summary>Names the destination group during tile drag/drop.</summary>
    DropIntoGroup,
    /// <summary>Identifies a folder drop destination.</summary>
    DropIntoFolder,
    /// <summary>Identifies an independent tile drop.</summary>
    DropIndependent,
    /// <summary>Identifies a group move preview.</summary>
    DropMoveGroup,
    /// <summary>The backup was restored.</summary>
    RecoveryBackupRestored,
    /// <summary>folder</summary>
    TileFolderSuffix,
    /// <summary>Ctrl+Alt+Space is in use. Open ReStart from the notification area.</summary>
    ErrorHotkeyInUse,
    /// <summary>Put this PC to sleep?</summary>
    ConfirmSleep,
    /// <summary>ReStart — Open with Ctrl+Alt+Space</summary>
    TrayTooltip,
    /// <summary>ReStart encountered an error and will close. Your saved layout is preserved.</summary>
    ErrorUnhandled,
    /// <summary>ReStart settings</summary>
    SettingsTitle,
    /// <summary>Exit ReStart</summary>
    AppExit,
    /// <summary>Open ReStart</summary>
    AppOpen,
    /// <summary>Website URL</summary>
    PromptWebsiteUrl,
    /// <summary>Pin a website</summary>
    PinWebsite,
    /// <summary>Windows shortcuts such as Win+E and Win+D remain available. You can also use Ctrl+Alt+Space or the notification area.</summary>
    SettingsShortcutHelp,
    /// <summary>Windows settings</summary>
    OpenWindowsSettings,
    /// <summary>Start when I sign in to Windows</summary>
    SettingsAutostart,
    /// <summary>Could not capture the Windows key:</summary>
    ErrorWindowsKeyCapture,
    /// <summary>Open ReStart with a single Windows key press</summary>
    SettingsWindowsKey,
    /// <summary>Enter an http or https URL.</summary>
    ErrorInvalidUrl,
    /// <summary>Other</summary>
    /// <summary>Everyday apps</summary>
    SeedEveryday,
    /// <summary>All apps</summary>
    NavAllApps,
    /// <summary>Account</summary>
    NavAccount,
    /// <summary>Change account settings</summary>
    AccountSettings,
    /// <summary>Accent</summary>
    AccentPrefix,
    /// <summary>Accent color</summary>
    SettingsAccent,
    /// <summary>App</summary>
    AppFallback,
    /// <summary>Could not load apps:</summary>
    ErrorCatalogLoad,
    /// <summary>Pin an app</summary>
    PinApp,
    /// <summary>Search</summary>
    SearchPlaceholder,
    /// <summary>Loading apps…</summary>
    StatusLoading,
    /// <summary>Import</summary>
    CommonImport,
    /// <summary>File Explorer</summary>
    AppExplorer,
    /// <summary>Export</summary>
    CommonExport,
    /// <summary>Exported. The file includes launch targets and local paths.</summary>
    StatusExported,
    /// <summary>Calendar</summary>
    AppCalendar,
    /// <summary>Calendar tile</summary>
    AddCalendar,
    /// <summary>Cancel</summary>
    CommonCancel,
    /// <summary>Localized confirmation dialog title.</summary>
    CommonConfirm,
    /// <summary>Affirmative confirmation action.</summary>
    CommonYes,
    /// <summary>Negative confirmation action and safe default.</summary>
    CommonNo,
    /// <summary>Clock</summary>
    AppClock,
    /// <summary>Group:</summary>
    GroupPrefix,
    /// <summary>Group name</summary>
    PromptGroupName,
    /// <summary>Name group</summary>
    NameGroup,
    /// <summary>Name folder</summary>
    NameFolder,
    /// <summary>Group width</summary>
    SettingsGroupWidth,
    /// <summary>Move to group</summary>
    MoveToGroup,
    /// <summary>Delete group</summary>
    DeleteGroup,
    /// <summary>Move group earlier</summary>
    MoveGroupEarlier,
    /// <summary>Move group later</summary>
    MoveGroupLater,
    /// <summary>Rename group</summary>
    RenameGroup,
    /// <summary>Resize</summary>
    ResizeTile,
    /// <summary>Sign out</summary>
    AccountSignOut,
    /// <summary>Follow system</summary>
    ThemeSystem,
    /// <summary>Shut down</summary>
    PowerShutdown,
    /// <summary>Start</summary>
    NavStart,
    /// <summary>Unpin from Start</summary>
    UnpinTile,
    /// <summary>Sleep</summary>
    PowerSleep,
    /// <summary>Tile name</summary>
    PromptTileName,
    /// <summary>Tile size</summary>
    SettingsTileSize,
    /// <summary>Add tile</summary>
    AddTile,
    /// <summary>Too many tiles.</summary>
    ErrorTooManyTiles,
    /// <summary>Dark</summary>
    ThemeDark,
    /// <summary>Tools</summary>
    SeedTools,
    /// <summary>Theme</summary>
    SettingsTheme,
    /// <summary>Use the desktop wallpaper as the background</summary>
    SettingsWallpaper,
    /// <summary>Documents</summary>
    NavDocuments,
    /// <summary>Pictures</summary>
    NavPictures,
    /// <summary>Choose a file to pin</summary>
    PromptPinFile,
    /// <summary>Choose a folder to pin</summary>
    PromptPinFolder,
    /// <summary>Pinned tiles</summary>
    NavPinned,
    /// <summary>Choose file</summary>
    ChooseFile,
    /// <summary>Pin a file or folder</summary>
    PinFileFolder,
    /// <summary>Photos</summary>
    AppPhotos,
    /// <summary>Folder</summary>
    TileFolder,
    /// <summary>Create or add to folder</summary>
    CreateFolder,
    /// <summary>Nested folders are not supported.</summary>
    ErrorNestedFolder,
    /// <summary>Unpack folder</summary>
    UnpackFolder,
    /// <summary>Choose folder</summary>
    ChooseFolder,
    /// <summary>Close folder</summary>
    CloseFolder,
    /// <summary>Paint</summary>
    AppPaint,
    /// <summary>Expand menu</summary>
    ExpandMenu,
    /// <summary>Notepad</summary>
    AppNotepad,
    /// <summary>Mail</summary>
    AppMail,
    /// <summary>Light</summary>
    ThemeLight,
    /// <summary>Turn live tile off</summary>
    DisableLiveTile,
    /// <summary>Turn live tile on</summary>
    EnableLiveTile,
    /// <summary>The layout exceeds 5 MB.</summary>
    ErrorLayoutTooLarge,
    /// <summary>The layout is empty.</summary>
    ErrorLayoutEmpty,
    /// <summary>Layout and data</summary>
    SettingsLayout,
    /// <summary>Import layout</summary>
    ImportLayout,
    /// <summary>Layout imported.</summary>
    StatusImported,
    /// <summary>Export layout</summary>
    ExportLayout,
    /// <summary>Lock</summary>
    AccountLock,
    /// <summary>No matching apps</summary>
    SearchNoMatches,
    /// <summary>Positions, sizes and folders are saved automatically. Hold Shift and drop a tile onto another to create a folder.</summary>
    SettingsLayoutHelp,
    /// <summary>Medium</summary>
    TileMedium,
    /// <summary>3 medium tiles</summary>
    ColumnsThree,
    /// <summary>4 medium tiles</summary>
    ColumnsFour,
    /// <summary>Save</summary>
    CommonSave,
    /// <summary>The language changes when you save. App names and your custom labels are preserved.</summary>
    SettingsLanguageHelp,
    /// <summary>Could not save:</summary>
    ErrorSave,
    /// <summary>Storage:</summary>
    StoragePrefix,
    /// <summary>Restart</summary>
    PowerRestart,
    /// <summary>Default layout restored.</summary>
    StatusLayoutReset,
    /// <summary>Restore default layout</summary>
    ResetLayout,
    /// <summary>Close when switching to another app</summary>
    SettingsCloseOnDeactivate,
    /// <summary>Search with part of an app name.</summary>
    SearchHint,
    /// <summary>Rename</summary>
    RenameTile,
    /// <summary>Appearance</summary>
    SettingsAppearance,
    /// <summary>Large</summary>
    TileLarge,
    /// <summary>Small</summary>
    TileSmall,
    /// <summary>About</summary>
    SettingsAbout,
    /// <summary>New group</summary>
    NewGroup,
    /// <summary>Pin to a new group</summary>
    PinNewGroup,
    /// <summary>This layout uses a newer format and is protected from changes.</summary>
    ErrorFutureLayout,
    /// <summary>Native name of Japanese in the language selector.</summary>
    LanguageJapanese,
    /// <summary>Use classic colored tiles</summary>
    SettingsColoredTiles,
    /// <summary>Use app-provided tile images</summary>
    SettingsAppTileImages,
    /// <summary>Explains installed tile artwork and ordinary icon fallback.</summary>
    AppTileImagesHelp,
    /// <summary>Clock tile</summary>
    AddClock,
    /// <summary>Best match</summary>
    SearchBestMatch,
    /// <summary>Unsupported</summary>
    ErrorUnsupported,
    /// <summary>Search</summary>
    SearchTitle,
    /// <summary>Clear search</summary>
    SearchClear,
    /// <summary>Wide</summary>
    TileWide,
    /// <summary>Restore the default pinned layout?</summary>
    ConfirmReset,
    /// <summary>Replace your current pins? Imported tiles include launch targets. Only use files you trust.</summary>
    ConfirmImport,
    /// <summary>Exit</summary>
    CommonExit,
    /// <summary>Background opacity</summary>
    SettingsOpacity,
    /// <summary>Animate when opening</summary>
    SettingsAnimation,
    /// <summary>Display language</summary>
    SettingsDisplayLanguage,
    /// <summary>Language</summary>
    SettingsLanguage,
    /// <summary>Settings</summary>
    NavSettings,
    /// <summary>Could not read:</summary>
    ErrorRead,
    /// <summary>Could not launch:</summary>
    ErrorLaunch,
    /// <summary>Startup and controls</summary>
    SettingsStartup,
    /// <summary>No launch target is set.</summary>
    ErrorNoTarget,
    /// <summary>No ads or usage reporting. Installed apps and shortcuts are read locally on this PC.</summary>
    SettingsPrivacy,
    /// <summary>Close</summary>
    CommonClose,
    /// <summary>Calculator</summary>
    AppCalculator,
    /// <summary>Power</summary>
    NavPower,
    /// <summary>+ Group</summary>
    AddGroup,
    /// <summary>+ Create a group and pin apps</summary>
    EmptyGroups,
    /// <summary>Choose a letter    × Close</summary>
    AlphabetTitle,
    /// <summary>{0} apps  ·  Type to search  ·  Esc to close</summary>
    StatusCatalogReady,
    /// <summary>Delete “{0}” and its pins? The apps themselves will remain installed.</summary>
    ConfirmDeleteGroup,
    /// <summary>Pin to “{0}”</summary>
    PinToGroup,
    /// <summary>Pinned {0}</summary>
    StatusPinned,
    /// <summary>{0} this PC? Save your work first.</summary>
    ConfirmPower,
    /// <summary>Could not read the layout. The original file is preserved. {0}</summary>
    ErrorLayoutRead,
    /// <summary>Unsupported layout version: {0}</summary>
    ErrorLayoutVersion,
    /// <summary>Add groups or tiles</summary>
    AddMenuTooltip,
    /// <summary>☰     Start</summary>
    RailStart,
    /// <summary>▦     Pinned tiles</summary>
    RailPinned,
    /// <summary>☷     All apps</summary>
    RailAllApps,
    /// <summary>▤     Documents</summary>
    RailDocuments,
    /// <summary>▧     Pictures</summary>
    RailPictures,
    /// <summary>⚙     ReStart settings</summary>
    RailSettings,
    /// <summary>⏻     Power</summary>
    RailPower,
    /// <summary>ReStart — Preview</summary>
    PreviewTitle,
    /// <summary>Use the portable ReStart.exe to register startup.</summary>
    ErrorStartupExecutable,
    /// <summary>The startup path is too long. Move ReStart to a shorter path.</summary>
    ErrorStartupPathLength,
    /// <summary>Preview mode does not change Windows startup registration.</summary>
    PreviewAutostartHelp,
    /// <summary>Starts in the notification area when you sign in. Use the shortcut to open Start.</summary>
    StartupBackgroundHelp,
    /// <summary>Registered startup command:</summary>
    StartupCommandPrefix,
    /// <summary>Could not restore startup registration:</summary>
    ErrorStartupRollback,
    /// <summary>JavaScript tiles</summary>
    SettingsScriptTiles,
    /// <summary>Create a JavaScript tile</summary>
    CreateScriptTile,
    /// <summary>Import a JavaScript tile</summary>
    ImportScriptTile,
    /// <summary>Select the JavaScript tile manifest.json</summary>
    PromptScriptManifest,
    /// <summary>Edit JavaScript</summary>
    EditScript,
    /// <summary>Edit tile settings</summary>
    EditScriptSettings,
    /// <summary>Open tile folder</summary>
    OpenScriptFolder,
    /// <summary>Reload tile</summary>
    ReloadScript,
    /// <summary>Tile error:</summary>
    ErrorScriptPrefix,
    /// <summary>Tile error</summary>
    ErrorScript,
    /// <summary>Up to eight JavaScript tiles can update at once.</summary>
    /// <summary>Create sample</summary>
    CreateScriptSample,
    /// <summary>Tile added. Close settings to see it.</summary>
    StatusScriptAdded,
    /// <summary>Build your own live tiles with JavaScript. Create a sample and edit tile.js in Notepad. Saved changes appear on the next refresh.</summary>
    ScriptAuthoringHelp,
    /// <summary>Scripts receive the time, language, settings and previous state. Network, file, Node.js, DOM and CLR APIs are not exposed. See the tile development guide linked in the README.</summary>
    ScriptCapabilitiesHelp,
    /// <summary>Invalid tile manifest (version 1, title 1–80, refreshSeconds 1–3600, settings JSON ≤8192 characters).</summary>
    ErrorScriptManifestInvalid,
    /// <summary>Invalid tile frame. Check text lengths, #RRGGBB colors, progress 0–1 and state JSON ≤8192 characters.</summary>
    ErrorScriptFrameInvalid,
    /// <summary>Tile protocol size limit exceeded.</summary>
    ErrorScriptProtocolSize,
    /// <summary>Invalid installed tile id.</summary>
    ErrorScriptPackageId,
    /// <summary>Tile package is too large.</summary>
    ErrorScriptPackageSize,
    /// <summary>Missing manifest.</summary>
    ErrorScriptManifestMissing,
    /// <summary>Tile input is too large.</summary>
    ErrorScriptInputSize,
    /// <summary>Tile output exceeds 16 KB.</summary>
    ErrorScriptOutputSize,
    /// <summary>render(ctx) must return an object.</summary>
    ErrorScriptReturnObject,
    /// <summary>Could not start tile worker.</summary>
    ErrorScriptWorkerStart,
    /// <summary>Tile request is too large.</summary>
    ErrorScriptRequestSize,
    /// <summary>Tile worker exited.</summary>
    ErrorScriptWorkerExit,
    /// <summary>Missing tile response.</summary>
    ErrorScriptResponseMissing,
    /// <summary>Missing tile frame.</summary>
    ErrorScriptFrameMissing,
    /// <summary>Tile worker exceeded its deadline.</summary>
    ErrorScriptDeadline,
    /// <summary>Missing request.</summary>
    ErrorScriptRequestMissing,
    /// <summary>Pin to Start</summary>
    PinToStart,
    /// <summary>More</summary>
    MenuMore,
    /// <summary>Run as administrator</summary>
    RunAsAdministrator,
    /// <summary>Open file location</summary>
    OpenFileLocation,
    /// <summary>Open</summary>
    MenuOpen,
    /// <summary>Open folder location</summary>
    OpenFolderLocation,
    /// <summary>Run as different user</summary>
    RunAsDifferentUser,
    /// <summary>Pin to taskbar</summary>
    PinToTaskbar,
    /// <summary>Unpin from taskbar</summary>
    UnpinFromTaskbar,
    /// <summary>App settings</summary>
    AppSettings,
    /// <summary>Installed apps…</summary>
    ManageInstalledApps,
    /// <summary>Share</summary>
    ShareItem,
    /// <summary>Rate and review</summary>
    RateAndReview,
    /// <summary>Copy full path</summary>
    CopyFullPath,
    /// <summary>Uninstall</summary>
    UninstallApp,
    /// <summary>Properties</summary>
    AppProperties,
    /// <summary>Loading…</summary>
    MenuLoading,
    /// <summary>Pin to group</summary>
    PinToGroupMenu,
    /// <summary>Unnamed group</summary>
    UnnamedGroup,
    /// <summary>Unpinned {0}</summary>
    StatusUnpinned,
    /// <summary>This command is no longer available for this app.</summary>
    ErrorAppCommandUnavailable,
    /// <summary>Could not run the app command: </summary>
    ErrorAppCommand,
    /// <summary>Updates</summary>
    SettingsUpdates,
    /// <summary>Version: {0}</summary>
    AboutVersion,
    /// <summary>Build: {0}</summary>
    AboutBuild,
    /// <summary>Runtime: {0} · {1}</summary>
    AboutRuntime,
    /// <summary>Settings schema: {0}</summary>
    AboutSchema,
    /// <summary>Last saved by: {0}</summary>
    AboutSavedVersion,
    /// <summary>Licenses and notices</summary>
    AboutLicenses,
    /// <summary>ReStart licenses</summary>
    LicenseTitle,
    /// <summary>Settings migration</summary>
    SettingsMigration,
    /// <summary>No recorded upgrade. New settings use the current format.</summary>
    MigrationNone,
    /// <summary>{0} → {1} · schema {2} → {3} · {4}</summary>
    MigrationLast,
    /// <summary>Rollback backup: {0}</summary>
    MigrationBackup,
    /// <summary>Legacy / unknown</summary>
    MigrationLegacy,
    /// <summary>These settings were created by a newer version and are read-only.</summary>
    MigrationReadOnly,
    /// <summary>To roll back, exit ReStart, keep a copy of the current layout, and restore the matching backup as layout.json. An older app cannot edit a newer schema.</summary>
    MigrationRollbackHelp,
    /// <summary>Open settings folder</summary>
    OpenDataFolder,
    /// <summary>Check for updates</summary>
    UpdateCheckNow,
    /// <summary>Checking GitHub releases…</summary>
    UpdateChecking,
    /// <summary>ReStart {0} is available.</summary>
    UpdateAvailable,
    /// <summary>This version is up to date for the selected channel.</summary>
    UpdateUpToDate,
    /// <summary>No complete release is available for this architecture and channel.</summary>
    UpdateNoReleases,
    /// <summary>The public release service is unavailable. Try again later or open Releases.</summary>
    UpdateServiceUnavailable,
    /// <summary>GitHub's request limit was reached. Try again later.</summary>
    UpdateRateLimited,
    /// <summary>The update service could not be reached. Check the connection and try again.</summary>
    UpdateNetworkError,
    /// <summary>The release response could not be verified. Open Releases to check manually.</summary>
    UpdateInvalidResponse,
    /// <summary>Last checked: {0}</summary>
    UpdateLastChecked,
    /// <summary>Check for updates at startup</summary>
    UpdateOnStartup,
    /// <summary>Include prereleases</summary>
    UpdateIncludePrereleases,
    /// <summary>Open Releases</summary>
    UpdateOpenRelease,
    /// <summary>Release notes</summary>
    UpdateReleaseNotes,
    /// <summary>Updates are checked only. Download and extract the release manually; the app does not replace itself.</summary>
    UpdateNoAutoInstall,
    /// <summary>Update checks contact api.github.com with the app version. No app list, layout or usage history is sent.</summary>
    UpdatePrivacy,
    /// <summary>Network checks are disabled in preview mode.</summary>
    UpdatesDisabledInPreview,
    /// <summary>Select a license or notice to read and copy.</summary>
    LicenseChoose,
    /// <summary>Show accent color on Start</summary>
    SettingsStartAccent,
    /// <summary>Applies to the dark Start surface. Classic tiles share the accent unless explicitly customized.</summary>
    SettingsStartAccentHelp,
    /// <summary>Tile background color</summary>
    TileBackgroundColor,
    /// <summary>Labels the active theme's background as a tile color.</summary>
    TileThemeColor,
    /// <summary>Labels the ordinary app, widget, or shared tile appearance.</summary>
    TileAutomaticColor,
    /// <summary>Labels the active theme's accent as a tile background.</summary>
    TileThemeAccent,
    /// <summary>Custom color…</summary>
    TileCustomColor,
    /// <summary>Background color (#RRGGBB)</summary>
    PromptTileColor,
    /// <summary>Enter a color as #RRGGBB, for example #0078D7.</summary>
    ErrorInvalidColor,
    /// <summary>Getting ready</summary>
    StartupTitle,
    /// <summary>Finding your apps…</summary>
    StartupApps,
    /// <summary>Preparing icons…</summary>
    StartupIcons,
    /// <summary>Preparing live tiles…</summary>
    StartupTiles,
    /// <summary>Ready</summary>
    StartupReady,
    /// <summary>Color themes</summary>
    SettingsColorThemes,
    /// <summary>Base theme</summary>
    SettingsBaseTheme,
    /// <summary>Labels the Start presentation selector.</summary>
    SettingsStartMenuMode,
    /// <summary>Labels the option to show windows behind the Compact Start background.</summary>
    SettingsTransparentCompactBackground,
    /// <summary>Labels the opacity slider for the theme tint over the Compact backdrop.</summary>
    SettingsCompactTint,
    /// <summary>Names the original full-screen Start presentation.</summary>
    StartMenuFullscreen,
    /// <summary>Names the resizable Compact Start presentation.</summary>
    StartMenuCompact,
    /// <summary>Background color</summary>
    SettingsBackgroundColor,
    /// <summary>Use #RRGGBB. An empty background uses the base theme. Text and controls adjust for readable contrast.</summary>
    SettingsCustomColorsHelp,
    /// <summary>Reset colors</summary>
    SettingsResetColors,
    /// <summary>Midnight</summary>
    ThemeMidnight,
    /// <summary>Ocean</summary>
    ThemeOcean,
    /// <summary>Forest</summary>
    ThemeForest,
    /// <summary>Rose</summary>
    ThemeRose,
    /// <summary>Sand</summary>
    ThemeSand,
    /// <summary>Apps</summary>
    SearchApps,
    /// <summary>Settings</summary>
    SearchSettings,
    /// <summary>Files</summary>
    SearchFiles,
    /// <summary>Search indexed files</summary>
    SearchFilesSetting,
    /// <summary>File search is unavailable. Check Windows indexing settings.</summary>
    SearchIndexUnavailable,
    /// <summary>Recent</summary>
    JumpRecent,
    /// <summary>Frequent</summary>
    JumpFrequent,
    /// <summary>Recently added</summary>
    RecentlyAdded,
    /// <summary>Most used</summary>
    MostUsed,
    /// <summary>Show recently added apps</summary>
    SettingsShowRecentApps,
    /// <summary>Show most used apps (record launches in ReStart)</summary>
    SettingsShowMostUsed,
    /// <summary>Include existing Windows app usage (read-only)</summary>
    SettingsWindowsAppHistory,
    /// <summary>Use Windows advanced file queries</summary>
    SettingsWindowsSearchSyntax,
    /// <summary>Use Windows query syntax and relevance ranking. Canonical properties such as System.Kind:picture work across languages. Document contents still require the separate content option.</summary>
    WindowsSearchSyntaxHelp,
    /// <summary>Search Windows and cloud</summary>
    SearchInWindows,
    /// <summary>Search the web for “{0}”</summary>
    SearchWebFor,
    /// <summary>Show web search in the default browser</summary>
    SettingsWebSearch,
    /// <summary>Opens Bing in your default browser only when you choose the action. Typing does not send search text to the Web.</summary>
    WebSearchHelp,
    /// <summary>Rank equal app matches using enabled app history</summary>
    SettingsPersonalizeAppSearch,
    /// <summary>Requires Most used app history below. Exact names and executable aliases keep priority. Windows Start's private ranking is not reproduced.</summary>
    AppSearchRankingHelp,
    /// <summary>Show more</summary>
    RecentAppsShowMore,
    /// <summary>Show less</summary>
    RecentAppsShowLess,
    /// <summary>Show Windows Search handoff</summary>
    SettingsWindowsSearchHandoff,
    /// <summary>Opens Windows Search and transfers the current query when its focused input is available. Cloud results depend on Windows permissions and connected accounts.</summary>
    WindowsSearchHandoffHelp,
    /// <summary>Windows Search opened but did not accept the query. Enter it in Windows Search.</summary>
    WindowsSearchQueryUnavailable,
    /// <summary>Show Windows app live tiles (compatibility)</summary>
    SettingsLegacyUwpTiles,
    /// <summary>Reads active tile and badge data only for pinned packaged apps. Uses an undocumented Windows store; unavailable data falls back to icons. Toasts are not read and remote images are not downloaded.</summary>
    LegacyUwpTilesHelp,
    /// <summary>Windows usage is read only when Windows app tracking is enabled. Nothing is imported to disk. Clearing ReStart history does not clear Windows history.</summary>
    WindowsAppHistoryHelp,
    /// <summary>Clear app history</summary>
    ClearAppHistory,
    /// <summary>History stays on this PC. Usage counts are recorded only when enabled. Existing apps are not marked as newly added on the first run.</summary>
    AppHistoryHelp,
    /// <summary>App history cleared</summary>
    HistoryCleared,
    /// <summary>Navigation shortcuts</summary>
    SettingsSidebar,
    /// <summary>Downloads</summary>
    NavDownloads,
    /// <summary>Music</summary>
    NavMusic,
    /// <summary>Videos</summary>
    NavVideos,
    /// <summary>Network</summary>
    NavNetwork,
    /// <summary>Personal folder</summary>
    NavPersonalFolder,
    /// <summary>Display</summary>
    WindowsDisplay,
    /// <summary>Sound</summary>
    WindowsSound,
    /// <summary>Network &amp; Internet</summary>
    WindowsNetwork,
    /// <summary>Bluetooth &amp; devices</summary>
    WindowsBluetooth,
    /// <summary>Installed apps</summary>
    WindowsApps,
    /// <summary>Storage</summary>
    WindowsStorage,
    /// <summary>Windows Update</summary>
    WindowsUpdates,
    /// <summary>Privacy</summary>
    WindowsPrivacy,
    /// <summary>Personalization</summary>
    WindowsPersonalization,
    /// <summary>Background wallpaper</summary>
    WindowsBackground,
    /// <summary>Colors</summary>
    WindowsColors,
    /// <summary>Date &amp; time</summary>
    WindowsDateTime,
    /// <summary>Language &amp; region</summary>
    WindowsLanguage,
    /// <summary>Accessibility</summary>
    WindowsAccessibility,
    /// <summary>Power &amp; battery</summary>
    WindowsPower,
    /// <summary>Start behavior</summary>
    SettingsStartExperience,
    /// <summary>Search Windows settings</summary>
    SettingsSearchSettings,
    /// <summary>Show recent and frequent documents in app menus</summary>
    SettingsJumpLists,
    /// <summary>Expand navigation on hover</summary>
    SettingsRailHover,
    /// <summary>Highlight tiles on hover</summary>
    SettingsTileHover,
    /// <summary>Scroll automatically near board edges</summary>
    SettingsDragScroll,
    /// <summary>Show drop hints and hover progress</summary>
    SettingsDropHints,
    /// <summary>Create groups by overlapping independent tiles</summary>
    SettingsDropGroups,
    /// <summary>Use free tile placement</summary>
    SettingsFreePlacement,
    /// <summary>Normally, dropping into empty space creates an unnamed group. Hold Alt while dragging for independent placement. Existing layouts are kept.</summary>
    SettingsFreePlacementHelp,
    /// <summary>Create a new group</summary>
    DropNewGroup,
    /// <summary>Create folders by overlapping grouped tiles</summary>
    SettingsDropFolders,
    /// <summary>Show group boundaries on hover</summary>
    SettingsGroupHover,
    /// <summary>Searching indexed files…</summary>
    SearchIndexSearching,
    /// <summary>Launch options…</summary>
    LaunchOptions,
    /// <summary>Use custom launch options</summary>
    UseLaunchOptions,
    /// <summary>Arguments</summary>
    LaunchArguments,
    /// <summary>Start in</summary>
    WorkingDirectory,
    /// <summary>Arguments are passed to the app or shortcut. Quote paths containing spaces. Empty fields keep the Shell defaults. Windows handles administrator consent.</summary>
    LaunchOptionsHelp,
    /// <summary>Custom launch options are available for desktop applications and application shortcuts supported by Windows.</summary>
    LaunchOptionsUnavailable,
    /// <summary>Choose an existing working directory.</summary>
    ErrorWorkingDirectory,
    /// <summary>Background image</summary>
    BackgroundSource,
    /// <summary>None (use theme color)</summary>
    BackgroundNone,
    /// <summary>Desktop wallpaper</summary>
    BackgroundDesktop,
    /// <summary>ReStart image</summary>
    BackgroundCustom,
    /// <summary>Choose image…</summary>
    BackgroundChoose,
    /// <summary>Your chosen image is copied into local ReStart storage. Desktop changes do not affect it. High contrast hides background images.</summary>
    BackgroundHelp,
    /// <summary>Image copied. Save to apply.</summary>
    BackgroundSelected,
    /// <summary>Choose a readable image file of at most 32 MiB.</summary>
    BackgroundInvalid,
    /// <summary>Local app data was deleted while ReStart was running. Restart ReStart to start fresh; the deleted layout will not be recreated from memory.</summary>
    ErrorDataDeleted,
    /// <summary>Tile source files are missing. Reinstall the package or replace its source in Tile Studio.</summary>
    ErrorScriptSourceMissing,
    /// <summary>Lock layout</summary>
    LockLayout,
    /// <summary>Lock this group</summary>
    LockGroup,
    /// <summary>Locks protect positions, sizes and membership. Apps, names and colors still work. Press Ctrl+Shift+L to toggle the layout lock.</summary>
    LayoutLockHelp,
    /// <summary>Layout is locked. Unlock it to change positions, sizes or contents.</summary>
    LayoutLockedStatus,
    /// <summary>Layout unlocked.</summary>
    LayoutUnlockedStatus,
    /// <summary>Arrows: focus · Space: select · Ctrl+arrows: move tiles · Ctrl+Shift+arrows: move group · Alt+1–4: size · Ctrl+G: new group · Shift+F10: tile menu · Ctrl+Z/Y: undo/redo</summary>
    KeyboardLayoutHelp,
    /// <summary>Tile Studio</summary>
    StudioTitle,
    /// <summary>Edit JavaScript and the manifest, then preview all four sizes. Previews use offline sample metrics and data. Save drafts to your tile library or save and pin a working tile.</summary>
    StudioHelp,
    /// <summary>Template</summary>
    StudioTemplate,
    /// <summary>Load template</summary>
    StudioLoadTemplate,
    /// <summary>Preview all sizes</summary>
    StudioPreview,
    /// <summary>Preview while typing</summary>
    StudioAutoPreview,
    /// <summary>Reset preview state</summary>
    StudioResetState,
    /// <summary>Save to tile library</summary>
    StudioSaveLibrary,
    /// <summary>Save and pin</summary>
    StudioSaveAndPin,
    /// <summary>Sample data (JSON)</summary>
    StudioSamples,
    /// <summary>Last preview input</summary>
    StudioInput,
    /// <summary>Editor document</summary>
    StudioEditor,
    /// <summary>Click a preview to test its click event. Sample data stays in this session. Ctrl+Enter previews; Ctrl+S saves.</summary>
    StudioClickHint,
    /// <summary>All four sizes rendered successfully.</summary>
    StudioPreviewSuccess,
    /// <summary>Saved. Library drafts and installed tiles are separate copies.</summary>
    StudioSaved,
    /// <summary>Discard unsaved changes to this tile?</summary>
    StudioDiscard,
    /// <summary>The original source is missing. A clock template is shown for repair. Saving replaces the missing source while keeping this tile’s position.</summary>
    StudioMissing,
    /// <summary>Allow this tile to access the network</summary>
    AllowWebTileNetwork,
    /// <summary>The web tile process stopped. Reload this tile to try again.</summary>
    ErrorWebTileProcess,
    /// <summary>The HTML tile could not load. Check its HTML, CSS and JavaScript in Tile Studio.</summary>
    ErrorWebTileLoad,
    /// <summary>HTML tiles require Microsoft Edge WebView2 Runtime. Install it from Microsoft, then restart ReStart.</summary>
    ErrorWebTileRuntime,
    /// <summary>Show live tile names</summary>
    ShowLiveTileNames,
    /// <summary>An update is available</summary>
    UpdateNoticeTitle,
    /// <summary>ReStart {0} is available. You’re using {1}.</summary>
    UpdateNoticeBody,
    /// <summary>Not now</summary>
    UpdateNoticeLater,
    /// <summary>Review what's new in ReStart, then choose whether to download the update.</summary>
    UpdateNoticeHelp,
    /// <summary>Review update</summary>
    UpdateReview,
    /// <summary>Format document</summary>
    StudioFormat,
    /// <summary>Formatting…</summary>
    StudioFormatting,
    /// <summary>Document formatted. Ctrl+Z undoes the change.</summary>
    StudioFormatted,
    /// <summary>The document changed while formatting. Try again.</summary>
    StudioFormatChanged,
    /// <summary>Unable to format this document: </summary>
    StudioFormatFailed,
    /// <summary>Support development</summary>
    AboutSupport,
    /// <summary>Diagnostic information</summary>
    DiagnosticsTitle,
    /// <summary>Copy report</summary>
    DiagnosticsCopy,
    /// <summary>Review the complete report below before sharing it. It contains versions, display mode and error categories only. No logs, names, paths, app lists, tile data or credentials are included. Nothing is uploaded.</summary>
    DiagnosticsHelp,
    /// <summary>Copied.</summary>
    DiagnosticsCopied,
    /// <summary>The clipboard is busy. Try again or select and copy the text.</summary>
    DiagnosticsClipboardBusy,
    /// <summary>Download update</summary>
    UpdateDownload,
    /// <summary>Restart and update</summary>
    UpdateRestart,
    /// <summary>Restart ReStart to install the verified update? Your layout and saved settings are kept. Unsaved changes in this settings window will not be applied.</summary>
    UpdateConfirmRestart,
    /// <summary>Guided updates require a complete portable package with its distribution manifest in a writable folder. Developer builds and older packages can be updated manually from GitHub.</summary>
    UpdatePortableRequired,
    /// <summary>Downloading and verifying… {0:N1} MiB</summary>
    UpdateDownloading,
    /// <summary>ReStart {0} is verified and ready. Restart when convenient.</summary>
    UpdatePrepared,
    /// <summary>The update could not be prepared. Your installation was not changed. Check the connection, free space and folder permissions, or use the GitHub download.</summary>
    UpdatePrepareFailed,
    /// <summary>Download cancelled. Your installation was not changed.</summary>
    UpdateCancelled,
    /// <summary>ReStart was updated successfully.</summary>
    UpdateInstalled,
    /// <summary>The update could not be recovered automatically. Exit ReStart and extract the complete portable ZIP into the installation folder. Keep your local ReStart settings. Update backups remain in ReStart.UpdateCache under local app data.</summary>
    UpdateRepairHelp,
    /// <summary>The update did not finish. ReStart is running the previous installation.</summary>
    UpdateRestored,
    /// <summary>Wallpaper blur</summary>
    SettingsBlur,
    /// <summary>0 keeps the image sharp. Blur affects only ReStart’s background image.</summary>
    SettingsBlurHelp,
    /// <summary>Explains the immediate appearance, geometry and language preview.</summary>
    SettingsPreviewHelp,
    /// <summary>Titles the unsaved-settings decision dialog.</summary>
    SettingsUnsavedTitle,
    /// <summary>Explains save, discard and restoration of visual draft settings.</summary>
    SettingsUnsavedBody,
    /// <summary>Keeps the settings editor open with its draft intact.</summary>
    SettingsKeepEditing,
    /// <summary>Discards draft settings and closes the editor.</summary>
    SettingsDiscardClose,
    /// <summary>Saves settings before closing the editor.</summary>
    SettingsSaveClose,
    /// <summary>Titles a reminder for an explicitly downloaded update.</summary>
    UpdateReadyTitle,
    /// <summary>Identifies the prepared release version in placeholder zero.</summary>
    UpdateReadyBody,
    /// <summary>Explains explicit restart consent and the persistent reminder.</summary>
    UpdateReadyHelp,
    /// <summary>Show shortcut tips while dragging.</summary>
    SettingsDragShortcutHints,
    /// <summary>Place independently in empty space.</summary>
    DragHintIndependent,
    /// <summary>Rearrange without creating or entering folders.</summary>
    DragHintReorder,
    /// <summary>Keyboard shortcuts.</summary>
    KeyboardShortcuts,
    /// <summary>Start and navigation.</summary>
    ShortcutsGeneral,
    /// <summary>While dragging tiles.</summary>
    ShortcutsDragging,
    /// <summary>While selecting tiles (Ctrl+E).</summary>
    ShortcutsSelection,
    /// <summary>Open or close ReStart (Win key must be enabled).</summary>
    ShortcutOpen,
    /// <summary>Close the current view, then Start.</summary>
    ShortcutEscape,
    /// <summary>Focus a tile.</summary>
    ShortcutFocus,
    /// <summary>Toggle the focused tile’s selection.</summary>
    ShortcutSelect,
    /// <summary>Move tiles by one small cell.</summary>
    ShortcutMoveTiles,
    /// <summary>Move the focused tile’s group.</summary>
    ShortcutMoveGroup,
    /// <summary>Resize: small / medium / wide / large.</summary>
    ShortcutSize,
    /// <summary>Create a group from the selection.</summary>
    ShortcutNewGroup,
    /// <summary>Open the focused tile’s menu.</summary>
    ShortcutTileMenu,
    /// <summary>Clear selection, then finish selecting.</summary>
    ShortcutClearSelection,
    /// <summary>Remove from this list.</summary>
    RemoveFromThisList,
    /// <summary>Don't show in this list.</summary>
    DontShowInThisList,
    /// <summary>Clear list (Recently added only).</summary>
    ClearRecentList,
    /// <summary>Personalize this list (navigation shortcuts).</summary>
    PersonalizeThisList,
    /// <summary>Unpin group from Start.</summary>
    UnpinGroupFromStart,
    /// <summary>Tasks registered by the installed application.</summary>
    JumpTasks,
    /// <summary>Destinations pinned in the application's Windows jump list.</summary>
    JumpPinned,
    /// <summary>Pin a destination to this Windows jump list.</summary>
    PinToJumpList,
    /// <summary>Unpin a destination from this Windows jump list.</summary>
    UnpinFromJumpList,
    /// <summary>Windows accepted a jump-list pin change.</summary>
    JumpListUpdated,
    /// <summary>Whether installed application tasks appear in menus.</summary>
    SettingsAppTasks,
    /// <summary>Whether Windows-pinned destinations appear in menus.</summary>
    SettingsPinnedJumpLists,
}
