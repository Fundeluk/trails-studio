using Managers;
using Obstacles;
using Obstacles.Landing;
using UnityEngine;

namespace States
{
    /// <summary>
    /// State for the landing positioning phase, always after the <ref>TakeoffBuildState</ref>.
    /// </summary>
    public class LandingBuildState : State
    {
        LandingPositioner positioner;

        public LandingBuildState(LandingPositioner positioner)
        {
            this.positioner = positioner;
        }

        public LandingBuildState()
        { }

        protected override void OnEnter()
        {
            if (positioner == null)
            {
                positioner = BuildManager.Instance.StartLandingBuild();
            }

            CameraManager.Instance.AddOnTDCamBlendFinishedEvent((_, _) =>
            {
                StudioUIManager.Instance.ShowUI(StudioUIManager.Instance.landingBuildUI);
                positioner.enabled = true;
            });
            
            CameraManager.Instance.TopDownFollowHighlight(positioner.gameObject, 15, 0f);
        }

        protected override void OnExit()
        {
            CameraManager.Instance.ClearOnTDCamBlendFinishedEvents();
            positioner.enabled = false;
        }
    }
}
