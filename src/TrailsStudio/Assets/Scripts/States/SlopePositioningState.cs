using UnityEngine;
using System.Collections;
using Assets.Scripts.Managers;
using System;

namespace Assets.Scripts.States
{
    // TODO use this just for positioning the start of the slope change. The length will be set by a value control sidebar, not by dragging.
    public class SlopePositioningState : State
	{       
        protected override void OnEnter()
        {
            GameObject highlighter = TerrainManager.Instance.StartSlopeBuild();
            UIManager.Instance.ShowUI(UIManager.Instance.slopePositionUI);
            CameraManager.Instance.TopDownFollowHighlight(highlighter);
        }        
    }
}