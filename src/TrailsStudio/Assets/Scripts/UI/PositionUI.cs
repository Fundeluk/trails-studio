using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public abstract class PositionUI : MonoBehaviour
    {
        private VisualElement anchorIcon;
        public void ToggleAnchorIcon(bool enable)
        {
            anchorIcon.style.visibility = enable ? Visibility.Visible : Visibility.Hidden;
        }     
        
        protected virtual void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            anchorIcon = uiDocument.rootVisualElement.Q<VisualElement>("AnchorIconContainer");
        }
    }
}