using UnityEngine;
using System.Collections;
using Assets.Scripts.Managers;
using System;

namespace Assets.Scripts.States
{
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