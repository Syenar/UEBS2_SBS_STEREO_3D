using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UEBS2Stereo
{
    internal sealed class EffectsBridge
    {
        private static readonly HashSet<string> Deny = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PostProcessingBehaviour", "HBAO", "AllPost", "UEBSTwoLighting", "PostLightBg", "RFX1_LegacyRenderDistortion"
        };

        // Temporal-history producers stay disabled for Phase 1 (Deferred).
        private static readonly HashSet<string> TemporalDeferred = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PostProcessingBehaviour", "HBAO"
        };

        private readonly StateSnapshot snapshot;
        private readonly List<Behaviour> disabled = new List<Behaviour>();
        private bool appliedPerEyeNote;

        internal EffectsBridge(StateSnapshot snapshot) { this.snapshot = snapshot; }

        internal void DisableForStereo(Camera source)
        {
            disabled.Clear();
            if (source == null) return;
            foreach (Behaviour b in source.GetComponentsInChildren<Behaviour>(true))
            {
                if (b == null) continue;
                Type type = b.GetType();
                bool deny = Deny.Contains(type.Name);
                bool onRenderImage = type.GetMethod("OnRenderImage", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) != null;
                if (!deny && !onRenderImage) continue;
                snapshot.Capture(b);
                if (b.enabled)
                {
                    b.enabled = false;
                    disabled.Add(b);
                }
            }
        }

        /// <summary>
        /// Non-temporal denied effects remain off on the center camera (eyes do not copy image effects).
        /// Ledger notes Deferred temporal producers; others are accepted as eye-RT path not hosting PPS.
        /// </summary>
        internal void TickPerEye(StereoRig rig)
        {
            if (appliedPerEyeNote || rig == null || rig.Source == null) return;
            appliedPerEyeNote = true;
            foreach (Behaviour b in disabled)
            {
                if (b == null) continue;
                string name = b.GetType().Name;
                if (TemporalDeferred.Contains(name))
                    Plugin.Instance?.ledgerSafeAdd(name, "deferred-temporal", "Shared temporal history; left disabled for Phase 1");
                else
                    Plugin.Instance?.ledgerSafeAdd(name, "effects-disabled-center", "Not copied to eye cameras; world stereo uses clean eye RTs");
            }
        }

        internal void Restore()
        {
            disabled.Clear();
            appliedPerEyeNote = false;
        }
    }
}
