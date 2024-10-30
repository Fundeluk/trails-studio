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
    /// State for the takeoff positioning phase.
    /// This state corresponds to the phase where the user is selecting where the takeoff will be.
    /// </summary>
    public class TakeOffPositioningState : State
    {
        protected override void OnEnter()
        {
            stateController.takeOffPositionUI.SetActive(true);
            CameraManager.Instance.TopDownFollow(GridHighlighter.Instance.highlight);
        }

        protected override void OnExit()
        {
            stateController.takeOffPositionUI.SetActive(false);
        }
    }
}
