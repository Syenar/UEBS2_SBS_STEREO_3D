# Deploy UEBS2Stereo as a self-contained BepInEx plugin folder (Nexus-ready)
$ErrorActionPreference = "Stop"
$GameRoot = "C:\Program Files (x86)\Steam\steamapps\common\UEBS2"
$Workspace = Split-Path $PSScriptRoot -Parent
$Dll = Join-Path $Workspace "StereoMod\bin\Release\UEBS2Stereo.dll"
if (-not (Test-Path $Dll)) {
    $Dll = Join-Path $Workspace "StereoMod\bin\Debug\UEBS2Stereo.dll"
}
if (-not (Test-Path $Dll)) {
    throw "Build output not found. Run: dotnet build StereoMod\UEBS2Stereo.csproj -c Release"
}

$PluginRoot = Join-Path $GameRoot "BepInEx\plugins\UEBS2Stereo"
$BundleSrc = Join-Path $Workspace "StereoMod\Bundles\sbs_composite"
$ReadmeSrc = Join-Path $Workspace "StereoMod\README.md"

New-Item -ItemType Directory -Force -Path (Join-Path $PluginRoot "Bundles") | Out-Null
Copy-Item $Dll (Join-Path $PluginRoot "UEBS2Stereo.dll") -Force

if (Test-Path $BundleSrc) {
    Copy-Item $BundleSrc (Join-Path $PluginRoot "Bundles\sbs_composite") -Force
} else {
    Write-Warning "Bundles/sbs_composite missing - stereo will refuse to engage."
}

if (Test-Path $ReadmeSrc) {
    Copy-Item $ReadmeSrc (Join-Path $PluginRoot "README.md") -Force
}

# Remove legacy flat deploy paths from earlier builds
$legacyDll = Join-Path $GameRoot "BepInEx\plugins\UEBS2Stereo.dll"
$legacyBundle = Join-Path $GameRoot "BepInEx\plugins\Bundles"
if (Test-Path $legacyDll) { Remove-Item $legacyDll -Force }
if (Test-Path $legacyBundle) { Remove-Item $legacyBundle -Recurse -Force }

Write-Host "Deployed modular plugin -> $PluginRoot"
