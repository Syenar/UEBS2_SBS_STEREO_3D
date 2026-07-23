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
        public const string Version = "1.1.1";

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
            Log.LogInfo(Name + " v" + Version + " loaded. F8 toggles Half-SBS. ResolutionScale=" + resolutionScale);
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
            rig.LateSync();
            compositor.LateSync();
            if (!rig.HasValidSource) EnterDualMonoSbs("No valid perspective gameplay source.");
        }

        internal void EnableStereo()
        {
            if (stereoEngaged) return;
            try
            {
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

                ledger.Add("Plugin", "engaged", "F8 Half-SBS");
                Log.LogInfo("Half-SBS stereo engaged.");
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
