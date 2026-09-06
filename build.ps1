param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\PEAK',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'PeakTrollMod.csproj'
$frameworkMsBuild = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe'
$msbuildCommand = Get-Command msbuild.exe -ErrorAction SilentlyContinue
$builder = if ($msbuildCommand) { $msbuildCommand.Source } elseif (Test-Path -LiteralPath $frameworkMsBuild) { $frameworkMsBuild } else { throw 'MSBuild was not found. Install the .NET Framework 4.7.2 Developer Pack or Visual Studio Build Tools.' }

if (-not (Test-Path -LiteralPath (Join-Path $GameDir 'PEAK_Data\Managed\Assembly-CSharp.dll'))) {
    throw "PEAK managed assemblies were not found beneath: $GameDir"
}

& $builder $project /t:Rebuild "/p:Configuration=$Configuration" "/p:GameDir=$GameDir" /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

Write-Host "Built: $(Join-Path $PSScriptRoot "bin\$Configuration\PeakTrollMod.dll")"
