using System;
using System.IO;
using UnityEngine;

namespace UEBS2Stereo
{
    internal sealed class StereoCompositor
    {
        private readonly StereoRig rig;
        private readonly StereoProjection projection;
        private Camera presenter;
        private Camera uiCamera;
        private RenderTexture uiRt;
        private RenderTexture packedRt;
        private Material material;
        private AssetBundle bundle;
        private Texture2D cursorTexture;
        private bool dualMono;
        private bool loggedMissingBundle;
        private bool materialReady;
        private bool rendering;
        private bool loggedRenderFrame;
        private bool loggedComposite;
        private int width;
        private int height;

        internal Camera UiCamera => uiCamera;
        internal float ResolutionScale { get; set; } = 0.5f;

        internal StereoCompositor(StereoRig rig, StereoProjection projection)
        {
            this.rig = rig;
            this.projection = projection;
        }

        internal bool EnsurePresenter()
        {
            if (!EnsureMaterial())
            {
                if (!loggedMissingBundle)
                {
                    Plugin.Log.LogError("Missing Bundles/sbs_composite next to the plugin. Stereo will not engage.");
                    loggedMissingBundle = true;
                }
                return false;
            }

            if (presenter != null) return true;

            GameObject p = new GameObject("UEBS2Stereo SBS Presenter");
            p.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(p);
            presenter = p.AddComponent<Camera>();
            presenter.clearFlags = CameraClearFlags.SolidColor;
            presenter.backgroundColor = Color.black;
            presenter.cullingMask = 0;
            presenter.depth = 1000f;
            presenter.allowHDR = false;
            presenter.allowMSAA = false;
            presenter.useOcclusionCulling = false;
            presenter.enabled = true;
            p.AddComponent<PresenterHook>().Initialize(this);

            GameObject u = new GameObject("UEBS2Stereo UI Camera");
            u.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(u);
            uiCamera = u.AddComponent<Camera>();
            uiCamera.enabled = false;
            uiCamera.clearFlags = CameraClearFlags.SolidColor;
            uiCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            uiCamera.cullingMask = 0;
            uiCamera.stereoTargetEye = StereoTargetEyeMask.None;
            uiCamera.allowHDR = false;
            uiCamera.allowMSAA = false;
            uiCamera.orthographic = false;
            uiCamera.fieldOfView = 60f;
            uiCamera.nearClipPlane = 0.3f;
            uiCamera.farClipPlane = 1000f;

            if (cursorTexture == null)
            {
                cursorTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                cursorTexture.SetPixel(0, 0, Color.white);
                cursorTexture.Apply(false, true);
            }

            EnsureTextures();
            return true;
        }

        private bool EnsureMaterial()
        {
            if (materialReady && material != null) return true;

            string pluginDir = Path.GetDirectoryName(typeof(StereoCompositor).Assembly.Location) ?? ".";
            string[] candidates =
            {
                Path.Combine(pluginDir, "Bundles", "sbs_composite"),
                Path.Combine(pluginDir, "sbs_composite"),
                Path.Combine(Directory.GetParent(pluginDir)?.FullName ?? pluginDir, "Bundles", "sbs_composite")
            };

            bool bundleOk = false;
            foreach (string path in candidates)
            {
                if (!File.Exists(path)) continue;
                try
                {
                    if (bundle == null) bundle = AssetBundle.LoadFromFile(path);
                    if (bundle != null)
                    {
                        bundleOk = true;
                        Plugin.Log.LogInfo("Compositor AssetBundle ready: " + path);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning("AssetBundle load failed: " + ex.Message);
                }
            }
            if (!bundleOk) return false;

            Shader blit = Shader.Find("Hidden/Internal-GUITexture")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("UI/Default");
            if (blit == null)
            {
                Plugin.Log.LogError("No blit shader available for SBS compositor.");
                return false;
            }

            material = new Material(blit);
            materialReady = true;
            return true;
        }

        internal void SetDualMono(bool value) { dualMono = value; }

        internal void LateSync()
        {
            if (presenter == null) return;
            float depth = HighestDepth() + 100f;
            if (presenter.depth < depth) presenter.depth = depth;
            if (width != Screen.width || height != Screen.height) EnsureTextures();
        }

        private float HighestDepth()
        {
            Camera[] cameras = new Camera[32];
            int count = Camera.GetAllCameras(cameras);
            float depth = 0f;
            for (int i = 0; i < count; i++)
            {
                Camera c = cameras[i];
                if (c != null && c != presenter && c.targetTexture == null)
                    depth = Mathf.Max(depth, c.depth);
            }
            return depth;
        }

        private void EnsureTextures()
        {
            width = Mathf.Max(1, Screen.width);
            height = Mathf.Max(1, Screen.height);
            Vector2Int eye = EyeSize;
            if (uiRt != null && packedRt != null
                && uiRt.width == eye.x && uiRt.height == eye.y
                && packedRt.width == width && packedRt.height == height) return;
            if (uiRt != null) { uiRt.Release(); UnityEngine.Object.Destroy(uiRt); }
            if (packedRt != null) { packedRt.Release(); UnityEngine.Object.Destroy(packedRt); }
            uiRt = new RenderTexture(eye.x, eye.y, 0, RenderTextureFormat.ARGB32)
            {
                name = "UEBS2 UI RT",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear
            };
            uiRt.Create();
            packedRt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "UEBS2 Packed SBS RT",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear
            };
            packedRt.Create();
            if (uiCamera != null)
            {
                uiCamera.targetTexture = uiRt;
                uiCamera.aspect = eye.x / (float)eye.y;
            }
        }

