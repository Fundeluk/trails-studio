using System;
using Assets.Scripts.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.States
{
    /// <summary>
    /// State for the landing positioning phase, always after the <ref>TakeoffBuildState</ref>.
    /// </summary>
    public class LandingPositioningState : State
    {
        public static event Action<bool> LandingHighlighterToggle;
        protected override void OnEnter()
        {
            CameraManager.Instance.TopDownFollowHighlight();
            UIManager.Instance.ShowUI(UIManager.Instance.landingPositionUI);
            LandingHighlighterToggle?.Invoke(true);
        }
        protected override void OnExit()
        {
            LandingHighlighterToggle?.Invoke(false);
        }
    }
}
