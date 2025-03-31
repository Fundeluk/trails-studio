using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.Managers;
using UnityEngine;
using Assets.Scripts.Builders;
using Assets.Scripts.UI;

namespace Assets.Scripts.States
{
    public class LandingBuildState : State
    {
        private readonly LandingBuilder builder;

        public LandingBuildState(LandingBuilder builder)
        {
            if (Line.Instance.GetLastLineElement() is not Takeoff)
            {
                Debug.LogError("The last element in the line is not a takeoff.");
            }

            this.builder = builder;
        }

        protected override void OnEnter()
        {
            CameraManager.Instance.DetailedView(builder.GetCameraTarget());

            UIManager.Instance.ShowUI(UIManager.Instance.landingBuildUI);
        }        
    }
}
