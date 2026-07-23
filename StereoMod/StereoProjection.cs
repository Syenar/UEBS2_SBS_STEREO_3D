using System;
using UnityEngine;

namespace UEBS2Stereo
{
    internal sealed class StereoProjection
    {
        internal float IpD { get; private set; } = 0.064f;
        internal float Convergence { get; private set; } = 10f;
        internal bool EyeSwap { get; private set; }
        internal void AdjustIpd(float amount) { IpD = Mathf.Clamp(IpD + amount, 0f, 0.25f); }
        internal void AdjustConvergence(float amount) { Convergence = Mathf.Clamp(Convergence + amount, 0.05f, 10000f); }
        internal void SetZeroIpd() { IpD = 0f; }
        internal void ToggleSwap() { EyeSwap = !EyeSwap; }
        internal bool Valid(Camera source, float aspect)
        {
            return source != null && !source.orthographic && source.nearClipPlane > 0f && source.farClipPlane > source.nearClipPlane &&
                   Convergence > source.nearClipPlane && IsFinite(aspect) && aspect > 0f && IsFinite(IpD);
        }
        internal Matrix4x4 GetMatrix(Camera source, float aspect, bool left)
        {
            if (!Valid(source, aspect)) throw new ArgumentException("Invalid stereo frustum.");
            bool physicalLeft = EyeSwap ? !left : left;
            float eye = (physicalLeft ? -0.5f : 0.5f) * IpD;
            float n = source.nearClipPlane;
            float t = n * Mathf.Tan(source.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float w = t * aspect;
            float shift = -eye * n / Convergence;
            return Matrix4x4.Frustum(-w + shift, w + shift, -t, t, n, source.farClipPlane);
        }
        internal float EyeOffset(bool left) { return (EyeSwap ? (left ? 1f : -1f) : (left ? -1f : 1f)) * IpD * .5f; }
        private static bool IsFinite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
