using Assets.Scripts.Builders;
using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.States
{    
    /// <summary>
    /// State representing the takeoff positioning phase.
    /// </summary>
    public class TakeOffBuildState : State
    {
        TakeoffPositionHighlighter highlighter;

        public TakeOffBuildState(TakeoffPositionHighlighter highlighter)
        {
            this.highlighter = highlighter;
        }

        public TakeOffBuildState()
        { }

        protected override void OnEnter()
        {
            if (highlighter == null)
            {
                highlighter = BuildManager.Instance.StartTakeoffBuild();
            }

           
            CameraManager.Instance.AddOnTDCamBlendFinishedEvent((mixer, cam) => {
                highlighter.enabled = true;
                UIManager.Instance.ShowUI(UIManager.Instance.takeOffBuildUI);
            });

            CameraManager.Instance.TopDownFollowHighlight(highlighter.gameObject);
        }

        protected override void OnExit()
        {
            CameraManager.Instance.ClearOnTDCamBlendFinishedEvents();
            highlighter.enabled = false;
        }
    }
}
