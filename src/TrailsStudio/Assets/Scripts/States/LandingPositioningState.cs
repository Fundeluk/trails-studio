using System;
using Assets.Scripts.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.Builders;

namespace Assets.Scripts.States
{
    /// <summary>
    /// State for the landing positioning phase, always after the <ref>TakeoffBuildState</ref>.
    /// </summary>
    public class LandingPositioningState : State
    {
        LandingPositionHighlighter highlight;
        protected override void OnEnter()
        {
            highlight = BuildManager.Instance.StartLandingBuild();
            highlight.enabled = true;
            //CameraManager.Instance.GetTDCamEvents().BlendFinishedEvent.AddListener((mixer, cam) => highlight.enabled = true);
            CameraManager.Instance.TopDownFollowHighlight(highlight.gameObject);
            UIManager.Instance.ShowUI(UIManager.Instance.landingPositionUI);
        }

        protected override void OnExit()
        {
            //CameraManager.Instance.GetTDCamEvents().BlendFinishedEvent.RemoveAllListeners();
            highlight.enabled = false;
        }
    }
}
