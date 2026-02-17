using Assets.Scripts.UI;
using Assets.Scripts.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
namespace Assets.Scripts.Managers
{

    public class StudioUIManager : MessagingSystem<StudioUIManager>
    {
        [Header("UI GameObjects")]
        public GameObject sidebarMenuUI;
        public GameObject takeOffBuildUI;
        public GameObject landingBuildUI;
        public GameObject slopeBuildUI;
        public GameObject deleteUI;
        public GameObject obstacleTooltip;
        public GameObject slopeInfoPrefab;
        public GameObject escMenuPrefab;

        private EscMenuUI escMenu = null;

        private InputAction escapeAction;

        public static bool IsPointerOverUI { get; private set; } = false;

        protected virtual void Update()
        {
            IsPointerOverUI = EventSystem.current.IsPointerOverGameObject();
        }

        public GameObject CurrentUI { get; private set; }

        private void OnEnable()
        {
            escapeAction = InputSystem.actions.FindAction("Cancel");
        }
        

        /// <summary>
        /// Hides the current UI and shows the given UI.
        /// </summary>
        public void ShowUI(GameObject ui)
        {
            if (CurrentUI != null)
            {
                CurrentUI.SetActive(false);
            }

            ui.SetActive(true);
            CurrentUI = ui;
        }

        public void HideUI()
        {
            HideESCMenu();
            if (CurrentUI != null)
            {
                CurrentUI.SetActive(false);
                CurrentUI = null;
            }
        }

        /// <summary>
        /// Toggles whether the ESC menu can be opened with the ESC key.
        /// </summary>
        /// <param name="enable">If true, the menu is available.</param>
        public void ToggleESCMenu(bool enable)
        {
            if (enable)
            {
                escapeAction.performed += OnEscapePressed;
            }
            else
            {
                HideESCMenu();
                escapeAction.performed -= OnEscapePressed;
            }
        }

        public void HideESCMenu()
        {
            if (escMenu != null)
            {
                Destroy(escMenu.gameObject);
                escMenu = null;
            }
        }
        

        private void OnEscapePressed(InputAction.CallbackContext ctx)
        {
            if (escMenu == null)
            {
                escMenu = Instantiate(escMenuPrefab, transform).GetComponent<EscMenuUI>();
            }
            else
            {
                Destroy(escMenu.gameObject);
                escMenu = null;
            }
        }

        public void ToggleObstacleTooltips(bool enable)
        {
            if (enable)
            {
                LineMouseEventHandler.Instance.OnMouseClickEvent += ShowObstacleTooltip;
                LineMouseEventHandler.Instance.EnableObstacleOutlining();
            }
            else
            {
                obstacleTooltip.SetActive(false);
                LineMouseEventHandler.Instance.OnMouseClickEvent -= ShowObstacleTooltip;
                LineMouseEventHandler.Instance.DisableObstacleOutlining();
            }
        }

        void ShowObstacleTooltip(ILineElement obstacle)
        {
            obstacleTooltip.SetActive(true);
            obstacleTooltip.GetComponent<ObstacleTooltip>().LineElement = obstacle;
        }

        /// <summary>
        /// Creates a slope change info 3D UI at the given position with the given info and returns its GameObject.
        /// </summary>
        /// <param name="info">Information to be shown.</param>
        /// <param name="lineAnchor">The position where the line from the UI should be anchored to.</param>
        /// <returns>The created UI.</returns>
        public GameObject ShowSlopeInfo(IEnumerable<(string name, string value)> info, Vector3 position, Transform parent, Vector3 lineAnchor)
        {
            List<string> fieldNames = new();
            List<string> fieldValues = new();
            foreach (var (name, value) in info)
            {
                fieldNames.Add(name);
                fieldValues.Add(value);
            }

            Quaternion rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            GameObject slopeInfo = Instantiate(slopeInfoPrefab, position, rotation, parent);
            slopeInfo.GetComponent<SlopeInfo>().SetSlopeInfo(fieldNames, fieldValues);

            slopeInfo.GetComponent<LineRenderer>().SetPosition(0, position);
            slopeInfo.GetComponent<LineRenderer>().SetPosition(1, lineAnchor);

            return slopeInfo;
        }


        public SidebarMenu GetSidebar()
        {
            return sidebarMenuUI.GetComponent<SidebarMenu>();            
        }

        public DeleteUI GetDeleteUI()
        {
            return deleteUI.GetComponent<DeleteUI>();
        }
    }
}