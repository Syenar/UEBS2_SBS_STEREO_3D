# UEBS2 Half-SBS Stereo

BepInEx 5 plugin that presents **Ultimate Epic Battle Simulator 2** as fuseable **half side-by-side** stereo for 3D projectors.

**Current packages:**
- **Easiest (BepInEx included):** [`releases/UEBS2Stereo-EasyInstall-1.1.6.zip`](../releases/UEBS2Stereo-EasyInstall-1.1.6.zip)
- Plugin only (if you already have BepInEx): [`releases/UEBS2Stereo-1.1.6.zip`](../releases/UEBS2Stereo-1.1.6.zip)

## Requirements

- UEBS2 (Unity 2018.4.26f1, Mono x64)
- BepInEx 5 x64 — **included in the EasyInstall zip**

## Install (easiest)

1. Close UEBS2.
2. Download [`UEBS2Stereo-EasyInstall-1.1.6.zip`](../releases/UEBS2Stereo-EasyInstall-1.1.6.zip).
3. Open the zip → open **`Into_UEBS2_Game_Folder`**.
4. Copy everything inside into your UEBS2 folder (where `UEBS2.exe` is), or run `Install.bat`.
5. Launch and press **F8**.

## Install (plugin only)

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) Windows x64 if needed.
2. Extract [`UEBS2Stereo-1.1.6.zip`](../releases/UEBS2Stereo-1.1.6.zip) into the UEBS2 game folder.
3. Launch and press **F8**.

## Hotkeys

| Key | Action |
|-----|--------|
| F8 | Toggle stereo |
| F3 / F4 | Weaker / stronger depth (% of screen) |
| F1 / F2 | Screen plane closer / farther (farther = more pop-out) |
| PageDown / PageUp | Screen plane (same as F1/F2) |
| Home / End | Depth (same as F3/F4) |
| Keypad - / + | Screen plane |
| F5 | Comfort reset (recover from runaway tuning) |
| F10 | Re-auto place pop-out screen plane |
| F7 | Swap left/right eyes |
| F6 | Zero-IPD / flat diagnostic |
| F9 | Exit proof UI hide (only if enabled in config) |

While stereo is on, a large HUD shows live **DEPTH %**, **SCREEN**, and **IPD**.

## Config

`BepInEx/config/com.uebs2.stereo.cfg`

- `Stereo.ResolutionScale` (default `0.5`) — lower = smoother on high-res displays
- `Stereo.AllowHighResolutionScale` (default `false`) — permits scales above 0.5
- `Stereo.EyeSeparation` (default `8`) — bootstrap IPD; live tuning uses depth %
- `Stereo.Convergence` (default `140`) — bootstrap screen plane; auto-place biases behind subject for pop-out
- `Stereo.AutoScreenPlane` (default `true`) — auto screen plane on engage / F10
- `Stereo.MaxEyeSeparation` (default `80`) — hard IPD cap
- `Debug.FirstProofUiHide` (default `false`) — temporary UI hide for world-stereo proof only

## Uninstall

Delete `BepInEx/plugins/UEBS2Stereo/` and optionally `BepInEx/config/com.uebs2.stereo.cfg`.  
This mod never modifies `UEBS2.exe`, `Assembly-CSharp.dll`, `UnityPlayer.dll`, or `boot.config`.

## Modular layout

Zip paths start at `BepInEx/plugins/` so you can extract onto the game root:

```
BepInEx/plugins/UEBS2Stereo/
  UEBS2Stereo.dll
  README.md
  INSTALL.txt
  Bundles/sbs_composite
```

Rebuild the zip anytime with:

```powershell
powershell -File tools/package-nexus.ps1
```
