using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.States
{
    /// <summary>
    /// Default state of the application.
    /// Used for default view and looking around the scene.
    /// </summary>
    public class DefaultState : State
    {

        protected override void OnEnter()
        {
            stateController.sidebarMenuUI.SetActive(true);
            CameraManager.Instance.DefaultView();
        }

        protected override void OnExit()
        {
            stateController.sidebarMenuUI.SetActive(false);
        }



    }
}
