# UEBS2 Half-SBS Stereo

BepInEx 5 plugin that presents **Ultimate Epic Battle Simulator 2** as fuseable **half side-by-side** stereo for 3D projectors.

## Requirements

- UEBS2 (Unity 2018.4.26f1, Mono x64)
- [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) Windows x64 (stable)

## Install (Nexus / manual)

1. Install BepInEx 5 into the UEBS2 game folder if you do not already have it.
2. Copy the `UEBS2Stereo` folder into `BepInEx/plugins/` so you have:
   ```
   BepInEx/plugins/UEBS2Stereo/UEBS2Stereo.dll
   BepInEx/plugins/UEBS2Stereo/Bundles/sbs_composite
   ```
3. Launch the game once so BepInEx generates config.
4. Press **F8** to toggle half-SBS.

## Hotkeys

| Key | Action |
|-----|--------|
| F8 | Toggle stereo |
| F1 / F2 | Screen plane closer / farther (farther = more pop-out) |
| F3 / F4 | Weaker / stronger eye separation |
| PageDown / PageUp | Screen plane (same as F1/F2) |
| Home / End | Separation (same as F3/F4) |
| Keypad - / + | Screen plane |
| [ ] - = | Same as above (via OnGUI; may miss on some layouts) |
| F10 | Re-auto place screen plane |
| F7 | Swap left/right eyes |
| F6 | Zero-IPD diagnostic |
| F9 | Exit proof UI hide (only if enabled in config) |

While stereo is on, a HUD line shows live `IPD` and `Screen` values so you can confirm keys are registering.

## Config

`BepInEx/config/com.uebs2.stereo.cfg`

- `Stereo.ResolutionScale` (default `0.5`) — lower = smoother on high-res displays
- `Stereo.AllowHighResolutionScale` (default `false`) — permits scales above 0.5
- `Stereo.EyeSeparation` (default `0.28`) — parallax strength; try `0.35`–`0.55` if still mild
- `Stereo.Convergence` (default `80`) — screen-plane distance when auto is off; farther = more pop-out
- `Stereo.AutoScreenPlane` (default `true`) — lock screen plane to look-at so near pops out / far stays behind
- `Stereo.MaxEyeSeparation` (default `1.5`) — clamp for live `[` / `]` tuning
- `Debug.FirstProofUiHide` (default `false`) — temporary UI hide for world-stereo proof only

## Uninstall

Delete `BepInEx/plugins/UEBS2Stereo/` and optionally `BepInEx/config/com.uebs2.stereo.cfg`.  
This mod never modifies `UEBS2.exe`, `Assembly-CSharp.dll`, `UnityPlayer.dll`, or `boot.config`.

## Modular layout

All plugin code lives under the `UEBS2Stereo` namespace and deploys as a single self-contained plugin folder for Nexus packaging.
