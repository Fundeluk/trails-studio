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
        private TakeoffPositioner positioner;

        public TakeOffBuildState(TakeoffPositioner positioner)
        {
            this.positioner = positioner;
        }

        public TakeOffBuildState()
        { }

    
        protected override void OnEnter()
        {
            if (positioner == null)
            {
                positioner = BuildManager.Instance.StartTakeoffBuild();
            }
           
            CameraManager.Instance.AddOnTDCamBlendFinishedEvent((_, _) => {
                StudioUIManager.Instance.ShowUI(StudioUIManager.Instance.takeOffBuildUI);
                positioner.enabled = true;
            });
            
            CameraManager.Instance.TopDownFollowHighlight(positioner.gameObject, 15f, 10f);
        }

        protected override void OnExit()
        {
            CameraManager.Instance.ClearOnTDCamBlendFinishedEvents();
            positioner.enabled = false;
        }
    }
}
