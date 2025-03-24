using Assets.Scripts.Builders.TakeOff;
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
    /// State representing the takeoff positioning phase.
    /// </summary>
    public class TakeOffPositioningState : State
    {        
        protected override void OnEnter()
        {
            TakeoffBuilder builder = BuildManager.Instance.StartTakeoffBuild();
            CameraManager.Instance.TopDownFollowHighlight(builder.gameObject);
            UIManager.Instance.ShowUI(UIManager.Instance.takeOffPositionUI);
        }        
    }
}
