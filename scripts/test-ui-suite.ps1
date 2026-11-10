[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('navigation', 'layout', 'tiles')][string]$Suite,
    [Parameter(Mandatory)][string]$Executable
)
$ErrorActionPreference = 'Stop'
# Each suite runs sequentially on its own runner: native preview processes share
# a per-session instance lease and must never compete on the same desktop.
$suites = @{
    navigation = @('platform', 'navigation', 'app-folders', 'first-input', 'search-selection', 'fidelity', 'startup', 'warm-resume', 'app-menus', 'jump-lists', 'release-info', 'file-pinning', 'icons')
    layout = @('appearance', 'settings-preview', 'motion', 'placement', 'folder-drops', 'editing', 'scrolling', 'compact')
    tiles = @('bundled-tiles', 'studio', 'tile-data')
}
foreach ($check in $suites[$Suite]) {
    Write-Output "Verifying $check"
    if ($check -eq 'app-folders') { & (Join-Path $PSScriptRoot 'test-navigation.ps1') -Executable $Executable -AppFolders }
    else { & (Join-Path $PSScriptRoot "test-$check.ps1") -Executable $Executable }
}
