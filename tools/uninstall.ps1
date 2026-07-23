# Uninstall only manifest-owned BepInEx paths
$ErrorActionPreference = "Stop"
$GameRoot = "C:\Program Files (x86)\Steam\steamapps\common\UEBS2"
$Workspace = Split-Path $PSScriptRoot -Parent
$ManifestPath = Join-Path $Workspace "docs\bepinex-install-manifest.json"

if (-not (Test-Path $ManifestPath)) {
    throw "Manifest not found: $ManifestPath"
}

$manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json
$paths = @()
if ($manifest.extractedPaths) { $paths += $manifest.extractedPaths }
if ($manifest.postLaunchDelta) { $paths += $manifest.postLaunchDelta }

# Also remove our plugin if present (modular folder + legacy flat paths)
$paths += "BepInEx\plugins\UEBS2Stereo\Bundles\sbs_composite"
$paths += "BepInEx\plugins\UEBS2Stereo\UEBS2Stereo.dll"
$paths += "BepInEx\plugins\UEBS2Stereo\README.md"
$paths += "BepInEx\plugins\UEBS2Stereo"
$paths += "BepInEx\plugins\UEBS2Stereo.dll"
$paths += "BepInEx\plugins\sbs_composite"
$paths += "BepInEx\plugins\Bundles\sbs_composite"
$paths += "BepInEx\plugins\Bundles"
$paths += "BepInEx\config\com.uebs2.stereo.cfg"

$paths = $paths | Select-Object -Unique | Sort-Object { $_.Length } -Descending

foreach ($rel in $paths) {
    $full = Join-Path $GameRoot $rel
    if (Test-Path $full) {
        Write-Host "Removing $rel"
        Remove-Item -Force -Recurse $full -ErrorAction SilentlyContinue
    }
}

Write-Host "Uninstall complete (manifest-owned paths only)."
