#Requires -Version 7
# Download/extract helpers for payload.ps1.

function Get-GitHubHeaders {
    $headers = @{ 'User-Agent' = 'ghx-payload' }
    if ($env:GH_TOKEN) {
        $headers['Authorization'] = "Bearer $($env:GH_TOKEN)"
    }
    elseif ($env:GITHUB_TOKEN) {
        $headers['Authorization'] = "Bearer $($env:GITHUB_TOKEN)"
    }
    return $headers
}

function Save-Url([string] $Url, [string] $Dest) {
    if (Test-Path $Dest) {
        Write-Host "Cached $Dest"
        return
    }
    Write-Host "Downloading $Url"
    $params = @{ Uri = $Url; OutFile = $Dest; MaximumRedirection = 5 }
    if ($Url -match '://(api\.)?github\.com/' -or $Url -match '://.*\.githubusercontent\.com/') {
        $params['Headers'] = Get-GitHubHeaders
    }
    Invoke-WebRequest @params
}

function Expand-TarGz([string] $Archive, [string] $Dest) {
    New-Item -ItemType Directory -Path $Dest -Force | Out-Null
    & tar -xf $Archive -C $Dest
    if ($LASTEXITCODE -ne 0) {
        throw "tar failed extracting $Archive (exit $LASTEXITCODE)"
    }
}

function Expand-Zip([string] $Archive, [string] $Dest) {
    New-Item -ItemType Directory -Path $Dest -Force | Out-Null
    # GNU tar cannot unpack zip; Expand-Archive is the cross-platform zip path.
    Expand-Archive -LiteralPath $Archive -DestinationPath $Dest -Force
}

function Get-GhAssetName([string] $PayloadRid, [string] $Version) {
    switch ($PayloadRid) {
        'win-x64' { "gh_${Version}_windows_amd64.zip" }
        'linux-x64' { "gh_${Version}_linux_amd64.tar.gz" }
        'linux-arm64' { "gh_${Version}_linux_arm64.tar.gz" }
        'osx-x64' { "gh_${Version}_macOS_amd64.zip" }
        'osx-arm64' { "gh_${Version}_macOS_arm64.zip" }
        default { throw "No GitHub CLI asset for $PayloadRid" }
    }
}

function Publish-GhPayload([string] $ExtractRoot, [string] $OutDir, [string] $ExeName) {
    $direct = Join-Path $ExtractRoot "bin/$ExeName"
    $source = $null
    if (Test-Path -LiteralPath $direct) {
        $source = $ExtractRoot
    }
    else {
        $nested = @(Get-ChildItem $ExtractRoot -Directory -ErrorAction SilentlyContinue | Where-Object {
            Test-Path -LiteralPath (Join-Path $_.FullName "bin/$ExeName")
        }) | Select-Object -First 1
        if ($nested) {
            $source = $nested.FullName
        }
    }
    if (-not $source) {
        throw "Archive did not contain bin/$ExeName"
    }
    Get-ChildItem $source -Force | ForEach-Object {
        Move-Item $_.FullName (Join-Path $OutDir $_.Name) -Force
    }
    if (-not (Test-Path -LiteralPath (Join-Path $OutDir "bin/$ExeName"))) {
        throw "Failed to place bin/$ExeName in $OutDir"
    }
}
