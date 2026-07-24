# UEBS2 SBS Stereo 3D

Removable **BepInEx** mod that renders **Ultimate Epic Battle Simulator 2** as fuseable **half side-by-side** stereo for 3D projectors.

## Download / install

**Player package (ready to extract):** [`releases/UEBS2Stereo-1.1.6.zip`](releases/UEBS2Stereo-1.1.6.zip)

1. Install [BepInEx 5 Windows x64](https://github.com/BepInEx/BepInEx/releases) into your UEBS2 folder.
2. Extract the zip so you get `BepInEx/plugins/UEBS2Stereo/` (DLL + `Bundles/sbs_composite`).
3. Launch UEBS2 and press **F8**.

Full hotkeys/config: [`StereoMod/README.md`](StereoMod/README.md)

## Repo layout

| Path | Purpose |
|------|---------|
| `releases/` | Versioned install zips for players |
| `StereoMod/` | Plugin source + AssetBundle |
| `tools/package-nexus.ps1` | Rebuild the release zip |
| `tools/deploy-plugin.ps1` | Build + deploy into a local UEBS2 install |
| `docs/PLAN.md` | Authoritative Phase 1 plan |

## Build from source

```powershell
dotnet build StereoMod\UEBS2Stereo.csproj -c Release
powershell -File tools\package-nexus.ps1
```
