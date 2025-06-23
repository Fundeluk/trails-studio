using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public abstract class PositionUI : MonoBehaviour
    {
        protected VisualElement anchorIcon;
        public virtual void ToggleAnchorIcon(bool enable)
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