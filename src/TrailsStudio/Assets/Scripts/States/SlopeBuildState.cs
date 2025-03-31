using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Assets.Scripts.Builders;

namespace Assets.Scripts.States
{
    public class SlopeBuildState : State
    {
        private readonly SlopeChangeBuilder slopeBuilder;

        public SlopeBuildState(SlopeChangeBuilder slopeBuilder)
        {
            this.slopeBuilder = slopeBuilder;
        }       

        protected override void OnEnter()
        {            
            CameraManager.Instance.DetailedView(slopeBuilder.GetCamTarget());

            UIManager.Instance.ShowUI(UIManager.Instance.slopeBuildUI);
        }
    }
}
