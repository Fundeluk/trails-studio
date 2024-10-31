using Assets.Scripts.Managers;
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
        public static event Action<bool> GridHighlighterToggle;
        protected override void OnEnter()
        {
            CameraManager.Instance.TopDownFollowHighlight();
            UIManager.Instance.ShowUI(UIManager.Instance.takeOffPositionUI);
            GridHighlighterToggle?.Invoke(true);
        }

        protected override void OnExit()
        {
            GridHighlighterToggle?.Invoke(false);
        }
    }
}
