using Assets.Scripts.UI;
using Assets.Scripts.Utilities;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
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
        public GameObject obstacleTooltip;
        public GameObject messagePrefab;

        private UIDocument activeMessage = null;

        public GameObject CurrentUI { get; private set; }

        /// <summary>
        /// Hides the current UI and shows the given UI.
        /// </summary>
        /// <param name="ui"></param>
        public void ShowUI(GameObject ui)
        {
            if (CurrentUI != null)
            {
                CurrentUI.SetActive(false);
            }

            ui.SetActive(true);
            CurrentUI = ui;
        }

        public void EnableObstacleTooltips()
        {
            LineMouseEventHandler.Instance.OnMouseClickEvent += ShowObstacleTooltip;
        }

        public void DisableObstacleTooltips()
        {
            obstacleTooltip.SetActive(false);
            LineMouseEventHandler.Instance.OnMouseClickEvent -= ShowObstacleTooltip;
        }

        void ShowObstacleTooltip(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            ILineElement lineElement = Line.GetLineElementFromGameObject(gameObject);

            if (lineElement == null)
            {
                return;
            }

            obstacleTooltip.SetActive(true);
            obstacleTooltip.GetComponent<ObstacleTooltip>().LineElement = lineElement;
        }

        public void ShowMessage(string message, float duration=0)
        {
            if (activeMessage == null)
            {
                activeMessage = Instantiate(messagePrefab, transform).GetComponent<UIDocument>();                
            }
            
            activeMessage.rootVisualElement.Q<Label>("Message").text = message;
            activeMessage.enabled = true;

            if (duration > 0)
            {
                if (destroyMessageCoroutine != null)
                {
                    StopCoroutine(destroyMessageCoroutine);
                }

                destroyMessageCoroutine = StartCoroutine(HideMessageAfterDelay(duration));
            }
        }

        public void HideMessage()
        {
            if (activeMessage != null)
            {
                if (destroyMessageCoroutine != null)
                {
                    StopCoroutine(destroyMessageCoroutine);
                    destroyMessageCoroutine = null;
                }

                activeMessage.enabled = false;
                Destroy(activeMessage.gameObject);
                activeMessage = null;
            }
        }

        private Coroutine destroyMessageCoroutine;

        private IEnumerator HideMessageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideMessage();
        }

        public SidebarMenu GetSidebar()
        {
            return sidebarMenuUI.GetComponent<SidebarMenu>();            
        }

        public DeleteUI GetDeleteUI()
        {
            return deleteUI.GetComponent<DeleteUI>();
        }

        public Coroutine StartCoroutineFromInstance(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }

        public void StopCoroutineFromInstance(Coroutine routine)
        {            
            StopCoroutine(routine);
        }

        public static void ToggleButton(Button button, bool enable)
        {
            if (button.enabledSelf == enable)
            {
                return;
            }

            if (enable)
            {
                button.RemoveFromClassList("sidebar-button__disabled");
            }
            else
            {
                button.AddToClassList("sidebar-button__disabled");
            }
            button.SetEnabled(enable);
        }
    }
}