using UnityEngine;
using System.Collections;
using Assets.Scripts.Managers;
using System;

namespace Assets.Scripts.States
{
    // TODO use this just for positioning the start of the slope change. The length will be set by a value control sidebar, not by dragging.
    public class SlopePositioningState : State
	{       
        public static event Action<bool> SlopePositionHighlighterToggle;
        protected override void OnEnter()
        {
            CameraManager.Instance.TopDownFollowHighlight();
            UIManager.Instance.ShowUI(UIManager.Instance.slopePositionUI);
            SlopePositionHighlighterToggle?.Invoke(true);
        }

        protected override void OnExit()
        {
            SlopePositionHighlighterToggle?.Invoke(false);
        }
    }
}