using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.States;
using Assets.Scripts.Managers;
using Assets.Scripts.Builders;

namespace Assets.Scripts.UI
{
    [RequireComponent(typeof(UIDocument))]
    internal class LandingPositionUI : MonoBehaviour
    {
        private Button returnButton;

        public void Initialize()
        {
            var uiDocument = GetComponent<UIDocument>();

            returnButton = uiDocument.rootVisualElement.Q<Button>("ReturnButton");
            returnButton.RegisterCallback<ClickEvent>(ReturnClicked);
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
            returnButton.UnregisterCallback<ClickEvent>(ReturnClicked);
        }        

        private void ReturnClicked(ClickEvent evt)
        {
            BuildManager.Instance.activeBuilder.DestroyUnderlyingGameObject();
            TakeoffBuilder builder = (Line.Instance.GetLastLineElement() as Takeoff).Revert();
            StateController.Instance.ChangeState(new TakeOffBuildState(builder));
        }
    }
}
