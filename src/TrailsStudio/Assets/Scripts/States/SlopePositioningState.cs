using UnityEngine;
using System.Collections;
using Assets.Scripts.Managers;
using System;
using Assets.Scripts.Builders;
using Assets.Scripts.UI;

namespace Assets.Scripts.States
{
    public class SlopePositioningState : State
	{
        SlopePositioner highlighter;
        protected override void OnEnter()
        {
            highlighter = TerrainManager.Instance.StartSlopeBuild();
            CameraManager.Instance.AddOnTDCamBlendFinishedEvent((mixer, cam) => highlighter.enabled = true);
            UIManager.Instance.ShowUI(UIManager.Instance.slopePositionUI);
            UIManager.Instance.CurrentUI.GetComponent<SlopePositionUI>().Init(highlighter);

            Vector3 rideDir = Line.Instance.GetCurrentRideDirection();

            Vector3 rideDirNormal = Vector3.Cross(rideDir, Vector3.up).normalized;

            Vector3 lookDir = highlighter.transform.position - (highlighter.transform.position + 15f * Vector3.up);

            CameraManager.Instance.TopDownFollowHighlight(highlighter.gameObject, lookDir);
        }

        protected override void OnExit()
        {
            CameraManager.Instance.ClearOnTDCamBlendFinishedEvents();
            highlighter.enabled = false;
        }
    }
}