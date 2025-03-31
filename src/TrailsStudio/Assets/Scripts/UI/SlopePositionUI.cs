using UnityEngine;
using System.Collections;
using Assets.Scripts.States;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
	public class SlopePositionUI : MonoBehaviour
	{
		private Button cancelButton;        

        public void Initialize()
        {
            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);                     
        }

        // Start is called before the first frame update
        void Start()
        {
            //Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }
        private void OnDisable()
        {
            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
        }        

        private void CancelClicked(ClickEvent evt)
        {
            StateController.Instance.ChangeState(new DefaultState());
        }
	}
}