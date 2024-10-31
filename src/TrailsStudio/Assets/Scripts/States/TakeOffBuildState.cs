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
        protected override void OnEnter()
        {
            if (GridHighlighter.Instance.desiredTakeOffPosition is null)
            {
                Debug.LogError("Takeoff position not set while changing state to takeoff build state");
            }

            CameraManager.Instance.SideView(GridHighlighter.Instance.desiredTakeOffPosition.Value);
            UIManager.Instance.ShowUI(UIManager.Instance.takeOffBuildUI);
            // TODO start takeoff building process
        }

        protected override void OnExit()
        {
            // TODO stop building process
        }
    }
}
