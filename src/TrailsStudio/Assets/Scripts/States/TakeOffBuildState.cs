using Assets.Scripts.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Assets.Scripts.UI;
using Assets.Scripts.Builders;
using Unity.VisualScripting;

namespace Assets.Scripts.States
{
    /// <summary>
    /// This state corresponds to a phase where the user selects parameters of the takeoff.
    /// The takeoff's position is already set.
    /// </summary>
    public class TakeOffBuildState : State
    {
        private readonly TakeoffBuilder builder;

        public TakeOffBuildState(TakeoffBuilder builder)
        {
            this.builder = builder;
        }        

        protected override void OnEnter()
        {
            CameraManager.Instance.DetailedView(builder.GetCameraTarget());

            UIManager.Instance.ShowUI(UIManager.Instance.takeOffBuildUI);
        }        
    }
}
