# Compact Start

ReStart's pinned board already scrolls in both directions. Compact puts that same board in a smaller Start window, so a group outside the viewport remains reachable without moving its tiles.

## Enable Compact

1. Build and run the fork on Windows, or launch its portable build. A Release build of the solution places the launcher at `src/ReStart.App/bin/Release/net10.0-windows/ReStart.exe`.
2. Open **Settings → Appearance → Start menu mode**, select **Compact**, and save. **Fullscreen** remains the default for profiles created before this change.
3. Press the Windows key to open Start. Drag an edge or corner when the sizing cursor appears. With a bottom taskbar, drag the top edge upward to make the menu taller. Escape and outside dismissal use the existing Start settings and input paths.

Compact starts at 800 × 640 device-independent pixels (DIPs). Its minimum is 640 × 480 DIPs where the monitor's work area can accommodate it. The menu opens against the taskbar on the display selected by the existing Start-button or pointer path. It opens at the left of a horizontal taskbar and at the top of a vertical one. Dragging a docked edge changes the size from its opposite edge so the menu stays against the taskbar.

The completed resize stores the changed dimensions in the local layout preferences. Opening on a smaller or higher-DPI display limits the window to the available area while retaining the preferred dimensions for a later opening. Changing back to Fullscreen keeps those preferences for the next use of Compact. A temporary auto-hide reveal uses the content-safe area published by the existing taskbar integration.

## Upstream integration

| Area | Fork change and merge point |
| --- | --- |
| Window lifecycle | `MainWindow.Compact.cs` owns the native frame, hit tests, sizing loop and resize save. Keep the HWND hook in `MainWindow.xaml.cs` and the shared placement call in the cold startup and warm presentation paths. |
| Placement and DPI | `MainWindow.Taskbar.cs` selects the original fullscreen fit or the compact fit. `CompactMenuBounds.cs` calculates the physical bounds. `TaskbarNativeBackend.cs` reads the dock edge using the existing Explorer and auto-hide discovery. Review these seams if upstream changes working-area resolution, DPI handling or taskbar reveal. |
| Input | `StartIntegration.cs` routes an outside click in Compact through the existing dismiss callback. MainWindow guards taskbar and focus dismissal during the native resize loop. Preserve these guards if upstream changes dismissal or activation. |
| Settings and storage | The mode and two DIP dimensions are additive fields on `Preferences`, normalized by `LayoutDocument` and saved by `LayoutStore`. The Appearance selector uses the normal settings draft and all eight locale catalogs. Review these additions if upstream changes the settings schema or editor. |
| Updates | The existing release source and updater still apply. A package from upstream can replace the fork executable. |

The usual preview window keeps its development frame. The isolated Compact fixture opts into the production frame and geometry paths with a fictional layout.

## Verification

```powershell
dotnet restore ReStart.slnx --locked-mode
dotnet build ReStart.slnx -c Release --no-restore
dotnet test ReStart.slnx -c Release --no-build
./scripts/test-compact.ps1 -Executable src/ReStart.App/bin/Release/net10.0-windows/ReStart.exe
```

The core tests cover geometry for all resize directions, taskbar edges, negative monitor coordinates, and several DPI values. The Windows fixture sends synthetic sizing messages to an owned HWND and checks its client area, resize hit regions, saved dimensions, changes to the work area, search, scrolling, a live clock and the switch back to Fullscreen.

For a physical Windows check, toggle Start with the Windows key, drag each edge and corner, dismiss with Escape or by clicking another window, then reopen. Repeat on displays with different scaling and resolutions, with the taskbar auto-hidden and with the taskbar on another edge where available. Try moving a tile into a folder after scrolling. These checks exercise native pointer capture, Explorer and display transitions beyond the fixture's coverage.
