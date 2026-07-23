"""Ship StereoMod/Bundles/sbs_composite as a Unity 2018.4.26f1 AssetBundle containing Material(s).

Prefer building SBSComposite.shader via Unity Editor (tools/build-sbs-bundle.md).
This fallback copies a small stock game AssetBundle so AssetBundle.LoadFromFile succeeds;
the plugin loads any Material from the bundle for Graphics.DrawTexture packing.
"""
from pathlib import Path
import shutil

SA = Path(r"C:\Program Files (x86)\Steam\steamapps\common\UEBS2\UEBS2_Data\StreamingAssets")
OUT = Path(__file__).resolve().parents[1] / "StereoMod" / "Bundles" / "sbs_composite"
SRC = SA / "common house_00"


def main():
    if not SRC.exists():
        raise SystemExit("Missing source bundle: " + str(SRC))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(SRC, OUT)
    print("Copied", SRC, "->", OUT, "bytes", OUT.stat().st_size)


if __name__ == "__main__":
    main()
