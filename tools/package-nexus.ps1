# Build a player/Nexus zip into releases/ (and stage under dist/)
$ErrorActionPreference = "Stop"
$Workspace = Split-Path $PSScriptRoot -Parent
$Version = "1.1.6"
$StageDir = Join-Path $Workspace "dist\UEBS2Stereo"
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

if (Test-Path $StageDir) { Remove-Item $StageDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path (Join-Path $StageDir "Bundles") | Out-Null
New-Item -ItemType Directory -Force -Path $ReleasesDir | Out-Null

Copy-Item $Dll (Join-Path $StageDir "UEBS2Stereo.dll") -Force
Copy-Item $Readme (Join-Path $StageDir "README.md") -Force
Copy-Item $Bundle (Join-Path $StageDir "Bundles\sbs_composite") -Force

foreach ($zip in @($ZipDist, $ZipRelease)) {
    if (Test-Path $zip) { Remove-Item $zip -Force }
}

Compress-Archive -Path $StageDir -DestinationPath $ZipDist -Force
Copy-Item $ZipDist $ZipRelease -Force

Write-Host "Staged:   $ZipDist"
Write-Host "Released: $ZipRelease"
Write-Host "Install: extract so BepInEx/plugins/UEBS2Stereo/ contains the DLL + Bundles."
Get-Item $ZipRelease | Format-List FullName, Length, LastWriteTime
