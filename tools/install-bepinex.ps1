# Install BepInEx 5 (Windows x64 Mono) into UEBS2
param(
    [string]$ReleaseTag = "v5.4.23.2"
)

$ErrorActionPreference = "Stop"

$GameRoot = "C:\Program Files (x86)\Steam\steamapps\common\UEBS2"
$Workspace = Split-Path $PSScriptRoot -Parent
$ManifestPath = Join-Path $Workspace "docs\bepinex-install-manifest.json"
$TempRoot = Join-Path $env:TEMP ("uebs2-bepinex-" + [guid]::NewGuid().ToString("N"))
$ZipUrl = "https://github.com/BepInEx/BepInEx/releases/download/$ReleaseTag/BepInEx_win_x64_5.4.23.2.zip"

Write-Host "Game root: $GameRoot"
if (-not (Test-Path (Join-Path $GameRoot "UEBS2.exe"))) {
    throw "UEBS2.exe not found at $GameRoot"
}

New-Item -ItemType Directory -Force -Path $TempRoot | Out-Null
$ZipPath = Join-Path $TempRoot "bepinex.zip"
Write-Host "Downloading $ZipUrl ..."
Invoke-WebRequest -Uri $ZipUrl -OutFile $ZipPath -UseBasicParsing

$ExtractPath = Join-Path $TempRoot "extract"
Expand-Archive -Path $ZipPath -DestinationPath $ExtractPath -Force

$before = @{}
Get-ChildItem -Path $GameRoot -Force -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
    $rel = $_.FullName.Substring($GameRoot.Length).TrimStart('\')
    $before[$rel] = $true
}

$collisions = @()
Get-ChildItem -Path $ExtractPath -Force -Recurse -File | ForEach-Object {
    $rel = $_.FullName.Substring($ExtractPath.Length).TrimStart('\')
    $dest = Join-Path $GameRoot $rel
    if (Test-Path $dest) { $collisions += $rel }
}

if ($collisions.Count -gt 0) {
    Remove-Item -Recurse -Force $TempRoot
    throw ("Aborting: destination collisions:`n" + ($collisions -join "`n"))
}

Write-Host "Copying BepInEx into game root..."
Copy-Item -Path (Join-Path $ExtractPath "*") -Destination $GameRoot -Recurse -Force

$afterFiles = @()
Get-ChildItem -Path $GameRoot -Force -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
    $rel = $_.FullName.Substring($GameRoot.Length).TrimStart('\')
    if (-not $before.ContainsKey($rel)) { $afterFiles += $rel }
}

$manifest = [ordered]@{
    installedAt = (Get-Date).ToString("o")
    releaseTag = $ReleaseTag
    gameRoot = $GameRoot
    extractedPaths = $afterFiles
    postLaunchDelta = @()
    note = "After first game launch, re-run tools/record-bepinex-delta.ps1 to capture generated files."
}

$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $ManifestPath -Encoding UTF8
Remove-Item -Recurse -Force $TempRoot

Write-Host "BepInEx installed. Manifest: $ManifestPath"
Write-Host "Launch UEBS2 once to generate BepInEx/config and plugins folders, then deploy the plugin."
