﻿using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Cinemachine;
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
            CameraManager.Instance.DefaultView();
            UIManager.Instance.ShowUI(UIManager.Instance.sidebarMenuUI);
        }

        protected override void OnExit()
        {
        }



    }
}
