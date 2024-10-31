using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
            this.buildPosition = Line.Instance.line[^1].obstacle.transform.position;
        }

        protected override void OnEnter()
        {            
            CameraManager.Instance.SideView(buildPosition);
            UIManager.Instance.ShowUI(UIManager.Instance.takeOffBuildUI);
            // TODO start takeoff building process
        }

        protected override void OnExit()
        {
            // TODO stop building process
        }
    }
}
