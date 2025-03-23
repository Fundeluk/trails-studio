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
        private GameObject camTarget;

        public SlopeBuildState(SlopeChangeBuilder slopeBuilder)
        {
            this.slopeBuilder = slopeBuilder;
        }       

        protected override void OnEnter()
        {
            camTarget = new("SlopeChange cam target");
            camTarget.transform.position = slopeBuilder.start;

            // make the camera target the middle of the takeoff
            CameraManager.Instance.DetailedView(camTarget);

            UIManager.Instance.ShowUI(UIManager.Instance.slopeBuildUI);
        }

        protected override void OnExit()
        {
            GameObject.Destroy(camTarget);
        }
    }
}
