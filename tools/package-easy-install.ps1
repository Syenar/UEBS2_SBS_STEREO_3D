# Build the easy all-in-one player zip:
#   releases/UEBS2Stereo-EasyInstall-<version>.zip
# Contains one folder to open, then copy everything into the UEBS2 game directory
# (BepInEx 5 x64 + UEBS2Stereo plugin already nested under BepInEx/plugins/).
$ErrorActionPreference = "Stop"
$Workspace = Split-Path $PSScriptRoot -Parent
$Version = "1.1.6"
$BepInExTag = "v5.4.23.2"
$BepInExZipName = "BepInEx_win_x64_5.4.23.2.zip"
$BepInExUrl = "https://github.com/BepInEx/BepInEx/releases/download/$BepInExTag/$BepInExZipName"

$ReleasesDir = Join-Path $Workspace "releases"
$CacheDir = Join-Path $Workspace "dist\cache"
$StageRoot = Join-Path $Workspace "dist\easy-install-root"
$DropFolderName = "Into_UEBS2_Game_Folder"
$DropDir = Join-Path $StageRoot $DropFolderName
$PluginDir = Join-Path $DropDir "BepInEx\plugins\UEBS2Stereo"
$ZipName = "UEBS2Stereo-EasyInstall-$Version.zip"
$ZipDist = Join-Path $Workspace "dist\$ZipName"
$ZipRelease = Join-Path $ReleasesDir $ZipName

Push-Location $Workspace
dotnet build StereoMod\UEBS2Stereo.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Pop-Location

$Dll = Join-Path $Workspace "StereoMod\bin\Release\UEBS2Stereo.dll"
$Bundle = Join-Path $Workspace "StereoMod\Bundles\sbs_composite"
$PluginReadme = Join-Path $Workspace "StereoMod\README.md"
if (-not (Test-Path $Dll)) { throw "Missing build output: $Dll" }
if (-not (Test-Path $Bundle)) { throw "Missing Bundles/sbs_composite" }

New-Item -ItemType Directory -Force -Path $CacheDir, $ReleasesDir | Out-Null
$CachedBep = Join-Path $CacheDir $BepInExZipName
if (-not (Test-Path $CachedBep)) {
    Write-Host "Downloading BepInEx $BepInExTag ..."
    Invoke-WebRequest -Uri $BepInExUrl -OutFile $CachedBep -UseBasicParsing
} else {
    Write-Host "Using cached $BepInExZipName"
}

