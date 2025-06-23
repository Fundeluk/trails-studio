using Assets.Scripts.Builders;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.States
{
    public class SlopeBuildState : State
    {
        private SlopePositioner highlighter;
       
        protected override void OnEnter()
        {
            highlighter = TerrainManager.Instance.StartSlopeBuild();
            CameraManager.Instance.AddOnTDCamBlendFinishedEvent((mixer, cam) =>
            {
                UIManager.Instance.ShowUI(UIManager.Instance.slopeBuildUI);
                highlighter.enabled = true;
            });

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