        internal Vector2Int EyeSize
        {
            get
            {
                float scale = Mathf.Clamp(ResolutionScale, 0.35f, 1f);
                return new Vector2Int(
                    Mathf.Max(1, Mathf.RoundToInt(Screen.width * scale)),
                    Mathf.Max(1, Mathf.RoundToInt(Screen.height * scale)));
            }
        }

        internal void RenderFrame()
        {
            if (rendering || rig.Source == null) return;
            if (!loggedRenderFrame)
            {
                loggedRenderFrame = true;
                Plugin.Log.LogInfo("SBS presenter OnPreCull is rendering eyes.");
            }
            rendering = true;
            try
            {
                rig.RenderEyes();
                if (uiCamera != null && uiRt != null) uiCamera.Render();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Stereo render failed: " + ex.Message);
            }
            finally
            {
                rendering = false;
            }
        }

        internal void Composite(RenderTexture destination)
        {
            if (material == null || packedRt == null) return;
            if (!loggedComposite)
            {
                loggedComposite = true;
                Plugin.Log.LogInfo("SBS compositor active. Final destination is "
                    + (destination == null ? "backbuffer (null RT)" : destination.name) + ".");
            }

            Texture left = dualMono ? (Texture)rig.MonoRt : (Texture)(projection.EyeSwap ? rig.RightRt : rig.LeftRt);
            Texture right = dualMono ? (Texture)rig.MonoRt : (Texture)(projection.EyeSwap ? rig.LeftRt : rig.RightRt);
            if (left == null) left = right;
            if (right == null) right = left;

            RenderTexture previous = RenderTexture.active;
            try
            {
                int dw = packedRt.width;
                int dh = packedRt.height;
                Graphics.SetRenderTarget(packedRt);
                GL.Clear(true, true, Color.black);
                if (left != null)
                {
                    GL.PushMatrix();
                    try
                    {
                        GL.LoadPixelMatrix(0f, dw, dh, 0f);
                        float half = dw * 0.5f;
                        DrawFlipped(new Rect(0f, 0f, half, dh), left);
                        DrawFlipped(new Rect(half, 0f, half, dh), right);
                        if (uiRt != null)
                        {
                            DrawFlipped(new Rect(0f, 0f, half, dh), uiRt);
                            DrawFlipped(new Rect(half, 0f, half, dh), uiRt);
                        }
                        DrawCursor(dw, dh);
                    }
                    finally
                    {
                        GL.PopMatrix();
                    }
                }

                Graphics.Blit(packedRt, destination);
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private void DrawFlipped(Rect screenRect, Texture texture)
        {
            if (texture == null) return;
            Rect uv = new Rect(0f, 0f, 1f, 1f);
            Graphics.DrawTexture(screenRect, texture, uv, 0, 0, 0, 0, Color.white, material);
        }

        private void DrawCursor(int dw, int dh)
        {
            if (cursorTexture == null) return;
            Vector3 pointer = Input.mousePosition;
            float half = dw * 0.5f;
            float sx = dw / (float)Mathf.Max(1, Screen.width);
            float sy = dh / (float)Mathf.Max(1, Screen.height);
            float x = Mathf.Clamp(pointer.x * 0.5f * sx, 0f, half - 1f);
            float y = Mathf.Clamp(pointer.y * sy, 0f, dh - 1f);
            Rect left = new Rect(x, dh - y - 16f, 16f, 16f);
            Graphics.DrawTexture(left, cursorTexture, material);
            Graphics.DrawTexture(new Rect(left.x + half, left.y, left.width, left.height), cursorTexture, material);
        }

        internal void DisposePresenter()
        {
            if (uiRt != null) { uiRt.Release(); UnityEngine.Object.Destroy(uiRt); uiRt = null; }
            if (packedRt != null) { packedRt.Release(); UnityEngine.Object.Destroy(packedRt); packedRt = null; }
            if (presenter != null) { UnityEngine.Object.Destroy(presenter.gameObject); presenter = null; }
            if (uiCamera != null) { UnityEngine.Object.Destroy(uiCamera.gameObject); uiCamera = null; }
            width = height = 0;
            rendering = false;
            loggedRenderFrame = false;
            loggedComposite = false;
        }

        internal void Dispose()
        {
            DisposePresenter();
            if (material != null) { UnityEngine.Object.Destroy(material); material = null; }
            if (cursorTexture != null) { UnityEngine.Object.Destroy(cursorTexture); cursorTexture = null; }
            if (bundle != null) { bundle.Unload(false); bundle = null; }
            materialReady = false;
        }

        private sealed class PresenterHook : MonoBehaviour
        {
            private StereoCompositor owner;
            internal void Initialize(StereoCompositor value) { owner = value; }
            private void OnPreCull() { owner?.RenderFrame(); }
            private void OnRenderImage(RenderTexture src, RenderTexture dst)
            {
                if (owner != null) owner.Composite(dst);
                else Graphics.Blit(src, dst);
            }
        }
    }
}
