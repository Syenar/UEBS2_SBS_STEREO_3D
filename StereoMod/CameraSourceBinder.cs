using System;
using UnityEngine;

namespace UEBS2Stereo
{
    internal sealed class CameraSourceBinder
    {
        private static readonly string[] ControllerNames = { "HeroCamera", "FreeCamera", "ExampleCharacterCamera", "FlyCamera" };
        private readonly StereoRig rig;
        private readonly StereoCompositor compositor;
        private readonly AcceptanceLedger ledger;
        private readonly Camera[] buffer = new Camera[64];
        private int boundId;
        private int lastFingerprint;
        private Camera fallback;

        internal Camera SourceCamera => rig.Source;
        internal Camera FallbackCamera => fallback;

        internal CameraSourceBinder(StereoRig rig, StereoCompositor compositor, AcceptanceLedger ledger)
        {
            this.rig = rig;
            this.compositor = compositor;
            this.ledger = ledger;
        }

        internal void FindAndBind() { Rebind(FindBestCamera()); }

        internal void Invalidate() { boundId = 0; lastFingerprint = 0; }

        internal void UpdateBinding()
        {
            int count = Camera.GetAllCameras(buffer);
            int fingerprint = count;
            for (int i = 0; i < count; i++)
            {
                Camera c = buffer[i];
                if (c == null) continue;
                fingerprint = fingerprint * 31 + c.GetInstanceID() + (c.enabled ? 1 : 0) + (c.targetTexture == null ? 7 : 0);
            }
            if (fingerprint == lastFingerprint && rig.Source != null && rig.Source.enabled) return;
            lastFingerprint = fingerprint;
            Camera next = FindBestCamera(count);
            if (next != null && next.GetInstanceID() != boundId) Rebind(next);
            else if (next == null) Plugin.Instance.EnterDualMonoSbs("Camera stack has no supported source.");
        }

        private Camera FindBestCamera(int knownCount = -1)
        {
            int count = knownCount >= 0 ? knownCount : Camera.GetAllCameras(buffer);
            Camera best = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                Camera c = buffer[i];
                if (c == null || !c.enabled) continue;
                if (c.gameObject.name.StartsWith("UEBS2Stereo", StringComparison.Ordinal)) continue;
                if (c.targetTexture != null && c.GetInstanceID() != boundId) continue;
                if (c.orthographic) continue;

                float score = c.depth;
                string hierarchy = HierarchyNames(c.transform);
                for (int n = 0; n < ControllerNames.Length; n++)
                {
                    if (hierarchy.IndexOf(ControllerNames[n], StringComparison.OrdinalIgnoreCase) >= 0)
                        score += 1000 - n;
                }
                if (c.CompareTag("MainCamera")) score += 100;
                if (score > bestScore) { best = c; bestScore = score; }
            }
            return best;
        }

        private static string HierarchyNames(Transform t)
        {
            string s = "";
            for (; t != null; t = t.parent) s += t.name + "/";
            return s;
        }

        private void Rebind(Camera c)
        {
            if (c == null) return;
            try
            {
                Vector2Int size = compositor.EyeSize;
                rig.Bind(c, size.x, size.y);
                boundId = c.GetInstanceID();
                bool dual = !rig.HasValidSource;
                compositor.SetDualMono(dual);
                rig.SetDualMono(dual);
                ledger.Add("Camera", "bound", c.name + " " + size.x + "x" + size.y);
                Plugin.Log.LogInfo("Bound stereo source: " + c.name);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Camera bind failed: " + ex.Message);
                Plugin.Instance.EnterDualMonoSbs("Camera bind error.");
            }
        }

        internal void EnsureFallbackCamera()
        {
            if (fallback != null) return;
            GameObject go = new GameObject("UEBS2Stereo Fallback Mono Camera");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            fallback = go.AddComponent<Camera>();
            fallback.enabled = true;
            fallback.clearFlags = CameraClearFlags.SolidColor;
            fallback.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            fallback.cullingMask = 0;
            fallback.tag = "Untagged";
            fallback.stereoTargetEye = StereoTargetEyeMask.None;
            fallback.fieldOfView = 60f;
            fallback.nearClipPlane = 0.3f;
            fallback.farClipPlane = 1000f;
        }

        internal void Clear()
        {
            if (fallback != null) UnityEngine.Object.Destroy(fallback.gameObject);
            fallback = null;
            boundId = 0;
            lastFingerprint = 0;
        }
    }
}
