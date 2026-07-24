# Build a player/Nexus zip into releases/ (and stage under dist/)
# Zip layout extracts to the UEBS2 game root:
#   BepInEx/plugins/UEBS2Stereo/UEBS2Stereo.dll
#   BepInEx/plugins/UEBS2Stereo/Bundles/sbs_composite
#   BepInEx/plugins/UEBS2Stereo/README.md
$ErrorActionPreference = "Stop"
$Workspace = Split-Path $PSScriptRoot -Parent
$Version = "1.1.6"
$StageRoot = Join-Path $Workspace "dist\package-root"
$PluginDir = Join-Path $StageRoot "BepInEx\plugins\UEBS2Stereo"
$ReleasesDir = Join-Path $Workspace "releases"
$ZipName = "UEBS2Stereo-$Version.zip"
$ZipDist = Join-Path $Workspace "dist\$ZipName"
$ZipRelease = Join-Path $ReleasesDir $ZipName

Push-Location $Workspace
dotnet build StereoMod\UEBS2Stereo.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Pop-Location

$Dll = Join-Path $Workspace "StereoMod\bin\Release\UEBS2Stereo.dll"
$Bundle = Join-Path $Workspace "StereoMod\Bundles\sbs_composite"
$Readme = Join-Path $Workspace "StereoMod\README.md"
if (-not (Test-Path $Dll)) { throw "Missing build output: $Dll" }
if (-not (Test-Path $Bundle)) { throw "Missing Bundles/sbs_composite - required for install package." }

if (Test-Path $StageRoot) { Remove-Item $StageRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path (Join-Path $PluginDir "Bundles") | Out-Null
New-Item -ItemType Directory -Force -Path $ReleasesDir | Out-Null

Copy-Item $Dll (Join-Path $PluginDir "UEBS2Stereo.dll") -Force
Copy-Item $Readme (Join-Path $PluginDir "README.md") -Force
Copy-Item $Bundle (Join-Path $PluginDir "Bundles\sbs_composite") -Force

# Tiny marker so empty intermediate folders are obvious in explorers
$InstallTxt = @"
UEBS2 Half-SBS Stereo $Version

Install:
1. Install BepInEx 5 x64 into the UEBS2 game folder first (if needed).
2. Extract this zip into the UEBS2 game folder (same place as UEBS2.exe).
3. You should end up with:
   BepInEx\plugins\UEBS2Stereo\UEBS2Stereo.dll
   BepInEx\plugins\UEBS2Stereo\Bundles\sbs_composite
4. Launch the game and press F8.
"@
Set-Content -Path (Join-Path $PluginDir "INSTALL.txt") -Value $InstallTxt -Encoding UTF8

foreach ($zip in @($ZipDist, $ZipRelease)) {
    if (Test-Path $zip) { Remove-Item $zip -Force }
}

# Compress the contents of package-root so zip paths start at BepInEx/...
Compress-Archive -Path (Join-Path $StageRoot "*") -DestinationPath $ZipDist -Force
Copy-Item $ZipDist $ZipRelease -Force

Write-Host "Staged:   $ZipDist"
Write-Host "Released: $ZipRelease"
Write-Host "Extract zip into the UEBS2 game folder (next to UEBS2.exe)."
Get-Item $ZipRelease | Format-List FullName, Length, LastWriteTime
