using LineSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class ObstacleTooltip : MonoBehaviour
    {
        [SerializeField]
        VisualTreeAsset fieldTemplate;

        ScrollView fieldContainer;

        Button closeButton;

        private ILineElement lineElement;
        public ILineElement LineElement
        {
            get => lineElement;
            set
            {                
                fieldContainer.Clear();
                lineElement?.OnTooltipClosed();
                lineElement = value;
                if (lineElement != null)
                {                    
                    lineElement.OnTooltipShow();
                    var fields = lineElement.GetLineElementInfo();
                    Debug.Log($"Showing tooltip for {lineElement.GetType().Name} with {fields.Count} fields.");
                    foreach (var field in fields)
                    {
                        var fieldInstance = fieldTemplate.CloneTree();
                        var nameLabel = fieldInstance.Q<Label>("FieldName");
                        var valueLabel = fieldInstance.Q<Label>("FieldValue");
                        
                        nameLabel.text = field.name;                        
                        valueLabel.text = field.value;

                        fieldContainer.Add(fieldInstance);                        
                    }
                }               
            }
        }

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;
            fieldContainer = root.Q<ScrollView>("FieldsContainer");
            closeButton = root.Q<Button>("CloseButton");
            closeButton.RegisterCallback<ClickEvent>(CloseClicked);            
        }

        private void OnDisable()
        {
            closeButton.UnregisterCallback<ClickEvent>(CloseClicked);

            if (lineElement != null)
            {
                lineElement.OnTooltipClosed();
                LineElement = null;
            }
        }

        void CloseClicked(ClickEvent evt)
        {
            gameObject.SetActive(false);
        }
    }
}