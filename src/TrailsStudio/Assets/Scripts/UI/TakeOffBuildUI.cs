using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class TakeOffBuildUI : MonoBehaviour
    {
        // TODO create takeoff prefab
        //public GameObject takeoffPrefab;

        private Button cancelButton;
        private Button returnButton;

        private void Initialize()
        {
            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            returnButton = uiDocument.rootVisualElement.Q<Button>("ReturnButton");
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            returnButton.RegisterCallback<ClickEvent>(ReturnClicked);
        }

        void Start()
        {
            Initialize();
        }

        void OnEnable()
        {
            Initialize();
        }

        void OnDisable()
        {
            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
            returnButton.UnregisterCallback<ClickEvent>(ReturnClicked);
        }

        private void CancelClicked(ClickEvent evt)
        {
            // TODO cleanup takeoff
            Debug.Log("Cancel clicked");
            StateController.Instance.ChangeState(StateController.defaultState);
        }

        private void ReturnClicked(ClickEvent evt)
        {
            // TODO cleanup takeoff
            Debug.Log("Return clicked");
            StateController.Instance.ChangeState(StateController.takeoffPositionState);
        }

    }
}
