import UnityPy

env = UnityPy.load(r"C:\Program Files (x86)\Steam\steamapps\common\UEBS2\UEBS2_Data\resources.assets")
mats = []
shaders = []
for obj in env.objects:
    if obj.type.name == "Material":
        try:
            d = obj.read()
            mats.append(getattr(d, "m_Name", None) or getattr(d, "name", "?"))
        except Exception as e:
            mats.append(f"<err {e}>")
    elif obj.type.name == "Shader":
        try:
            d = obj.read()
            shaders.append(getattr(d, "m_Name", None) or getattr(d, "name", "?"))
        except Exception as e:
            shaders.append(f"<err {e}>")
print("materials", len(mats))
print("sample mats", mats[:40])
print("shaders", len(shaders))
print("sample shaders", shaders[:40])
print("UnityPy attrs", [x for x in dir(UnityPy) if "bundle" in x.lower() or "save" in x.lower() or "Asset" in x])
