# Build SBS compositor AssetBundle

The player cannot compile `.shader` text at runtime. Ship a prebuilt Unity **2018.4.x** AssetBundle (prefer **2018.4.26f1** to match UEBS2).

## Requirements

- Unity Editor 2018.4.26f1 (or matching 2018.4.x)
- Shader/material sampling `_LeftTex`, `_RightTex`, `_UiTex`
- Remap each packed half to full eye UVs; apply identical UI UVs to both halves

## Output

- Place bundle at `StereoMod/Bundles/sbs_composite`
- Plugin loads via `AssetBundle.LoadFromFile`
- If missing: refuse `stereoEngaged`, log once

## Prefer Unity Editor 2018.4.26f1

Build `StereoMod/Bundles/src/SBSComposite.shader` into a material named `sbs_composite` and pack it into AssetBundle `sbs_composite`.

## Fallback without Unity Editor

```powershell
python tools/build_sbs_bundle_fallback.py
```

Copies a stock UEBS2 StreamingAssets UnityFS bundle. The plugin loads a Material from that bundle (prefab renderer materials are acceptable) and retargets it to `Unlit/Texture` for `Graphics.DrawTexture` packing. Replace with a true `_LeftTex`/`_RightTex`/`_UiTex` compositor material when Editor is available.
