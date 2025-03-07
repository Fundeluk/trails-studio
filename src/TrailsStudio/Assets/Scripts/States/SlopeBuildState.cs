using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.States
{
    public class SlopeBuildState : State
    {
        private SlopeChange slopeChange;
        private GameObject camTarget;

        public SlopeBuildState(SlopeChange change)
        {
            this.slopeChange = change;
        }       

        protected override void OnEnter()
        {
            camTarget = new("SlopeChange cam target");
            camTarget.transform.position = Vector3.Lerp(slopeChange.startPoint, slopeChange.endPoint, 0.5f);

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
