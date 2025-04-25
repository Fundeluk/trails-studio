using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class ObstacleTooltip : MonoBehaviour
    {
        [SerializeField]
        VisualTreeAsset fieldTemplate;

        VisualElement fieldContainer;

        Button closeButton;

        ILineElement _lineElement;
        public ILineElement LineElement
        {
            get
            {
                return _lineElement;
            }
            set
            {                
                fieldContainer.Clear();
                if (_lineElement != null)
                {
                    _lineElement.OnTooltipClosed();
                }
                _lineElement = value;
                if (_lineElement != null)
                {                    
                    _lineElement.OnTooltipShow();
                    var fields = _lineElement.GetLineElementInfo();
                    foreach (var field in fields)
                    {
                        var fieldInstance = fieldTemplate.CloneTree();
                        var nameLabel = fieldInstance.Q<Label>("FieldName");
                        var valueLabel = fieldInstance.Q<Label>("FieldValue");
                        
                        nameLabel.text = field.name;                        
                        valueLabel.text = field.value;

                        Debug.Log($"field container: {fieldContainer}");
                        Debug.Log($"field template instance: {fieldTemplate.CloneTree()}");

                        fieldContainer.Add(fieldInstance);                        
                    }
                }               
            }
        }

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;
            fieldContainer = root.Q<VisualElement>("FieldsContainer");
            closeButton = root.Q<Button>("CloseButton");
            closeButton.RegisterCallback<ClickEvent>(CloseClicked);            
        }

        private void OnDisable()
        {
            closeButton.UnregisterCallback<ClickEvent>(CloseClicked);

            if (_lineElement != null)
            {
                _lineElement.OnTooltipClosed();
                LineElement = null;
            }
        }

        void CloseClicked(ClickEvent evt)
        {
            gameObject.SetActive(false);
        }
    }
}