# UEBS2 SBS Stereo 3D

Removable mod that renders **Ultimate Epic Battle Simulator 2** as fuseable **half side-by-side** stereo for 3D projectors.

## Easiest install (recommended)

**Download:** [`releases/UEBS2Stereo-EasyInstall-1.1.6.zip`](releases/UEBS2Stereo-EasyInstall-1.1.6.zip)

This zip includes **BepInEx + the stereo plugin** already nested correctly.

1. Close UEBS2.
2. Open the zip → open **`Into_UEBS2_Game_Folder`**.
3. Copy **everything inside that folder** into your UEBS2 game folder (the one with `UEBS2.exe`),  
   **or** double-click `Install.bat` if Steam is in the default location.
4. Launch UEBS2 and press **F8**.

Typical Steam path:  
`C:\Program Files (x86)\Steam\steamapps\common\UEBS2`

## Already have BepInEx?

Use the smaller plugin-only pack: [`releases/UEBS2Stereo-1.1.6.zip`](releases/UEBS2Stereo-1.1.6.zip)  
(extract into the game folder; it already contains `BepInEx/plugins/UEBS2Stereo/`).

## Hotkeys / config

See [`StereoMod/README.md`](StereoMod/README.md).

## Uninstall (easy pack)

From the UEBS2 game folder, delete:

- `winhttp.dll`
- `doorstop_config.ini`
- `.doorstop_version`
- `BepInEx\`

## Build packages from source

```powershell
powershell -File tools\package-easy-install.ps1   # BepInEx + plugin
powershell -File tools\package-nexus.ps1          # plugin only
```
