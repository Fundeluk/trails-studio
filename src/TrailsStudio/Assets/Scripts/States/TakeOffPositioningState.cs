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
    public class TakeOffPositioningState : State
    {
        TakeoffPositionHighlighter highlighter;
        protected override void OnEnter()
        {
            highlighter = BuildManager.Instance.StartTakeoffBuild();
            CameraManager.Instance.GetTDCamEvents().BlendFinishedEvent.AddListener((mixer, cam) => highlighter.enabled = true);
            UIManager.Instance.ShowUI(UIManager.Instance.takeOffPositionUI);
            CameraManager.Instance.TopDownFollowHighlight(highlighter.gameObject);
        }

        protected override void OnExit()
        {
            CameraManager.Instance.GetTDCamEvents().BlendFinishedEvent.RemoveAllListeners();
            highlighter.enabled = false;
        }
    }
}
