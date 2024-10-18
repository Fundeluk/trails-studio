using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.States
{
    /// <summary>
    /// State for the takeoff building phase.
    /// </summary>
    public class TakeoffState : State
    {
        protected override void OnEnter()
        {
            stateController.buildPhaseUI.SetActive(true);
            CameraManager.Instance.TopDownView();
            // TODO start takeoff process
        }

        protected override void OnExit()
        {
            // TODO stop building process
            stateController.buildPhaseUI.SetActive(false);
        }
    }
}
