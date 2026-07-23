# Record post-first-launch BepInEx delta into the install manifest
$ErrorActionPreference = "Stop"
$GameRoot = "C:\Program Files (x86)\Steam\steamapps\common\UEBS2"
$Workspace = Split-Path $PSScriptRoot -Parent
$ManifestPath = Join-Path $Workspace "docs\bepinex-install-manifest.json"

if (-not (Test-Path $ManifestPath)) { throw "Manifest missing: $ManifestPath" }
$manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json
$known = @{}
foreach ($p in $manifest.extractedPaths) { $known[$p] = $true }
foreach ($p in $manifest.postLaunchDelta) { $known[$p] = $true }

$delta = @()
Get-ChildItem -Path (Join-Path $GameRoot "BepInEx") -Force -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
    $rel = $_.FullName.Substring($GameRoot.Length).TrimStart('\')
    if (-not $known.ContainsKey($rel)) { $delta += $rel }
}

$manifest.postLaunchDelta = @($manifest.postLaunchDelta) + $delta | Select-Object -Unique
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $ManifestPath -Encoding UTF8
Write-Host ("Recorded {0} new paths." -f $delta.Count)
