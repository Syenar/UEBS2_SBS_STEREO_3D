using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UEBS2Stereo
{
    internal sealed class StateSnapshot
    {
        private readonly Dictionary<int, CameraState> cameras = new Dictionary<int, CameraState>();
        private readonly Dictionary<int, CanvasState> canvases = new Dictionary<int, CanvasState>();
        private readonly Dictionary<int, BehaviourState> behaviours = new Dictionary<int, BehaviourState>();
        private bool cursorCaptured;
        private bool cursorVisible;
        private CursorLockMode cursorLock;

        internal bool HasState => cursorCaptured || cameras.Count != 0 || canvases.Count != 0 || behaviours.Count != 0;
        internal void CaptureCursor() { if (!cursorCaptured) { cursorCaptured = true; cursorVisible = Cursor.visible; cursorLock = Cursor.lockState; } }
        internal void Capture(Camera c)
        {
            if (c == null || cameras.ContainsKey(c.GetInstanceID())) return;
            cameras.Add(c.GetInstanceID(), new CameraState(c));
        }
        internal void Capture(Canvas c)
        {
            if (c == null || canvases.ContainsKey(c.GetInstanceID())) return;
            canvases.Add(c.GetInstanceID(), new CanvasState(c));
        }
        internal void Capture(Behaviour b)
        {
            if (b == null || behaviours.ContainsKey(b.GetInstanceID())) return;
            behaviours.Add(b.GetInstanceID(), new BehaviourState(b));
        }
        internal int OriginalCullingMask(Camera c)
        {
            return c != null && cameras.TryGetValue(c.GetInstanceID(), out CameraState s) ? s.cullingMask : c != null ? c.cullingMask : 0;
        }

        internal void RestoreCamera(Camera c)
        {
            if (c == null) return;
            if (cameras.TryGetValue(c.GetInstanceID(), out CameraState s)) s.Restore();
            else
            {
                c.ResetProjectionMatrix();
                c.targetTexture = null;
            }
        }

        internal void RestoreAll()
        {
            foreach (CameraState s in cameras.Values) s.Restore();
            foreach (CanvasState s in canvases.Values) s.Restore();
            foreach (BehaviourState s in behaviours.Values) s.Restore();
            if (cursorCaptured) { Cursor.visible = cursorVisible; Cursor.lockState = cursorLock; }
            cameras.Clear(); canvases.Clear(); behaviours.Clear(); cursorCaptured = false;
        }

        private class Identity
        {
            internal readonly Object target;
            internal readonly int id;
            internal readonly int sceneHandle;
            internal Identity(Object target) { this.target = target; id = target.GetInstanceID(); sceneHandle = target is Component c ? c.gameObject.scene.handle : 0; }
            internal bool Alive => target != null && (!(target is Component c) || c.gameObject.scene.handle == sceneHandle);
        }
        private sealed class CameraState : Identity
        {
            internal readonly int cullingMask; private readonly RenderTexture targetTexture; private readonly Rect rect; private readonly float depth; private readonly bool enabled;
            internal readonly CameraClearFlags clearFlags; private readonly Matrix4x4 projection; private readonly bool orthographic;
            internal CameraState(Camera c) : base(c) { cullingMask=c.cullingMask; targetTexture=c.targetTexture; rect=c.rect; depth=c.depth; enabled=c.enabled; clearFlags=c.clearFlags; projection=c.projectionMatrix; orthographic=c.orthographic; }
            internal void Restore() { if (!Alive) return; Camera c=(Camera)target; c.ResetProjectionMatrix(); c.cullingMask=cullingMask; c.targetTexture=targetTexture; c.rect=rect; c.depth=depth; c.clearFlags=clearFlags; c.orthographic=orthographic; c.projectionMatrix=projection; c.enabled=enabled; }
        }
        private sealed class CanvasState : Identity
        {
            private readonly RenderMode mode;
            private readonly Camera worldCamera;
            private readonly float planeDistance;
            private readonly bool enabled;
            private readonly bool active;
            private readonly List<GameObject> layerObjects = new List<GameObject>();
            private readonly List<int> layers = new List<int>();

            internal CanvasState(Canvas c) : base(c)
            {
                mode = c.renderMode;
                worldCamera = c.worldCamera;
                planeDistance = c.planeDistance;
                enabled = c.enabled;
                active = c.gameObject.activeSelf;
                CaptureLayers(c.transform);
            }

            private void CaptureLayers(Transform root)
            {
                if (root == null) return;
                layerObjects.Add(root.gameObject);
                layers.Add(root.gameObject.layer);
                for (int i = 0; i < root.childCount; i++) CaptureLayers(root.GetChild(i));
            }

            internal void Restore()
            {
                if (!Alive) return;
                Canvas c = (Canvas)target;
                c.renderMode = mode;
                c.worldCamera = worldCamera;
                c.planeDistance = planeDistance;
                c.enabled = enabled;
                c.gameObject.SetActive(active);
                for (int i = 0; i < layerObjects.Count; i++)
                {
                    if (layerObjects[i] != null) layerObjects[i].layer = layers[i];
                }
            }
        }
        private sealed class BehaviourState : Identity
        {
            private readonly bool enabled;
            internal BehaviourState(Behaviour b) : base(b) { enabled=b.enabled; }
            internal void Restore() { if (Alive) ((Behaviour)target).enabled=enabled; }
        }
    }
}
