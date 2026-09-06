param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\PEAK'
)

$ErrorActionPreference = 'Stop'
$icon = Join-Path $PSScriptRoot 'icon.png'
if (-not (Test-Path -LiteralPath $icon)) { throw 'Missing icon.png. Read ICON_INSTRUCTIONS.md and add an original 256x256 PNG.' }

& (Join-Path $PSScriptRoot 'build.ps1') -GameDir $GameDir -Configuration Release

$staging = Join-Path $PSScriptRoot 'package-staging'
$artifacts = Join-Path $PSScriptRoot 'artifacts'
$pluginFolder = Join-Path $staging 'BepInEx\plugins\PeakTrollMod'
if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
New-Item -ItemType Directory -Path $pluginFolder -Force | Out-Null
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null

Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin\Release\PeakTrollMod.dll') -Destination $pluginFolder
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Destination $staging
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $staging
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'CHANGELOG.md') -Destination $staging
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE') -Destination $staging
Copy-Item -LiteralPath $icon -Destination $staging

$archive = Join-Path $artifacts 'PEAK_Troll_Mod-0.2.0.zip'
if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $archive -CompressionLevel Optimal
Write-Host "Packaged: $archive"
