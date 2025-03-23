using Assets.Scripts.Utilities;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    public class UIManager : Singleton<UIManager>
    {
        [Header("UI Elements")]
        public GameObject sidebarMenuUI;
        public GameObject takeOffPositionUI;
        public GameObject takeOffBuildUI;
        public GameObject landingPositionUI;
        public GameObject landingBuildUI;
        public GameObject deleteUI;
        public GameObject slopePositionUI;
        public GameObject slopeBuildUI;
        public GameObject isOnSlopeMessage;

        private GameObject currentUI;

        /// <summary>
        /// Hides the current UI and shows the given UI.
        /// </summary>
        /// <param name="ui"></param>
        public void ShowUI(GameObject ui)
        {
            if (currentUI != null)
            {
                currentUI.SetActive(false);
            }

            ui.SetActive(true);
            currentUI = ui;
        }

        public void ShowOnSlopeMessage()
        {
            isOnSlopeMessage.SetActive(true);
        }

        public void HideOnSlopeMessage()
        {
            isOnSlopeMessage.SetActive(false);
        }

        public Coroutine StartCoroutineFromInstance(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }

        public void StopCoroutineFromInstance(Coroutine routine)
        {            
            StopCoroutine(routine);
        }
    }
}