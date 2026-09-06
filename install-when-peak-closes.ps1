param(
    [string]$Source = (Join-Path $PSScriptRoot 'bin\Release\PeakTrollMod.dll'),
    [string]$Destination = 'C:\Program Files (x86)\Steam\steamapps\common\PEAK\BepInEx\plugins\PeakTrollMod\PeakTrollMod.dll',
    [string]$ExpectedSha256 = ''
)

$ErrorActionPreference = 'Stop'
$log = Join-Path $PSScriptRoot 'install-update.log'
try {
    $running = Get-Process -Name 'PEAK' -ErrorAction SilentlyContinue
    if ($running) { $running | Wait-Process }
    $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $Source).Hash
    if ([string]::IsNullOrWhiteSpace($ExpectedSha256)) { $ExpectedSha256 = $actual }
    if ($actual -ne $ExpectedSha256) { throw "Source DLL hash changed: $actual" }
    Copy-Item -LiteralPath $Source -Destination $Destination -Force
    $installed = (Get-FileHash -Algorithm SHA256 -LiteralPath $Destination).Hash
    if ($installed -ne $ExpectedSha256) { throw "Installed DLL hash mismatch: $installed" }
    "$(Get-Date -Format o) Installed $installed" | Set-Content -LiteralPath $log
}
catch {
    "$(Get-Date -Format o) FAILED: $($_.Exception.Message)" | Set-Content -LiteralPath $log
    exit 1
}
