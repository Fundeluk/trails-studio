using System;
using Assets.Scripts.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.Builders;
using UnityEngine;

namespace Assets.Scripts.States
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

            CameraManager.Instance.AddOnTDCamBlendFinishedEvent((mixer, cam) =>
            {
                UIManager.Instance.ShowUI(UIManager.Instance.landingBuildUI);
                positioner.enabled = true;
            });

            Vector3 rideDir = Line.Instance.GetCurrentRideDirection();
            Vector3 rideDirNormal = Vector3.Cross(rideDir, Vector3.up).normalized;
            Vector3 lookDir = positioner.transform.position - (positioner.transform.position + 15f * Vector3.up);

            CameraManager.Instance.TopDownFollowHighlight(positioner.gameObject, lookDir);
        }

        protected override void OnExit()
        {
            CameraManager.Instance.ClearOnTDCamBlendFinishedEvents();
            positioner.enabled = false;
        }
    }
}
