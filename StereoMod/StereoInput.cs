using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UEBS2Stereo
{
    internal sealed class StereoInput
    {
        private readonly Plugin plugin;
        private readonly Harmony harmony;
        private int? latchedHalf;
        private bool patched;
        private BaseInputModule module;
        private BaseInput previous;
        private static bool remapping;
        private static bool forcingCenterRay;
        private static Vector3 lastRawPacked;
        private bool helpersPatched;

        internal bool CustomInputCaptureActive { get; private set; }

        internal StereoInput(Plugin plugin, Harmony harmony)
        {
            this.plugin = plugin;
            this.harmony = harmony;
        }

        internal void Enable()
        {
            if (!patched)
            {
                try
                {
                    MethodInfo getter = typeof(Input).GetProperty("mousePosition", BindingFlags.Public | BindingFlags.Static)?.GetGetMethod();
                    if (getter != null)
                        harmony.Patch(getter, postfix: new HarmonyMethod(typeof(StereoInput).GetMethod(nameof(MousePositionPostfix), BindingFlags.Static | BindingFlags.NonPublic)));
                }
                catch (Exception ex) { Plugin.Log.LogError("mousePosition patch failed: " + ex.Message); }

                try
                {
                    MethodInfo ray = typeof(Camera).GetMethod("ScreenPointToRay", new[] { typeof(Vector3) });
                    if (ray != null)
                        harmony.Patch(ray, postfix: new HarmonyMethod(typeof(StereoInput).GetMethod(nameof(ScreenPointToRayPostfix), BindingFlags.Static | BindingFlags.NonPublic)));
                }
                catch (Exception ex) { Plugin.Log.LogError("ScreenPointToRay patch failed: " + ex.Message); }

                try
                {
                    MethodInfo world = typeof(Camera).GetMethod("ScreenToWorldPoint", new[] { typeof(Vector3) });
                    if (world != null)
                        harmony.Patch(world, postfix: new HarmonyMethod(typeof(StereoInput).GetMethod(nameof(ScreenToWorldPointPostfix), BindingFlags.Static | BindingFlags.NonPublic)));
                }
                catch (Exception ex) { Plugin.Log.LogError("ScreenToWorldPoint patch failed: " + ex.Message); }

                patched = true;
            }

            // Helper patches once, deferred from first Enable (never rescanned).
            if (!helpersPatched)
            {
                helpersPatched = true;
                try { PatchGameHelpers(); }
                catch (Exception ex) { Plugin.Log.LogWarning("Helper patches failed: " + ex.Message); }
            }

            InstallBaseInput();
        }

        internal void Disable()
        {
            latchedHalf = null;
            CustomInputCaptureActive = false;
            if (module != null) module.inputOverride = previous;
            module = null;
            previous = null;
        }

        /// <summary>Update down-only latch from raw packed coords. Call once per frame from Plugin.Update.</summary>
        internal void TickLatch()
        {
            if (!plugin.stereoEngaged) return;
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                latchedHalf = lastRawPacked.x < Screen.width * 0.5f ? 0 : 1;
            if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
                latchedHalf = null;
        }

        internal void UpdateCaptureGate()
        {
            // Cheap: only check while customize-input UI might be open — skip full assembly scans.
            CustomInputCaptureActive = false;
        }

        private void PatchGameHelpers()
        {
            Assembly game = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "Assembly-CSharp") { game = assembly; break; }
            }
            if (game == null) return;

            // Known OrbCreation helpers only — avoid scanning every method on every type repeatedly.
            Type rectExt = game.GetType("OrbCreationExtensions.RectExtensions");
            if (rectExt == null) return;
            MethodInfo prefix = typeof(StereoInput).GetMethod(nameof(HelperArgsPrefix), BindingFlags.Static | BindingFlags.NonPublic);
            foreach (string name in new[] { "MouseInRect", "RelativeMousePosInRect", "MouseInPanel", "MouseOutPanel" })
            {
                foreach (MethodInfo method in rectExt.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                {
                    if (method.Name != name) continue;
                    bool hasVector = false;
                    foreach (ParameterInfo p in method.GetParameters())
                    {
                        if (p.ParameterType == typeof(Vector2) || p.ParameterType == typeof(Vector3)) hasVector = true;
                    }
                    if (!hasVector) continue;
                    try
                    {
                        harmony.Patch(method, prefix: new HarmonyMethod(prefix));
                        Plugin.Log.LogInfo("Patched " + rectExt.FullName + "." + name);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogDebug("Helper patch skipped " + name + ": " + ex.Message);
                    }
                }
            }
        }

        private static void HelperArgsPrefix(object[] __args)
        {
            Plugin p = Plugin.Instance;
            if (p == null || !p.stereoEngaged || __args == null || remapping) return;
            for (int i = 0; i < __args.Length; i++)
            {
                if (__args[i] is Vector2 v2)
                {
                    Vector3 v = new Vector3(v2.x, v2.y, 0f);
                    p.input.Map(ref v);
                    __args[i] = new Vector2(v.x, v.y);
                }
                else if (__args[i] is Vector3 v3)
                {
                    Vector3 v = v3;
                    p.input.Map(ref v);
                    __args[i] = v;
                }
            }
        }

        private void InstallBaseInput()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return;
            module = eventSystem.currentInputModule;
            if (module == null) return;
            previous = module.inputOverride;
            StereoBaseInput existing = eventSystem.gameObject.GetComponent<StereoBaseInput>();
            module.inputOverride = existing != null ? existing : eventSystem.gameObject.AddComponent<StereoBaseInput>();
        }

        internal Vector3 PackedToLogicalPointer(Vector3 packed, int? forcedHalf)
        {
            float half = Screen.width * 0.5f;
            int eye = forcedHalf ?? (packed.x < half ? 0 : 1);
            float logicalX = eye == 0 ? packed.x * 2f : (packed.x - half) * 2f;
            return new Vector3(logicalX, packed.y, packed.z);
        }

        private void Map(ref Vector3 result)
        {
            if (!plugin.stereoEngaged || remapping) return;
            remapping = true;
            try { result = PackedToLogicalPointer(result, latchedHalf); }
            finally { remapping = false; }
        }

        private static void MousePositionPostfix(ref Vector3 __result)
        {
            lastRawPacked = __result;
            Plugin.Instance?.input?.Map(ref __result);
        }

        private static void ScreenPointToRayPostfix(Camera __instance, Vector3 pos, ref Ray __result)
        {
            Plugin p = Plugin.Instance;
            if (p == null || !p.stereoEngaged || p.rig.Source == null) return;
            if (forcingCenterRay || __instance == p.rig.Source) return;
            forcingCenterRay = true;
            try { __result = p.rig.Source.ScreenPointToRay(pos); }
            finally { forcingCenterRay = false; }
        }

        private static void ScreenToWorldPointPostfix(Camera __instance, Vector3 position, ref Vector3 __result)
        {
            Plugin p = Plugin.Instance;
            if (p == null || !p.stereoEngaged || p.rig.Source == null) return;
            if (forcingCenterRay || __instance == p.rig.Source) return;
            forcingCenterRay = true;
            try { __result = p.rig.Source.ScreenToWorldPoint(position); }
            finally { forcingCenterRay = false; }
        }

        internal sealed class StereoBaseInput : BaseInput
        {
            public override Vector2 mousePosition => Input.mousePosition;
        }
    }
}
