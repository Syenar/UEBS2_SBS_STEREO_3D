using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace UEBS2Stereo
{
    internal static class RuntimeProbe
    {
        private static bool wroteSceneProbe;

        internal static void Write(AcceptanceLedger ledger, string reason = "startup")
        {
            try
            {
                StringBuilder json = new StringBuilder();
                json.Append("{\"utc\":\"").Append(DateTime.UtcNow.ToString("o"))
                    .Append("\",\"reason\":\"").Append(E(reason))
                    .Append("\",\"cameras\":[");

                Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
                bool firstCam = true;
                for (int i = 0; i < cameras.Length; i++)
                {
                    Camera c = cameras[i];
                    if (c == null || !c.gameObject.scene.IsValid()) continue;
                    if (!firstCam) json.Append(',');
                    firstCam = false;
                    CommandBuffer[] before = null;
                    try { before = c.GetCommandBuffers(CameraEvent.BeforeForwardOpaque); } catch { before = new CommandBuffer[0]; }
                    json.Append("{\"name\":\"").Append(E(c.name))
                        .Append("\",\"enabled\":").Append(c.enabled ? "true" : "false")
                        .Append(",\"depth\":").Append(c.depth)
                        .Append(",\"tag\":\"").Append(E(c.tag))
                        .Append("\",\"targetTexture\":").Append(c.targetTexture != null ? "true" : "false")
                        .Append(",\"orthographic\":").Append(c.orthographic ? "true" : "false")
                        .Append(",\"mask\":").Append(c.cullingMask)
                        .Append(",\"commandBuffers\":").Append(before != null ? before.Length : 0)
                        .Append('}');
                    ledger.Add(c.name, "camera-probed", "runtime camera");
                }

                json.Append("],\"canvases\":[");
                Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
                bool firstCanvas = true;
                for (int i = 0; i < canvases.Length; i++)
                {
                    Canvas c = canvases[i];
                    if (c == null || !c.gameObject.scene.IsValid()) continue;
                    if (!firstCanvas) json.Append(',');
                    firstCanvas = false;
                    json.Append("{\"name\":\"").Append(E(c.name))
                        .Append("\",\"mode\":\"").Append(c.renderMode)
                        .Append("\",\"enabled\":").Append(c.enabled ? "true" : "false")
                        .Append(",\"active\":").Append(c.gameObject.activeInHierarchy ? "true" : "false")
                        .Append('}');
                    ledger.Add(c.name, "canvas-probed", c.renderMode.ToString());
                }

                json.Append("],\"onGui\":[");
                bool firstGui = true;
                foreach (MonoBehaviour m in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
                {
                    if (m == null || !m.gameObject.scene.IsValid()) continue;
                    MethodInfo onGui = m.GetType().GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (onGui == null) continue;
                    if (!firstGui) json.Append(',');
                    firstGui = false;
                    string full = m.GetType().FullName ?? m.GetType().Name;
                    bool demo = full.IndexOf("Demo", StringComparison.OrdinalIgnoreCase) >= 0
                        || full.IndexOf("RFX1", StringComparison.OrdinalIgnoreCase) >= 0
                        || full.IndexOf("RainGUI", StringComparison.OrdinalIgnoreCase) >= 0;
                    json.Append("{\"type\":\"").Append(E(full)).Append("\",\"demo\":").Append(demo ? "true" : "false").Append('}');
                    ledger.Add(full, demo ? "imgui-demo" : "imgui-normal-play", "OnGUI owner");
                }

                json.Append("],\"effects\":[");
                bool firstFx = true;
                string[] deny = { "PostProcessingBehaviour", "HBAO", "AllPost", "UEBSTwoLighting", "PostLightBg", "RFX1_LegacyRenderDistortion" };
                foreach (Behaviour b in Resources.FindObjectsOfTypeAll<Behaviour>())
                {
                    if (b == null || !b.gameObject.scene.IsValid()) continue;
                    string name = b.GetType().Name;
                    bool match = false;
                    for (int i = 0; i < deny.Length; i++) if (string.Equals(deny[i], name, StringComparison.OrdinalIgnoreCase)) match = true;
                    if (!match && b.GetType().GetMethod("OnRenderImage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null) continue;
                    if (!firstFx) json.Append(',');
                    firstFx = false;
                    json.Append("{\"type\":\"").Append(E(b.GetType().FullName)).Append("\",\"enabled\":").Append(b.enabled ? "true" : "false").Append('}');
                    ledger.Add(name, "effect-probed", b.enabled ? "enabled" : "disabled");
                }

                json.Append("],\"pointerSites\":[\"Input.mousePosition\",\"Camera.ScreenPointToRay\",\"Camera.ScreenToWorldPoint\",\"MouseInPanel\",\"MouseOutPanel\",\"MouseInRect\",\"RelativeMousePosInRect\"]");
                json.Append(",\"cursor\":{\"visible\":").Append(Cursor.visible ? "true" : "false")
                    .Append(",\"lock\":\"").Append(Cursor.lockState).Append("\"}");
                json.Append(",\"colorSpace\":\"").Append(QualitySettings.activeColorSpace).Append('"');
                json.Append(",\"resolution\":{\"w\":").Append(Screen.width).Append(",\"h\":").Append(Screen.height).Append('}');
                json.Append(",\"ledger\":").Append(ledger.ToJson()).Append('}');

                WritePaths(json.ToString());
                Plugin.Log?.LogInfo("RuntimeProbe wrote docs/probe/latest.json (" + reason + ")");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning("Runtime probe failed: " + ex.Message);
            }
        }

        internal static void WriteOnceAfterScene(AcceptanceLedger ledger, string sceneName)
        {
            if (wroteSceneProbe) return;
            wroteSceneProbe = true;
            Write(ledger, "scene:" + sceneName);
        }

        internal static void ResetSceneFlag() { wroteSceneProbe = false; }

        private static void WritePaths(string content)
        {
            string pluginDir = Path.GetDirectoryName(typeof(RuntimeProbe).Assembly.Location) ?? ".";
            WriteOne(Path.Combine(pluginDir, "probe", "latest.json"), content);
            string[] candidates =
            {
                Path.Combine(@"c:\Users\samsa\Desktop\Workplace\Projects\UEBS2 Mods", "docs", "probe", "latest.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "docs", "probe", "latest.json")
            };
            foreach (string path in candidates) WriteOne(path, content);
        }

        private static void WriteOne(string path, string content)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, content);
            }
            catch { }
        }

        private static string E(string s)
        {
            return (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
