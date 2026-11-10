[CmdletBinding()]
param([Parameter(Mandatory)][string]$Executable)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$source = (Resolve-Path -LiteralPath $Executable).Path
$testRoot = Join-Path $repoRoot ('artifacts/compact/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$snapshot = Join-Path $testRoot 'start.png'
$process = Start-Process -FilePath $source -ArgumentList @('--compact-test', ('"' + $snapshot + '"')) -WindowStyle Hidden -PassThru
try {
    if (-not $process.WaitForExit(60000)) { $process.Kill(); throw 'Compact verification timed out.' }
    $result = Get-Content -LiteralPath ([IO.Path]::ChangeExtension($snapshot, '.json')) -Raw | ConvertFrom-Json
    if ($process.ExitCode -ne 0 -or -not $result.success -or $result.compactHitTests -ne 9) {
        $result | ConvertTo-Json -Depth 5 | Write-Output
        throw 'Compact verification failed.'
    }
    Write-Output "Compact hit tests: $($result.compactHitTests). Report: $testRoot"
}
finally { $process.Dispose() }
