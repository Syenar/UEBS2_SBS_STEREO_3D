using UnityEngine;

namespace UEBS2Stereo
{
    internal sealed class StereoHotkeys
    {
        private readonly Plugin plugin;
        private readonly StereoProjection projection;
        private readonly StereoInput input;
        internal StereoHotkeys(Plugin plugin,StereoProjection projection,StereoInput input) { this.plugin=plugin; this.projection=projection; this.input=input; }
        internal void Update()
        {
            if(input.CustomInputCaptureActive) return;
            if(Input.GetKeyDown(KeyCode.F8)) { if(plugin.stereoEngaged) plugin.DisableStereoAndRestore(); else plugin.EnableStereo(); }
            if(!plugin.stereoEngaged) return;
            if(Input.GetKeyDown(KeyCode.F9)) plugin.ExitProofUiHide();
            if(Input.GetKeyDown(KeyCode.LeftBracket)) projection.AdjustIpd(-.002f);
            if(Input.GetKeyDown(KeyCode.RightBracket)) projection.AdjustIpd(.002f);
            if(Input.GetKeyDown(KeyCode.Semicolon)) projection.AdjustConvergence(-.5f);
            if(Input.GetKeyDown(KeyCode.Quote)) projection.AdjustConvergence(.5f);
            if(Input.GetKeyDown(KeyCode.F7)) projection.ToggleSwap();
            if(Input.GetKeyDown(KeyCode.F6)) projection.SetZeroIpd();
        }
    }
}
