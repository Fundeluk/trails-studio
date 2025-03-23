using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.States;

namespace Assets.Scripts.UI
{
    [RequireComponent(typeof(UIDocument))]
    internal class LandingPositionUI : MonoBehaviour
    {
        public GameObject landingPositionHighlighter;

        private Button cancelButton;
        private Button returnButton;

        public void Initialize()
        {
            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);

            returnButton = uiDocument.rootVisualElement.Q<Button>("ReturnButton");
            returnButton.RegisterCallback<ClickEvent>(ReturnClicked);

            LandingPositioningState.LandingHighlighterToggle += SetHighlighterActive;
        }

        public void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
            returnButton.UnregisterCallback<ClickEvent>(ReturnClicked);

            LandingPositioningState.LandingHighlighterToggle -= SetHighlighterActive;
        }

        private void SetHighlighterActive(bool val)
        {
            landingPositionHighlighter.SetActive(val);
        }

        private void CancelClicked(ClickEvent evt)
        {
            // Remove the takeoff
            if (Line.Instance.GetLastLineElement() is TakeoffMeshGenerator.Takeoff)
            {
                Line.Instance.DestroyLastLineElement();
            }

            StateController.Instance.ChangeState(new DefaultState());
        }

        private void ReturnClicked(ClickEvent evt)
        {
            StateController.Instance.ChangeState(new TakeOffBuildState());
        }


    }
}
