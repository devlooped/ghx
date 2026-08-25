#Requires -Version 7
<#
.SYNOPSIS
  Builds a GitHub CLI Payload tree for one RID into -OutDir from cli/cli release archives.
#>
param(
    [Parameter(Mandatory)]
    [ValidateSet('win-x64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')]
    [string] $Rid,

    [Parameter(Mandatory)]
    [string] $GitHubCliVersion,

    [Parameter(Mandatory)]
    [string] $OutDir
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$GitHubCliVersion = $GitHubCliVersion.Trim()

$OutDir = [System.IO.Path]::GetFullPath($OutDir)
$sentinel = Join-Path $OutDir '.payload'
$expected = "$Rid $GitHubCliVersion"
if ((Test-Path $sentinel) -and (Get-Content $sentinel -Raw).Trim() -eq $expected) {
    Write-Host "Payload already present: $expected"
    return
}

if (Test-Path $OutDir) {
    Remove-Item $OutDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutDir | Out-Null

$cache = Join-Path ([System.IO.Path]::GetDirectoryName($OutDir)) 'cache'
New-Item -ItemType Directory -Path $cache -Force | Out-Null
. (Join-Path $PSScriptRoot 'payload.functions.ps1')

$asset = Get-GhAssetName $Rid $GitHubCliVersion
$url = "https://github.com/cli/cli/releases/download/v$GitHubCliVersion/$asset"
$archive = Join-Path $cache $asset
Save-Url $url $archive

$stage = Join-Path $cache "extract-$Rid-$GitHubCliVersion"
if (Test-Path $stage) {
    Remove-Item $stage -Recurse -Force
}
if ($asset.EndsWith('.tar.gz', [System.StringComparison]::OrdinalIgnoreCase)) {
    Expand-TarGz $archive $stage
}
else {
    Expand-Zip $archive $stage
}

$exeName = if ($Rid.StartsWith('win-')) { 'gh.exe' } else { 'gh' }
Publish-GhPayload $stage $OutDir $exeName

if (-not $IsWindows -and $exeName -eq 'gh') {
    & chmod +x (Join-Path $OutDir 'bin/gh')
}

Set-Content -Path $sentinel -Value $expected -Encoding ascii
Write-Host "Payload ready: $OutDir"
