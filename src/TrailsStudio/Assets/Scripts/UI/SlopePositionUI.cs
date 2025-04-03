using UnityEngine;
using System.Collections;
using Assets.Scripts.States;
using UnityEngine.UIElements;
using Assets.Scripts.Builders;

namespace Assets.Scripts.UI
{
	public class SlopePositionUI : MonoBehaviour
	{
		private Button cancelButton;
        private SlopePositionHighlighter highlight;

        public void Init(SlopePositionHighlighter highlight)
        {
            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            this.highlight = highlight;
        }
        
        private void OnDisable()
        {
            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
        }        

        private void CancelClicked(ClickEvent evt)
        {
            Destroy(highlight.gameObject);
            StateController.Instance.ChangeState(new DefaultState());
        }
	}
}