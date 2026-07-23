using UnityEngine;

namespace UEBS2Stereo
{
    internal sealed class StereoRig
    {
        private readonly StateSnapshot snapshot;
        private readonly StereoProjection projection;
        private Camera source;
        private Camera left;
        private Camera right;
        private Camera mono;
        private RenderTexture controlRt;
        private RenderTexture leftRt;
        private RenderTexture rightRt;
        private RenderTexture monoRt;
        private bool dualMono;
        private int allocW;
        private int allocH;

        internal Camera Source => source;
        internal RenderTexture LeftRt => leftRt;
        internal RenderTexture RightRt => rightRt;
        internal RenderTexture MonoRt => monoRt != null ? monoRt : leftRt;
        internal bool HasValidSource => source != null && projection.Valid(source, LogicalAspect);
        internal float LogicalAspect => allocH > 0 ? (float)allocW / allocH : (Screen.height > 0 ? (float)Screen.width / Screen.height : 1.7777778f);

        internal StereoRig(StateSnapshot snapshot, StereoProjection projection)
        {
            this.snapshot = snapshot;
            this.projection = projection;
        }

        internal void Bind(Camera camera, int width, int height)
        {
            if (camera == null) return;
            if (source == camera && leftRt != null && allocW == width && allocH == height)
            {
                LateSync();
                return;
            }

            DetachSource();
            source = camera;
            snapshot.Capture(source);
            Allocate(width, height, false);
            source.targetTexture = controlRt;
            source.cullingMask = 0;
            source.rect = new Rect(0f, 0f, 1f, 1f);

            left = CreateEye("UEBS2Stereo.LeftEye", leftRt);
            right = CreateEye("UEBS2Stereo.RightEye", rightRt);
            mono = CreateEye("UEBS2Stereo.MonoEye", monoRt);
            LateSync();
        }

        private Camera CreateEye(string name, RenderTexture target)
        {
            GameObject go = new GameObject(name);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(source.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            Camera eye = go.AddComponent<Camera>();
            eye.enabled = false;
            eye.tag = "Untagged";
            eye.stereoTargetEye = StereoTargetEyeMask.None;
            eye.targetTexture = target;
            eye.allowHDR = false;
            eye.allowMSAA = false;
            return eye;
        }

        private void Allocate(int w, int h, bool hdr)
        {
            ReleaseTextures();
            allocW = Mathf.Max(1, w);
            allocH = Mathf.Max(1, h);
            // ARGB32 is far cheaper than DefaultHDR at 1440p+ and avoids black-frame stalls.
            RenderTextureFormat format = RenderTextureFormat.ARGB32;
            controlRt = NewRt(allocW, allocH, format, "UEBS2 Control RT");
            leftRt = NewRt(allocW, allocH, format, "UEBS2 Left RT");
            rightRt = NewRt(allocW, allocH, format, "UEBS2 Right RT");
            monoRt = NewRt(allocW, allocH, format, "UEBS2 Mono RT");
        }

        private static RenderTexture NewRt(int w, int h, RenderTextureFormat format, string name)
        {
            RenderTexture rt = new RenderTexture(w, h, 24, format)
            {
                name = name,
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear
            };
            if (!rt.Create())
            {
                rt.Release();
                Object.Destroy(rt);
                rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32) { name = name, antiAliasing = 1 };
                rt.Create();
            }
            return rt;
        }

        internal void SetDualMono(bool value) { dualMono = value; }

        internal void LateSync()
        {
            if (source == null || mono == null) return;
            if (dualMono || !HasValidSource)
            {
                SyncEye(mono, true, MonoRt, forceCenter: true);
                return;
            }
            SyncEye(left, true, leftRt, forceCenter: false);
            SyncEye(right, false, rightRt, forceCenter: false);
            SyncEye(mono, true, MonoRt, forceCenter: true);
        }

        private void SyncEye(Camera eye, bool isLeft, RenderTexture rt, bool forceCenter)
        {
            if (eye == null || source == null || rt == null) return;

            // Copy FOV/planes from source without inheriting zero cull / control RT.
            float fov = source.fieldOfView;
            float near = source.nearClipPlane;
            float far = source.farClipPlane;
            CameraClearFlags clear = source.clearFlags;
            Color bg = source.backgroundColor;
            int mask = snapshot.OriginalCullingMask(source);

            eye.clearFlags = clear;
            eye.backgroundColor = bg;
            eye.nearClipPlane = near;
            eye.farClipPlane = far;
            eye.fieldOfView = fov;
            eye.orthographic = false;
            eye.cullingMask = mask;
            eye.targetTexture = rt;
            eye.rect = new Rect(0f, 0f, 1f, 1f);
            eye.enabled = false;
            eye.tag = "Untagged";
            eye.stereoTargetEye = StereoTargetEyeMask.None;
            eye.allowHDR = false;
            eye.allowMSAA = false;
            eye.depthTextureMode = DepthTextureMode.None;

            eye.transform.rotation = source.transform.rotation;
            float offset = forceCenter || dualMono ? 0f : projection.EyeOffset(isLeft);
            eye.transform.position = source.transform.position + source.transform.right * offset;

            if (forceCenter || dualMono)
            {
                eye.ResetProjectionMatrix();
                eye.aspect = LogicalAspect;
            }
            else
            {
                eye.projectionMatrix = projection.GetMatrix(source, LogicalAspect, isLeft);
            }
        }

        internal void RenderEyes()
        {
            if (source == null || mono == null) return;
            if (dualMono || !HasValidSource)
            {
                if (mono != null && MonoRt != null) mono.Render();
                return;
            }
            if (left != null && leftRt != null) left.Render();
            if (right != null && rightRt != null) right.Render();
        }

        internal void DetachSource()
        {
            DestroyCamera(left);
            DestroyCamera(right);
            DestroyCamera(mono);
            left = right = mono = null;
            if (source != null)
            {
                snapshot.RestoreCamera(source);
                source = null;
            }
            ReleaseTextures();
        }

        internal void Dispose() { DetachSource(); }

        private void ReleaseTextures()
        {
            Release(ref controlRt);
            Release(ref leftRt);
            Release(ref rightRt);
            Release(ref monoRt);
            allocW = allocH = 0;
        }

        private static void Release(ref RenderTexture rt)
        {
            if (rt == null) return;
            rt.Release();
            Object.Destroy(rt);
            rt = null;
        }

        private static void DestroyCamera(Camera c)
        {
            if (c != null) Object.Destroy(c.gameObject);
        }
    }
}
