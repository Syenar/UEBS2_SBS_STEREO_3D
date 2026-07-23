# UEBS2 Half-SBS Stereo (3D Projector)

BepInEx plugin that presents **Ultimate Epic Battle Simulator 2** as fuseable **half side-by-side** stereo for 3D projectors / SBS displays.

Repository: [Syenar/UEBS2_SBS_STEREO_3D](https://github.com/Syenar/UEBS2_SBS_STEREO_3D)

## Requirements

- UEBS2 (Unity 2018.4.26f1, Mono x64)
- [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) Windows x64

## Install

1. Install BepInEx 5 into the UEBS2 game folder.
2. Copy the `UEBS2Stereo` folder into `BepInEx/plugins/` so you have:
   ```
   BepInEx/plugins/UEBS2Stereo/UEBS2Stereo.dll
   BepInEx/plugins/UEBS2Stereo/Bundles/sbs_composite
   ```
3. Launch the game, then press **F8** to toggle half-SBS.

From this repo after building:

```powershell
dotnet build StereoMod\UEBS2Stereo.csproj -c Release
powershell -File tools\deploy-plugin.ps1
```

Or package a Nexus-ready zip:

```powershell
powershell -File tools\package-nexus.ps1
```

## Hotkeys

| Key | Action |
|-----|--------|
| F8 | Toggle stereo |
| F9 | Exit proof UI hide (if enabled in config) |
| [ / ] | Eye separation (IPD) |
| ; / ' | Convergence |
| F7 | Swap left/right eyes |
| F6 | Zero-IPD diagnostic |

## Notes

- This is a **BepInEx runtime mod**, not a Steam Workshop character/content pack. Workshop cannot install the loader or plugin DLL.
- Prefer distributing via GitHub Releases / Nexus Mods.
- See [`StereoMod/README.md`](StereoMod/README.md) for config details.

## Uninstall

Delete `BepInEx/plugins/UEBS2Stereo/` (and optionally `BepInEx/config/com.uebs2.stereo.cfg`).  
Does not modify `UEBS2.exe`, `Assembly-CSharp.dll`, `UnityPlayer.dll`, or `boot.config`.
