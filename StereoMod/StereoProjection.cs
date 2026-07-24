using System;
using UnityEngine;

namespace UEBS2Stereo
{
    /// <summary>
    /// Parallel-axis off-axis stereo.
    /// Convergence = zero-disparity screen plane (closer = pop-out, farther = behind glass).
    /// Depth strength is a % of that screen distance — not a tiny real-world IPD.
    /// </summary>
    internal sealed class StereoProjection
    {
        internal const float DefaultSeparationRatio = 0.055f; // ~5.5% — strong but fusible
        internal const float MaxComfortRatio = 0.10f;         // above this, image often won't fuse
        internal const float MaxHardRatio = 0.14f;
        internal const float PopOutBias = 1.4f;               // put screen behind subject so subject pops
        internal const float MinUsefulIpd = 2f;

        internal float IpD { get; private set; } = 8f;
        internal float Convergence { get; private set; } = 140f;
        internal float SeparationRatio { get; private set; } = DefaultSeparationRatio;
        internal bool EyeSwap { get; private set; }
        internal bool ManualConvergence { get; private set; }

        internal float MaxIpd { get; set; } = 80f;
        internal float MinConvergence { get; private set; } = 5f;
        internal float MaxConvergence { get; private set; } = 800f;

        internal float DepthPercent => Convergence > 0.05f ? IpD / Convergence * 100f : 0f;
        internal bool OutOfComfort => SeparationRatio > MaxComfortRatio + 0.0001f;

        internal void UpdateConvergenceLimits(Camera source)
        {
            if (source == null) return;
            float near = Mathf.Max(source.nearClipPlane, 0.05f);
            float far = Mathf.Max(source.farClipPlane, near + 10f);
            MinConvergence = Mathf.Max(near * 6f, 8f);
            MaxConvergence = Mathf.Clamp(far * 0.45f, 120f, 1200f);
            // Keep current plane legal after camera changes.
            Convergence = Mathf.Clamp(Convergence, MinConvergence, MaxConvergence);
            ApplyRatioToIpd();
        }

        internal void SetSeparationRatio(float ratio, bool fromUser = false)
        {
            SeparationRatio = Mathf.Clamp(ratio, 0f, MaxHardRatio);
            ApplyRatioToIpd();
            if (fromUser) { /* ratio is the depth control */ }
        }

        internal void SetConvergence(float value, bool fromUser = false)
        {
            Convergence = Mathf.Clamp(value, MinConvergence, MaxConvergence);
            if (fromUser) ManualConvergence = true;
            ApplyRatioToIpd(); // depth always tracks screen plane
        }

        internal void SetIpdAbsolute(float value)
        {
            // Only used for config bootstrap / zero-IPD diagnostic.
            IpD = Mathf.Clamp(value, 0f, MaxIpd);
            if (Convergence > 0.05f)
                SeparationRatio = Mathf.Clamp(IpD / Convergence, 0f, MaxHardRatio);
            ApplyRatioToIpd();
        }

        internal void ClearManualConvergence() { ManualConvergence = false; }

        internal void ApplyRatioToIpd()
        {
            IpD = Mathf.Clamp(Convergence * SeparationRatio, 0f, MaxIpd);
        }

        internal void AdjustSeparationRatio(float amount)
        {
            SetSeparationRatio(SeparationRatio + amount, fromUser: true);
        }

        internal void AdjustConvergence(float amount)
        {
            SetConvergence(Convergence + amount, fromUser: true);
        }

        internal void SetZeroIpd()
        {
            SeparationRatio = 0f;
            ApplyRatioToIpd();
        }

        internal void RestoreComfortDefaults(Camera source)
        {
            UpdateConvergenceLimits(source);
            ManualConvergence = false;
            SeparationRatio = DefaultSeparationRatio;
            float plane = EstimateScreenPlane(source);
            SetConvergence(plane, fromUser: false);
            ClearManualConvergence();
        }

        internal void ToggleSwap() { EyeSwap = !EyeSwap; }

        /// <summary>
        /// Estimate subject distance, then bias the screen plane farther so the subject
        /// sits in negative parallax (comes through the glass).
        /// </summary>
        internal float EstimateScreenPlane(Camera source)
        {
            if (source == null) return Convergence;
            UpdateConvergenceLimits(source);

            float near = Mathf.Max(source.nearClipPlane, 0.05f);
            float subject = 100f;

            Vector3 origin = source.transform.position;
            Vector3 dir = source.transform.forward;
            if (dir.y < -0.02f)
            {
                float groundY = origin.y - 40f;
                float t = (groundY - origin.y) / dir.y;
                if (t > near * 4f) subject = t;
            }
            else
            {
                subject = Mathf.Clamp(140f, MinConvergence, MaxConvergence);
            }

            // Screen behind the subject => subject + nearer content can pop out.
            float plane = subject * PopOutBias;
            return Mathf.Clamp(plane, MinConvergence, MaxConvergence);
        }

        internal bool Valid(Camera source, float aspect)
        {
            return source != null && !source.orthographic && source.nearClipPlane > 0f
                && source.farClipPlane > source.nearClipPlane
                && Convergence > source.nearClipPlane + 0.01f
                && IsFinite(aspect) && aspect > 0f && IsFinite(IpD);
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

        internal float EyeOffset(bool left)
        {
            return (EyeSwap ? (left ? 1f : -1f) : (left ? -1f : 1f)) * IpD * 0.5f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
