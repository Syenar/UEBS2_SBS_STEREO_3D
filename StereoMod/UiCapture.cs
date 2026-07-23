using System.Collections.Generic;
using UnityEngine;

namespace UEBS2Stereo
{
    /// <summary>
    /// Remaps Overlay/SSC canvases onto the UI capture camera.
    /// CRITICAL: never mutate canvases inside willRenderCanvases — that re-enters and freezes the player.
    /// </summary>
    internal sealed class UiCapture
    {
        private readonly StateSnapshot snapshot;
        private readonly AcceptanceLedger ledger;
        private readonly HashSet<int> converted = new HashSet<int>();
        private StereoCompositor compositor;
        private bool active;
        private bool proofHide;
        private bool scanRequested;
        private bool scanning;
        private int uiLayer = 31;
        private int framesUntilScan;

        internal UiCapture(StateSnapshot snapshot, AcceptanceLedger ledger)
        {
            this.snapshot = snapshot;
            this.ledger = ledger;
        }

        internal void Enable(StereoCompositor value)
        {
            compositor = value;
            active = true;
            converted.Clear();
            scanRequested = true;
            framesUntilScan = 0;
            uiLayer = FindLayer();
            if (compositor.UiCamera != null)
                compositor.UiCamera.cullingMask = 1 << uiLayer;

            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            Canvas.willRenderCanvases += OnWillRenderCanvases;
            // Convert immediately in Enable (outside render) — safe.
            scanning = false;
            ConvertNewCanvases();
            ledger.Add("UiCapture", "catch-all", "Deferred convert; willRenderCanvases only flags dirty");
        }

        internal void LateUpdateTick()
        {
            if (!active || proofHide) return;
            if (scanning) return;
            if (scanRequested || --framesUntilScan <= 0)
            {
                framesUntilScan = 20;
                scanRequested = false;
                ConvertNewCanvases();
            }
        }

        internal void Update()
        {
            if (!active) return;
            Cursor.visible = false;
        }

        /// <summary>Flag only — never remap here (Unity re-entrancy freeze).</summary>
        private void OnWillRenderCanvases()
        {
            if (!active || proofHide || scanning) return;
            scanRequested = true;
        }

        private void ConvertNewCanvases()
        {
            if (compositor == null || compositor.UiCamera == null || proofHide) return;
            if (scanning) return;
            scanning = true;
            try
            {
                Canvas[] all = Object.FindObjectsOfType<Canvas>();
                for (int i = 0; i < all.Length; i++)
                {
                    Canvas canvas = all[i];
                    if (canvas == null) continue;
                    int id = canvas.GetInstanceID();
                    if (converted.Contains(id)) continue;

                    if (canvas.renderMode == RenderMode.WorldSpace)
                    {
                        converted.Add(id);
                        ledger.Add(canvas.name, "world-space", "Left in eye renders");
                        continue;
                    }

                    if (canvas.worldCamera == compositor.UiCamera
                        && canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    {
                        converted.Add(id);
                        continue;
                    }

                    RemapCanvas(canvas);
                    converted.Add(id);
                }
            }
            finally
            {
                scanning = false;
            }
        }

        private void RemapCanvas(Canvas canvas)
        {
            snapshot.Capture(canvas);
            float planeDistance = canvas.planeDistance > 0.01f ? canvas.planeDistance : 1f;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = compositor.UiCamera;
            canvas.planeDistance = planeDistance;
            SetLayerRecursive(canvas.gameObject, uiLayer);
            ledger.Add(canvas.name, "ui-captured", "ScreenSpaceCamera");
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            Transform t = go.transform;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursive(t.GetChild(i).gameObject, layer);
        }

        private static int FindLayer()
        {
            for (int i = 31; i >= 8; i--)
            {
                if (string.IsNullOrEmpty(LayerMask.LayerToName(i))) return i;
            }
            return 31;
        }

        internal void EnterProofUiHide()
        {
            proofHide = true;
            Canvas[] all = Object.FindObjectsOfType<Canvas>();
            for (int i = 0; i < all.Length; i++)
            {
                Canvas c = all[i];
                if (c == null || c.renderMode == RenderMode.WorldSpace) continue;
                snapshot.Capture(c);
                c.enabled = false;
            }
            ledger.Add("Proof UI hide", "active", "Screen-space canvases hidden");
        }

        internal void ExitProofUiHide()
        {
            if (!proofHide) return;
            proofHide = false;
            Canvas[] all = Object.FindObjectsOfType<Canvas>();
            for (int i = 0; i < all.Length; i++)
            {
                Canvas c = all[i];
                if (c == null || c.renderMode == RenderMode.WorldSpace) continue;
                c.enabled = true;
            }
            converted.Clear();
            scanRequested = true;
            ledger.Add("Proof UI hide", "exited", "Visibility restored");
        }

        internal void Disable()
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            active = false;
            proofHide = false;
            scanning = false;
            scanRequested = false;
            converted.Clear();
            compositor = null;
        }
    }
}
