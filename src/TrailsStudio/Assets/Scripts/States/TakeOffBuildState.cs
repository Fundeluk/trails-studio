using Assets.Scripts.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Assets.Scripts.UI;

namespace Assets.Scripts.States
{
    /// <summary>
    /// This state corresponds to a phase where the user selects parameters of the takeoff.
    /// The takeoff's position is already set.
    /// </summary>
    public class TakeOffBuildState : State
    {
        private Vector3 buildPosition;

        public TakeOffBuildState(Vector3 buildPosition)
        {
            buildPosition.y = 0;
            this.buildPosition = buildPosition;
        }

        /// <summary>
        /// Makes the takeoff build state with the build position set to the last obstacle in the line.
        /// Used when returning from a state that follows this one, with the takeoff already built.
        /// </summary>
        public TakeOffBuildState()
        {
            buildPosition = Line.Instance.line[^1].GetTransform().position;
        }

        protected override void OnEnter()
        {
            TakeoffMeshGenerator.Takeoff takeoff = Line.Instance.AddTakeOff(buildPosition);

            // make the camera target the middle of the takeoff
            GameObject cameraTarget = takeoff.GetCameraTarget();

            CameraManager.Instance.SideView(takeoff);
            UIManager.Instance.ShowUI(UIManager.Instance.takeOffBuildUI);
            UIManager.Instance.takeOffBuildUI.GetComponent<TakeOffBuildUI>().SetTakeoffElement(takeoff);
        }

        protected override void OnExit()
        {
            // TODO stop building process
        }
    }
}
