using Assets.Scripts.Builders;
using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.States
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

           
            CameraManager.Instance.AddOnTDCamBlendFinishedEvent((mixer, cam) => {
                UIManager.Instance.ShowUI(UIManager.Instance.takeOffBuildUI);
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
