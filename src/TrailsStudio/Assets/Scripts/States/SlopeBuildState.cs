using Managers;
using TerrainEditing;
using TerrainEditing.Slope;
using UnityEngine;

namespace States
{
    public class SlopeBuildState : State
    {
        private SlopePositioner highlighter;
       
        protected override void OnEnter()
        {
            highlighter = TerrainManager.Instance.StartSlopeBuild();
            CameraManager.Instance.AddOnTDCamBlendFinishedEvent((_, _) =>
            {
                StudioUIManager.Instance.ShowUI(StudioUIManager.Instance.slopeBuildUI);
                highlighter.enabled = true;
            });

            Vector3 lookDir = highlighter.transform.position - (highlighter.transform.position + 15f * Vector3.up);
            CameraManager.Instance.TopDownFollowHighlight(highlighter.gameObject, lookDir);
        }

        protected override void OnExit()
        {
            CameraManager.Instance.ClearOnTDCamBlendFinishedEvents();
            highlighter.enabled = false;
        }
    }
}
