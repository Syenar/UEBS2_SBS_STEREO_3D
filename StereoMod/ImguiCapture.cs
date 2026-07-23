using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace UEBS2Stereo
{
    internal sealed class ImguiCapture
    {
        private readonly Harmony harmony;
        private readonly AcceptanceLedger ledger;
        private bool typesPatched;

        internal ImguiCapture(Harmony harmony, AcceptanceLedger ledger)
        {
            this.harmony = harmony;
            this.ledger = ledger;
        }

        internal void Enable()
        {
            if (typesPatched) return;
            typesPatched = true;

            // Patch each declaring type once — not every MonoBehaviour instance (that freezes F8).
            Assembly game = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "Assembly-CSharp")
                {
                    game = assembly;
                    break;
                }
            }
            if (game == null) return;

            Type[] types;
            try { types = game.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types; }

            MethodInfo prefix = typeof(ImguiCapture).GetMethod(nameof(SkipGameOnGui), BindingFlags.Static | BindingFlags.NonPublic);
            var seen = new HashSet<MethodInfo>();
            int patched = 0;
            foreach (Type type in types)
            {
                if (type == null) continue;
                MethodInfo onGui = type.GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (onGui == null || onGui.GetParameters().Length != 0) continue;
                if (!seen.Add(onGui)) continue;
                try
                {
                    harmony.Patch(onGui, prefix: new HarmonyMethod(prefix));
                    patched++;
                    string full = type.FullName ?? type.Name;
                    bool demo = IsDemoOnly(full);
                    ledger.Add(full, demo ? "imgui-demo" : "imgui-normal-play",
                        demo ? "No-op while stereoEngaged; demo-only" : "No-op while stereoEngaged; uGUI path covers menus");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogDebug("OnGUI patch skipped: " + ex.Message);
                }
            }
            Plugin.Log.LogInfo("IMGUI OnGUI prefixes installed: " + patched);
        }

        private static bool IsDemoOnly(string full)
        {
            return full.IndexOf("Demo", StringComparison.OrdinalIgnoreCase) >= 0
                || full.IndexOf("RFX1", StringComparison.OrdinalIgnoreCase) >= 0
                || full.IndexOf("RainGUI", StringComparison.OrdinalIgnoreCase) >= 0
                || full.IndexOf("Example", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal void Disable() { }

        private static bool SkipGameOnGui()
        {
            return Plugin.Instance == null || !Plugin.Instance.stereoEngaged;
        }
    }
}
