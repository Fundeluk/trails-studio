using UnityEngine;
using System.Collections;
using Assets.Scripts.States;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
	public class SlopePositionUI : MonoBehaviour
	{
		public GameObject positionHighligher;
		private Button cancelButton;
        
        

        public void Initialize()
        {
            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);

            // Subscribe to the event to toggle the grid highlighter
            SlopePositioningState.SlopePositionHighlighterToggle += SetGridHighlighterActive;            
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
            SlopePositioningState.SlopePositionHighlighterToggle -= SetGridHighlighterActive;
        }

        private void SetGridHighlighterActive(bool value)
        {
            positionHighligher.SetActive(value);
        }

        private void CancelClicked(ClickEvent evt)
        {
            StateController.Instance.ChangeState(new DefaultState());
        }
	}
}