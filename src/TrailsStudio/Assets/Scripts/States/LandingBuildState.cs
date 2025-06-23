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
        LandingPositioner highlighter;

        public LandingBuildState(LandingPositioner highlighter)
        {
            this.highlighter = highlighter;
        }

        public LandingBuildState()
        { }

        protected override void OnEnter()
        {
            if (highlighter == null)
            {
                highlighter = BuildManager.Instance.StartLandingBuild();
            }

            CameraManager.Instance.AddOnTDCamBlendFinishedEvent((mixer, cam) =>
            {
                UIManager.Instance.ShowUI(UIManager.Instance.landingBuildUI);
                highlighter.enabled = true;
            });

            Vector3 rideDir = Line.Instance.GetCurrentRideDirection();
            Vector3 rideDirNormal = Vector3.Cross(rideDir, Vector3.up).normalized;
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
