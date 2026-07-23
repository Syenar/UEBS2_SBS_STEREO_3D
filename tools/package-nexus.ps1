# Build a Nexus-ready zip: dist/UEBS2Stereo-<version>.zip
$ErrorActionPreference = "Stop"
$Workspace = Split-Path $PSScriptRoot -Parent
$Version = "1.1.1"
$OutDir = Join-Path $Workspace "dist\UEBS2Stereo"
$Zip = Join-Path $Workspace "dist\UEBS2Stereo-$Version.zip"

Push-Location $Workspace
dotnet build StereoMod\UEBS2Stereo.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Pop-Location

if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path (Join-Path $OutDir "Bundles") | Out-Null

Copy-Item (Join-Path $Workspace "StereoMod\bin\Release\UEBS2Stereo.dll") (Join-Path $OutDir "UEBS2Stereo.dll") -Force
Copy-Item (Join-Path $Workspace "StereoMod\README.md") (Join-Path $OutDir "README.md") -Force
$Bundle = Join-Path $Workspace "StereoMod\Bundles\sbs_composite"
if (Test-Path $Bundle) {
    Copy-Item $Bundle (Join-Path $OutDir "Bundles\sbs_composite") -Force
} else {
    Write-Warning "Missing sbs_composite bundle"
}

if (Test-Path $Zip) { Remove-Item $Zip -Force }
Compress-Archive -Path $OutDir -DestinationPath $Zip -Force
Write-Host "Nexus package: $Zip"
Write-Host "Install by extracting so BepInEx/plugins/UEBS2Stereo/ contains the DLL."
