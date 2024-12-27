using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.Managers;
using UnityEngine;
using Assets.Scripts.Builders;
using Assets.Scripts.UI;

namespace Assets.Scripts.States
{
    public class LandingBuildState : State
    {
        private Vector3 buildPosition;
        private Vector3 rideDirection;

        public LandingBuildState(Vector3 buildPosition, Vector3 rideDirection)
        {
            if (Line.Instance.line[^1] is not TakeoffMeshGenerator.Takeoff)
            {
                Debug.LogError("The last element in the line is not a takeoff.");
            }

            this.buildPosition = buildPosition;
            this.rideDirection = rideDirection;
        }

        protected override void OnEnter()
        {
            LandingMeshGenerator.Landing landing = Line.Instance.AddLanding(buildPosition, rideDirection);

            CameraManager.Instance.SideView(landing);

            UIManager.Instance.ShowUI(UIManager.Instance.landingBuildUI);
            UIManager.Instance.landingBuildUI.GetComponent<LandingBuildUI>().SetLandingElement(landing);
        }

        protected override void OnExit()
        {

        }
    }
}
