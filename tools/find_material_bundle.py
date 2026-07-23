"""Find a UnityFS StreamingAssets bundle whose container exposes Material main assets."""
from pathlib import Path
import UnityPy

SA = Path(r"C:\Program Files (x86)\Steam\steamapps\common\UEBS2\UEBS2_Data\StreamingAssets")


def is_unityfs(path: Path) -> bool:
    try:
        with path.open("rb") as f:
            return f.read(7) == b"UnityFS"
    except Exception:
        return False


def main():
    hits = []
    for path in sorted(SA.iterdir(), key=lambda p: p.stat().st_size if p.is_file() else 1 << 60):
        if not path.is_file() or path.suffix.lower() == ".manifest" or not is_unityfs(path):
            continue
        if path.stat().st_size > 80_000_000:
            continue
        try:
            env = UnityPy.load(str(path))
        except Exception:
            continue
        container = getattr(env, "container", None) or {}
        mat_paths = []
        for asset_path, obj in container.items():
            try:
                typ = obj.type.name if hasattr(obj, "type") else "?"
            except Exception:
                typ = "?"
            if typ == "Material" or (isinstance(asset_path, str) and asset_path.lower().endswith(".mat")):
                mat_paths.append(asset_path)
        # Also check objects vs container size
        mat_objs = sum(1 for o in env.objects if o.type.name == "Material")
        if mat_paths or mat_objs:
            hits.append((path.stat().st_size, path.name, len(mat_paths), mat_objs, mat_paths[:5]))
            print(f"{path.stat().st_size:9d} {path.name}: container_mats={len(mat_paths)} obj_mats={mat_objs} sample={mat_paths[:3]}")
        if len(hits) >= 25:
            break

    if not hits:
        print("No hits; dumping container keys for common house_00")
        env = UnityPy.load(str(SA / "common house_00"))
        keys = list((env.container or {}).keys())[:40]
        print("container count", len(env.container or {}), "sample", keys)


if __name__ == "__main__":
    main()
