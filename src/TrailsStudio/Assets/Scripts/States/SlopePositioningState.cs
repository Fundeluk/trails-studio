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
        SlopePositionHighlighter highlighter;
        protected override void OnEnter()
        {
            highlighter = TerrainManager.Instance.StartSlopeBuild();
            CameraManager.Instance.GetTDCamEvents().BlendFinishedEvent.AddListener((mixer, cam) => highlighter.enabled = true);
            UIManager.Instance.ShowUI(UIManager.Instance.slopePositionUI);
            UIManager.Instance.CurrentUI.GetComponent<SlopePositionUI>().Init(highlighter);
            CameraManager.Instance.TopDownFollowHighlight(highlighter.gameObject);
        }

        protected override void OnExit()
        {
            CameraManager.Instance.GetTDCamEvents().BlendFinishedEvent.RemoveAllListeners();
            highlighter.enabled = false;
        }
    }
}