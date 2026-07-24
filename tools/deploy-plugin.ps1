# Build and deploy UEBS2Stereo into the game's BepInEx plugins folder.
# If the game has the DLL locked, rename-swap so the next launch loads the new build.
$ErrorActionPreference = "Stop"
$GameRoot = "C:\Program Files (x86)\Steam\steamapps\common\UEBS2"
$Workspace = Split-Path $PSScriptRoot -Parent
$Proj = Join-Path $Workspace "StereoMod\UEBS2Stereo.csproj"

Write-Host "Building Release..."
dotnet build $Proj -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

$Dll = Join-Path $Workspace "StereoMod\bin\Release\UEBS2Stereo.dll"
if (-not (Test-Path $Dll)) {
    throw "Build output not found: $Dll"
}

$PluginRoot = Join-Path $GameRoot "BepInEx\plugins\UEBS2Stereo"
$BundleSrc = Join-Path $Workspace "StereoMod\Bundles\sbs_composite"
$ReadmeSrc = Join-Path $Workspace "StereoMod\README.md"
$Target = Join-Path $PluginRoot "UEBS2Stereo.dll"
$Staged = Join-Path $PluginRoot "UEBS2Stereo.dll.new"
$Bak = Join-Path $PluginRoot ("UEBS2Stereo.dll.bak-" + (Get-Date -Format "yyyyMMdd-HHmmss"))

New-Item -ItemType Directory -Force -Path (Join-Path $PluginRoot "Bundles") | Out-Null

$deployed = $false
try {
    Copy-Item $Dll $Target -Force -ErrorAction Stop
    $deployed = $true
    Write-Host "Deployed DLL directly."
} catch {
    Write-Host "DLL locked by running game - attempting rename-swap..."
    try {
        if (Test-Path $Target) {
            Move-Item -LiteralPath $Target -Destination $Bak -Force
        }
        Copy-Item $Dll $Target -Force -ErrorAction Stop
        $deployed = $true
        Write-Host "Rename-swap succeeded. Restart UEBS2 to load the new build."
        Write-Host "Backup: $Bak"
    } catch {
        Copy-Item $Dll $Staged -Force
        Write-Host "Could not replace locked DLL. Staged as UEBS2Stereo.dll.new"
        Write-Host "Close UEBS2, then re-run this script."
        throw
    }
}

if ($deployed -and (Test-Path $Staged)) {
    Remove-Item $Staged -Force -ErrorAction SilentlyContinue
}

if (Test-Path $BundleSrc) {
    Copy-Item $BundleSrc (Join-Path $PluginRoot "Bundles\sbs_composite") -Force
} else {
    Write-Warning "Bundles/sbs_composite missing - stereo will refuse to engage."
}

if (Test-Path $ReadmeSrc) {
    Copy-Item $ReadmeSrc (Join-Path $PluginRoot "README.md") -Force
}

$legacyDll = Join-Path $GameRoot "BepInEx\plugins\UEBS2Stereo.dll"
$legacyBundle = Join-Path $GameRoot "BepInEx\plugins\Bundles"
if (Test-Path $legacyDll) { Remove-Item $legacyDll -Force -ErrorAction SilentlyContinue }
if (Test-Path $legacyBundle) { Remove-Item $legacyBundle -Recurse -Force -ErrorAction SilentlyContinue }

Get-Item $Target | Format-List FullName, Length, LastWriteTime
Write-Host "Deployed modular plugin -> $PluginRoot"
