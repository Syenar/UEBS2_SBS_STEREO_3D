using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UEBS2Stereo
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.uebs2.stereo";
        public const string Name = "UEBS2 Half-SBS Stereo";
        public const string Version = "1.1.6";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }
        internal bool stereoEngaged;

        private Harmony harmony;
        private StateSnapshot snapshot;
        private StereoProjection projection;
        internal StereoRig rig;
        private CameraSourceBinder binder;
        private StereoCompositor compositor;
        private EffectsBridge effects;
        private UiCapture ui;
        private ImguiCapture imgui;
        internal StereoInput input;
        private StereoHotkeys hotkeys;
        private AcceptanceLedger ledger;
        private float resolutionScale = 0.5f;
        private bool imguiReady;
        private BepInEx.Configuration.ConfigEntry<float> cfgIpd;
        private BepInEx.Configuration.ConfigEntry<float> cfgConvergence;
        private BepInEx.Configuration.ConfigEntry<float> cfgMaxIpd;
        private BepInEx.Configuration.ConfigEntry<bool> cfgAutoScreen;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            DontDestroyOnLoad(gameObject);
            harmony = new Harmony(Guid);
            snapshot = new StateSnapshot();
            projection = new StereoProjection();
            ledger = new AcceptanceLedger();
            float requestedScale = Config.Bind("Stereo", "ResolutionScale", 0.5f,
                "Eye/UI render scale vs screen. Lower = smoother (0.5 recommended).").Value;
            bool allowHighScale = Config.Bind("Stereo", "AllowHighResolutionScale", false,
                "Allow ResolutionScale above 0.5. Expensive in UEBS2; enable only after stable testing.").Value;
            resolutionScale = Mathf.Clamp(requestedScale, 0.35f, allowHighScale ? 1f : 0.5f);

            cfgMaxIpd = Config.Bind("Stereo", "MaxEyeSeparation", 80f,
                "Hard cap on eye separation (world units). Comfort depth is a percent of screen distance.");
            cfgIpd = Config.Bind("Stereo", "EyeSeparation", 8f,
                "Bootstrap eye separation. Live tuning uses depth %% (F3/F4); IPD tracks screen plane.");
            cfgConvergence = Config.Bind("Stereo", "Convergence", 140f,
                "Bootstrap screen-plane distance. On engage, auto-place biases this behind the subject for pop-out.");
            cfgAutoScreen = Config.Bind("Stereo", "AutoScreenPlane", true,
                "Auto-place screen plane behind the subject on engage/F10 so near content can pop out.");

            // Migrate broken / runaway / micro configs.
            MigrateLegacyDepthConfig();
            ApplyStereoConfig();

            rig = new StereoRig(snapshot, projection);
            compositor = new StereoCompositor(rig, projection) { ResolutionScale = resolutionScale };
            binder = new CameraSourceBinder(rig, compositor, ledger);
            effects = new EffectsBridge(snapshot);
            ui = new UiCapture(snapshot, ledger);
            input = new StereoInput(this, harmony);
            imgui = new ImguiCapture(harmony, ledger);
            hotkeys = new StereoHotkeys(this, projection, input);

            // Warm material only (no cameras) so first F8 is fast.
            compositor.EnsurePresenter();
            compositor.DisposePresenter();

            SceneManager.sceneLoaded += OnSceneLoaded;
            Log.LogInfo(Name + " v" + Version + " loaded. F8 toggles Half-SBS. ResolutionScale="
                + resolutionScale + " IPD=" + projection.IpD + " Convergence=" + projection.Convergence);
        }

        private void MigrateLegacyDepthConfig()
        {
            bool weakIpd = cfgIpd != null && cfgIpd.Value < StereoProjection.MinUsefulIpd;
            bool weakMax = cfgMaxIpd != null && cfgMaxIpd.Value < 10f;
            bool runawayMax = cfgMaxIpd != null && cfgMaxIpd.Value > 100f;
            bool runawayScreen = cfgConvergence != null && cfgConvergence.Value > 900f;
            if (!weakIpd && !weakMax && !runawayMax && !runawayScreen) return;

            float plane = 140f;
            if (cfgConvergence != null)
                plane = Mathf.Clamp(cfgConvergence.Value, 40f, 400f);
            float ipd = Mathf.Clamp(plane * StereoProjection.DefaultSeparationRatio, StereoProjection.MinUsefulIpd, 40f);
            if (cfgMaxIpd != null) cfgMaxIpd.Value = 80f;
            if (cfgIpd != null) cfgIpd.Value = ipd;
            if (cfgConvergence != null) cfgConvergence.Value = plane;
            Log.LogWarning(string.Format(
                "Reset depth config to comfort defaults: IPD={0:0.##} Screen={1:0} Max={2:0}",
                ipd, plane, 80f));
        }

        private void ApplyStereoConfig()
        {
            if (cfgMaxIpd != null) projection.MaxIpd = Mathf.Clamp(cfgMaxIpd.Value, 10f, 80f);
            float plane = cfgConvergence != null ? cfgConvergence.Value : 140f;
            projection.SetConvergence(Mathf.Clamp(plane, 20f, 800f), fromUser: false);
            float ipd = cfgIpd != null ? cfgIpd.Value : 8f;
            float ratio = projection.Convergence > 0.05f
                ? Mathf.Clamp(ipd / projection.Convergence, 0.02f, StereoProjection.MaxComfortRatio)
                : StereoProjection.DefaultSeparationRatio;
            projection.SetSeparationRatio(ratio, fromUser: false);
            projection.ClearManualConvergence();
        }

        /// <summary>
        /// Place screen plane behind the subject so nearer content can come through the glass.
        /// </summary>
        internal void AutoPlaceScreenPlane(bool force = false)
        {
            if (!stereoEngaged || rig == null || rig.Source == null) return;
            if (!force && projection.ManualConvergence) return;
            if (cfgAutoScreen != null && !cfgAutoScreen.Value && !force) return;

            float plane = projection.EstimateScreenPlane(rig.Source);
            projection.ClearManualConvergence();
            projection.SetConvergence(plane, fromUser: false);
            if (force)
                hotkeys?.RefreshHud("AUTO POP-OUT plane — subject comes forward, backdrop stays behind");
            else
                hotkeys?.RefreshHud(string.Empty);
        }

        /// <summary>F5 — recover from runaway / unfusable tuning.</summary>
        internal void ResetComfortStereo()
        {
            if (!stereoEngaged) return;
            projection.RestoreComfortDefaults(rig != null ? rig.Source : null);
            if (rig != null && rig.Source != null && (cfgAutoScreen == null || cfgAutoScreen.Value))
            {
                float plane = projection.EstimateScreenPlane(rig.Source);
                projection.SetConvergence(plane, fromUser: false);
                projection.ClearManualConvergence();
            }
            hotkeys?.RefreshHud("RESET to comfort depth (~5.5%) + pop-out screen plane");
            Log.LogInfo(string.Format(
                "Comfort reset: depth={0:0.0}% screen={1:0} ipd={2:0.0}",
                projection.DepthPercent, projection.Convergence, projection.IpD));
        }

        internal void ledgerSafeAdd(string surface, string classification, string detail)
        {
            ledger?.Add(surface, classification, detail);
        }

        private void Update()
        {
            hotkeys.Update();
            if (!stereoEngaged) return;

            input.TickLatch();
            binder.UpdateBinding();
            ui.Update();
        }

        private void LateUpdate()
        {
            if (!stereoEngaged) return;
            Cursor.visible = false;
            ui.LateUpdateTick();
            if (rig != null && rig.Source != null)
                projection.UpdateConvergenceLimits(rig.Source);
            rig.LateSync();
            compositor.LateSync();
            if (!rig.HasValidSource) EnterDualMonoSbs("No valid perspective gameplay source.");
        }

        private void OnGUI()
        {
            if (hotkeys == null) return;
            hotkeys.HandleGuiEvent();
            if (!hotkeys.ShowHud) return;

            float pad = 16f;
            float w = Mathf.Min(Screen.width - pad * 2f, 1100f);
            float h = 96f;
            Rect box = new Rect(pad, pad, w, h);

            // Large high-contrast HUD so depth numbers are readable on a projector.
            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle title = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Screen.height / 28, 28, 48),
                fontStyle = FontStyle.Bold,
                normal = { textColor = hotkeys.HudText.Contains("!!") ? new Color(1f, 0.35f, 0.35f) : Color.white }
            };
            GUIStyle sub = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Screen.height / 42, 18, 28),
                normal = { textColor = new Color(1f, 0.92f, 0.35f) }
            };

            GUI.Label(new Rect(box.x + 18f, box.y + 10f, box.width - 36f, title.fontSize + 8f), hotkeys.HudText, title);
            GUI.Label(new Rect(box.x + 18f, box.y + 10f + title.fontSize + 12f, box.width - 36f, sub.fontSize + 8f), hotkeys.HudSubText, sub);
            GUI.color = prev;
        }

        internal void EnableStereo()
        {
            if (stereoEngaged) return;
            try
            {
                ApplyStereoConfig();
                compositor.ResolutionScale = resolutionScale;
                if (!compositor.EnsurePresenter())
                {
                    Log.LogError("Could not create SBS presenter; stereo was not engaged.");
                    return;
                }

                snapshot.CaptureCursor();
                input.Enable();
                if (!imguiReady)
                {
                    imgui.Enable();
                    imguiReady = true;
                }
                ui.Enable(compositor);

                bool proofMode = Config.Bind("Debug", "FirstProofUiHide", false,
                    "Proof-only: hide screen-space UI. F9 exits hide. Default false.").Value;
                if (proofMode) ui.EnterProofUiHide();

                binder.FindAndBind();
                effects.DisableForStereo(binder.SourceCamera);

                stereoEngaged = true;
                if (!rig.HasValidSource) EnterDualMonoSbs("Waiting for a supported camera.");
                else
                {
                    compositor.SetDualMono(false);
                    rig.SetDualMono(false);
                }

                AutoPlaceScreenPlane(force: true);

                ledger.Add("Plugin", "engaged", "F8 Half-SBS");
                Log.LogInfo("Half-SBS stereo engaged. depth%=" + projection.DepthPercent.ToString("0.0")
                    + " screen=" + projection.Convergence.ToString("0")
                    + " (F3/F4 depth%, F1/F2 pop-out, F5 reset, F10 auto)");
            }
            catch (System.Exception ex)
            {
                Log.LogError("Stereo setup failed: " + ex);
                DisableStereoAndRestore();
            }
        }

        internal void EnterDualMonoSbs(string reason)
        {
            if (!stereoEngaged) return;
            compositor.SetDualMono(true);
            rig.SetDualMono(true);
            binder.EnsureFallbackCamera();
            if (rig.Source == null && binder.FallbackCamera != null)
            {
                Vector2Int size = compositor.EyeSize;
                rig.Bind(binder.FallbackCamera, size.x, size.y);
            }
            ledger.Add("Camera", "dual-mono", reason);
        }

        internal void ExitProofUiHide()
        {
            if (!stereoEngaged) return;
            ui.ExitProofUiHide();
        }

        internal void DisableStereoAndRestore()
        {
            if (!stereoEngaged && !snapshot.HasState) return;
            stereoEngaged = false;
            try
            {
                input.Disable();
                imgui.Disable();
                ui.Disable();
                compositor.DisposePresenter();
                rig.Dispose();
                effects.Restore();
                snapshot.RestoreAll();
                binder.Clear();
                ledger.Add("Plugin", "restored", "native mono");
                Log.LogInfo("Half-SBS stereo restored to mono.");
            }
            catch (System.Exception ex)
            {
                Log.LogError("Stereo restore failed: " + ex);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!stereoEngaged) return;
            rig.DetachSource();
            binder.Invalidate();
            EnterDualMonoSbs("Scene transition");
            binder.FindAndBind();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            DisableStereoAndRestore();
            compositor?.Dispose();
            harmony?.UnpatchSelf();
            if (Instance == this) Instance = null;
        }
    }
}
