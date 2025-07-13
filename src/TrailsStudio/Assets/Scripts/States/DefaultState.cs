using Assets.Scripts.Managers;
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
            CameraManager.Instance.SplineCamView();
            StudioUIManager.Instance.ToggleObstacleTooltips(true);
            StudioUIManager.Instance.ToggleESCMenu(true);
            StudioUIManager.Instance.ShowUI(StudioUIManager.Instance.sidebarMenuUI);
        }

        protected override void OnExit()
        {
            StudioUIManager.Instance.HideUI();
            StudioUIManager.Instance.ToggleObstacleTooltips(false);
            TerrainManager.Instance.HideSlopeInfo();
            StudioUIManager.Instance.ToggleESCMenu(false);
        }
    }
}
