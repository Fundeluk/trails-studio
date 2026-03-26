using LineSystem;
using Managers;
using Obstacles;
using Obstacles.TakeOff;
using UnityEngine;

namespace States
{    
    /// <summary>
    /// State representing the takeoff positioning phase.
    /// </summary>
    public class TakeOffBuildState : State
    {
        TakeoffPositioner highlighter;

        public TakeOffBuildState(TakeoffPositioner highlighter)
        {
            this.highlighter = highlighter;
        }

        public TakeOffBuildState()
        { }

    
        protected override void OnEnter()
        {

            if (highlighter == null)
            {
                highlighter = BuildManager.Instance.StartTakeoffBuild();
            }

           
            CameraManager.Instance.AddOnTDCamBlendFinishedEvent((_, _) => {
                StudioUIManager.Instance.ShowUI(StudioUIManager.Instance.takeOffBuildUI);
                highlighter.enabled = true;
            });


            Vector3 rideDir = Line.Instance.GetCurrentRideDirection();

            Vector3 rideDirNormal = Vector3.Cross(rideDir, Vector3.up).normalized;

            Vector3 lookDir = highlighter.transform.position - (highlighter.transform.position + 15f * Vector3.up) + rideDirNormal * 10f;

            CameraManager.Instance.TopDownFollowHighlight(highlighter.gameObject, lookDir);
        }

        protected override void OnExit()
        {
            CameraManager.Instance.ClearOnTDCamBlendFinishedEvents();
            highlighter.enabled = false;
        }
    }
}
