using UnityEngine;

namespace UEBS2Stereo
{
    /// <summary>
    /// F3/F4 = depth strength (% of screen). F1/F2 = screen/pop-out plane.
    /// Depth IPD always tracks screen * ratio so runaway screen distance can't zero out 3D.
    /// </summary>
    internal sealed class StereoHotkeys
    {
        private readonly Plugin plugin;
        private readonly StereoProjection projection;
        private readonly StereoInput input;

        private float holdIpdDown;
        private float holdIpdUp;
        private float holdPlaneCloser;
        private float holdPlaneFarther;

        private const float HoldDelay = 0.22f;
        private const float HoldRepeat = 0.045f;
        private const float RatioStep = 0.005f; // 0.5% of screen per tick

        internal bool ShowHud => plugin != null && plugin.stereoEngaged;
        internal string HudText { get; private set; } = string.Empty;
        internal string HudSubText { get; private set; } = string.Empty;

        internal StereoHotkeys(Plugin plugin, StereoProjection projection, StereoInput input)
        {
            this.plugin = plugin;
            this.projection = projection;
            this.input = input;
            RefreshHud(string.Empty);
        }

        private float PlaneStep
        {
            get
            {
                // Bounded steps — old % scaling ran away to 10000 and broke fusion.
                float span = Mathf.Max(40f, projection.MaxConvergence - projection.MinConvergence);
                return Mathf.Clamp(span * 0.04f, 10f, 35f);
            }
        }

        internal void Update()
        {
            if (input != null && input.CustomInputCaptureActive) return;

            if (Down(KeyCode.F8))
            {
                if (plugin.stereoEngaged) plugin.DisableStereoAndRestore();
                else plugin.EnableStereo();
                return;
            }

            if (!plugin.stereoEngaged) return;

            if (Down(KeyCode.F9)) plugin.ExitProofUiHide();

            if (Down(KeyCode.F5))
            {
                plugin.ResetComfortStereo();
                return;
            }

            if (Down(KeyCode.F7))
            {
                projection.ToggleSwap();
                RefreshHud("EYE SWAP " + (projection.EyeSwap ? "ON" : "off"));
                return;
            }

            if (Down(KeyCode.F6))
            {
                projection.SetZeroIpd();
                RefreshHud("FLAT check (0% depth) — F4 or F5 to restore");
                return;
            }

            if (Down(KeyCode.F10))
            {
                plugin.AutoPlaceScreenPlane(force: true);
                return;
            }

            // Depth strength as % of screen (always linked).
            if (Repeat(ref holdIpdDown, KeyCode.F3, KeyCode.Home, KeyCode.KeypadDivide))
            {
                projection.AdjustSeparationRatio(-RatioStep);
                RefreshHud(projection.OutOfComfort ? "WEAKER — still high" : "WEAKER DEPTH (F3)");
            }

            if (Repeat(ref holdIpdUp, KeyCode.F4, KeyCode.End, KeyCode.KeypadMultiply))
            {
                projection.AdjustSeparationRatio(RatioStep);
                RefreshHud(projection.OutOfComfort
                    ? "STRONG — may not fuse / hurt. F5 reset"
                    : "STRONGER DEPTH (F4)");
            }

            // Screen plane / pop-out. IPD tracks automatically via ratio.
            if (Repeat(ref holdPlaneCloser, KeyCode.F1, KeyCode.PageDown, KeyCode.KeypadMinus))
            {
                projection.AdjustConvergence(-PlaneStep);
                RefreshHud("SCREEN CLOSER — more behind glass (F1)");
            }

            if (Repeat(ref holdPlaneFarther, KeyCode.F2, KeyCode.PageUp, KeyCode.KeypadPlus))
            {
                projection.AdjustConvergence(PlaneStep);
                RefreshHud("SCREEN FARTHER — more POP-OUT (F2)");
            }
        }

        internal void HandleGuiEvent()
        {
            if (!plugin.stereoEngaged) return;
            if (input != null && input.CustomInputCaptureActive) return;

            Event e = Event.current;
            if (e == null || e.type != EventType.KeyDown) return;

            KeyCode code = e.keyCode;
            char ch = e.character;

            if (code == KeyCode.LeftBracket || ch == '[')
            {
                projection.AdjustSeparationRatio(-RatioStep);
                RefreshHud("WEAKER DEPTH");
                e.Use();
            }
            else if (code == KeyCode.RightBracket || ch == ']')
            {
                projection.AdjustSeparationRatio(RatioStep);
                RefreshHud("STRONGER DEPTH");
                e.Use();
            }
            else if (code == KeyCode.Minus || ch == '-' || ch == '_' || code == KeyCode.Semicolon || ch == ';')
            {
                projection.AdjustConvergence(-PlaneStep);
                RefreshHud("SCREEN CLOSER");
                e.Use();
            }
            else if (code == KeyCode.Equals || ch == '=' || ch == '+' || code == KeyCode.Quote || ch == '\'' || ch == '"')
            {
                projection.AdjustConvergence(PlaneStep);
                RefreshHud("SCREEN FARTHER / POP-OUT");
                e.Use();
            }
        }

        private static bool Down(KeyCode key)
        {
            return key != KeyCode.None && Input.GetKeyDown(key);
        }

        private static bool Repeat(ref float timer, params KeyCode[] keys)
        {
            bool down = false;
            bool held = false;
            for (int i = 0; i < keys.Length; i++)
            {
                KeyCode key = keys[i];
                if (key == KeyCode.None) continue;
                if (Input.GetKeyDown(key)) down = true;
                if (Input.GetKey(key)) held = true;
            }

            if (down)
            {
                timer = HoldDelay;
                return true;
            }

            if (!held) return false;

            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return false;
            timer = HoldRepeat;
            return true;
        }

        internal void RefreshHud(string hint)
        {
            string warn = projection.OutOfComfort ? "  !! HIGH — may break fusion (F5)" : string.Empty;
            HudText = string.Format(
                "3D  DEPTH {0:0.0}%   SCREEN {1:0}   IPD {2:0.0}{3}",
                projection.DepthPercent,
                projection.Convergence,
                projection.IpD,
                warn);

            HudSubText = string.IsNullOrEmpty(hint)
                ? "F3/F4 depth%   F1/F2 pop-out plane   F5 reset   F10 auto   F6 flat"
                : hint;

            if (!string.IsNullOrEmpty(hint))
                Plugin.Log.LogInfo("Stereo tune: " + HudText + " | " + HudSubText);
        }
    }
}
