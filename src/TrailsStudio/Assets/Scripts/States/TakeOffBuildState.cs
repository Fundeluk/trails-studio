using Assets.Scripts.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Assets.Scripts.UI;
using Assets.Scripts.Builders;
using Unity.VisualScripting;

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
            this.buildPosition = buildPosition;
        }

        /// <summary>
        /// Makes the takeoff build state with the build position set to the last obstacle in the line.
        /// Used when returning from a state that follows this one, with the takeoff already built.
        /// </summary>
        public TakeOffBuildState()
        {
            if (Line.Instance.line[^1] is not TakeoffMeshGenerator.Takeoff)
            {
                Debug.LogError("The last element in the line is not a takeoff.");
            }

            buildPosition = Line.Instance.line[^1].GetTransform().position;
        }

        protected override void OnEnter()
        {
            // if returning from a state that follows this one, the takeoff is already built,
            // so we don't need to build it again
            TakeoffMeshGenerator.Takeoff takeoff;
            if (Line.Instance.line[^1] is not TakeoffMeshGenerator.Takeoff)
            {
                takeoff = Line.Instance.AddTakeOff(buildPosition);                
            }
            else
            {
                takeoff = Line.Instance.line[^1] as TakeoffMeshGenerator.Takeoff;
            }

            // make the camera target the middle of the takeoff
            CameraManager.Instance.DetailedView(takeoff);

            UIManager.Instance.ShowUI(UIManager.Instance.takeOffBuildUI);
        }

        protected override void OnExit()
        {
        }
    }
}
