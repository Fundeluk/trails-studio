using Managers;
using TerrainEditing;
using TerrainEditing.Slope;
using UnityEngine;

namespace States
{
    public class SlopeBuildState : State
    {
        private SlopePositioner positioner;
       
        protected override void OnEnter()
        {
            positioner = TerrainManager.Instance.StartSlopeBuild();
            CameraManager.Instance.AddOnTDCamBlendFinishedEvent((_, _) =>
            {
                StudioUIManager.Instance.ShowUI(StudioUIManager.Instance.slopeBuildUI);
                positioner.enabled = true;
            });
            
            CameraManager.Instance.TopDownFollowHighlight(positioner.gameObject, 15f, 0f);
        }

        protected override void OnExit()
        {
            CameraManager.Instance.ClearOnTDCamBlendFinishedEvents();
            positioner.enabled = false;
        }
    }
}