if (Test-Path $StageRoot) { Remove-Item $StageRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $DropDir | Out-Null

Write-Host "Extracting BepInEx into drop folder..."
Expand-Archive -Path $CachedBep -DestinationPath $DropDir -Force

# Ensure plugin path exists even if BepInEx zip uses different casing
New-Item -ItemType Directory -Force -Path (Join-Path $PluginDir "Bundles") | Out-Null
Copy-Item $Dll (Join-Path $PluginDir "UEBS2Stereo.dll") -Force
Copy-Item $PluginReadme (Join-Path $PluginDir "README.md") -Force
Copy-Item $Bundle (Join-Path $PluginDir "Bundles\sbs_composite") -Force

$HowTo = @"
UEBS2 Half-SBS Stereo $Version - Easy Install
============================================

ONE-TIME INSTALL (about 30 seconds)
-----------------------------------
1. Make sure Ultimate Epic Battle Simulator 2 is closed.
2. Open this zip and open the folder:
      Into_UEBS2_Game_Folder
3. Select EVERYTHING inside that folder (Ctrl+A).
4. Copy it into your UEBS2 game folder - the same folder that has UEBS2.exe.
   Typical Steam path:
   C:\Program Files (x86)\Steam\steamapps\common\UEBS2
5. Launch UEBS2 once.
6. Press F8 to turn half-SBS stereo on/off.

OR use Install.bat inside Into_UEBS2_Game_Folder if Steam is in the default location.

WHAT THIS INCLUDES
------------------
- BepInEx 5.4.23.2 (Windows x64 / Mono) - the mod loader
- UEBS2Stereo plugin + compositor bundle

UNINSTALL
---------
Delete these from the UEBS2 game folder if you want to remove everything:
  winhttp.dll
  doorstop_config.ini
  .doorstop_version
  BepInEx\
"@
Set-Content -Path (Join-Path $StageRoot "README-INSTALL.txt") -Value $HowTo -Encoding UTF8
Set-Content -Path (Join-Path $DropDir "README-INSTALL.txt") -Value $HowTo -Encoding UTF8

$InstallBat = @'
@echo off
setlocal
set "DEST="
if exist "%ProgramFiles(x86)%\Steam\steamapps\common\UEBS2\UEBS2.exe" set "DEST=%ProgramFiles(x86)%\Steam\steamapps\common\UEBS2"
if exist "%ProgramFiles%\Steam\steamapps\common\UEBS2\UEBS2.exe" set "DEST=%ProgramFiles%\Steam\steamapps\common\UEBS2"
if "%DEST%"=="" (
  echo Could not find UEBS2 at the default Steam path.
  echo Copy everything in this folder into your UEBS2 directory manually.
  echo That is the folder that contains UEBS2.exe.
  pause
  exit /b 1
)
echo Installing into:
echo   %DEST%
echo.
xcopy /E /I /Y /Q "%~dp0*" "%DEST%\" >nul
if errorlevel 1 (
  echo Copy failed. Try running this bat as Administrator, or copy manually.
  pause
  exit /b 1
)
echo Done. Launch UEBS2 and press F8.
pause
'@
# Avoid copying the bat into itself recursively via xcopy of * - put bat at StageRoot level instead
Set-Content -Path (Join-Path $StageRoot "Install-to-default-Steam-UEBS2.bat") -Value $InstallBat -Encoding ASCII

# Better bat: lives inside drop folder but excludes itself from naive instructions;
# xcopy of * would copy the bat into game root which is fine.
$InstallBatInner = @'
@echo off
setlocal
set "DEST="
if exist "%ProgramFiles(x86)%\Steam\steamapps\common\UEBS2\UEBS2.exe" set "DEST=%ProgramFiles(x86)%\Steam\steamapps\common\UEBS2"
if exist "%ProgramFiles%\Steam\steamapps\common\UEBS2\UEBS2.exe" set "DEST=%ProgramFiles%\Steam\steamapps\common\UEBS2"
if "%DEST%"=="" (
  echo Could not find UEBS2 at the default Steam path.
  echo Manual install: select all files in this folder and copy them into the folder that contains UEBS2.exe.
  pause
  exit /b 1
)
echo Installing into:
echo   %DEST%
echo.
robocopy "%~dp0." "%DEST%" /E /XD /NFL /NDL /NJH /NJS /nc /ns /np >nul
set "RC=%ERRORLEVEL%"
if %RC% GEQ 8 (
  echo Copy failed. Try Run as administrator, or copy the folder contents manually.
  pause
  exit /b 1
)
echo Done. Launch UEBS2 and press F8 to toggle stereo.
pause
'@
Set-Content -Path (Join-Path $DropDir "Install.bat") -Value $InstallBatInner -Encoding ASCII

foreach ($zip in @($ZipDist, $ZipRelease)) {
    if (Test-Path $zip) { Remove-Item $zip -Force }
}

Compress-Archive -Path (Join-Path $StageRoot "*") -DestinationPath $ZipDist -Force
Copy-Item $ZipDist $ZipRelease -Force

Write-Host "Released: $ZipRelease"
Get-Item $ZipRelease | Format-List FullName, Length, LastWriteTime
Write-Host "Players: extract zip, open Into_UEBS2_Game_Folder, copy all into UEBS2 (or run Install.bat)."
