using System;
using LineSystem;
using Managers;
using Obstacles.Landing;
using Obstacles.TakeOff;
using States;
using TerrainEditing;
using TerrainEditing.Slope;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace UI
{
    [RequireComponent(typeof(UIDocument))]
    public class DeleteUI : MonoBehaviour
    {
        private VisualElement root;

        private Button cancelButton;
        private Button deleteButton;

        [SerializeField]
        Material dirtMaterial;

        [SerializeField]
        Material canDeleteMaterial;

        [SerializeField]
        Material cantDeleteMaterial;

        public Material DirtMaterial => dirtMaterial;
        public Material CanDeleteMaterial => canDeleteMaterial;
        public Material CantDeleteMaterial => cantDeleteMaterial;

        // Events for the State to subscribe to
        public event Action OnCancelClicked;
        public event Action OnDeleteClicked;
        
        
        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;

            cancelButton = root.Q<Button>("CancelButton");
            cancelButton.RegisterCallback<ClickEvent>(evt => OnCancelClicked?.Invoke());

            deleteButton = root.Q<Button>("DeleteButton");
            deleteButton.RegisterCallback<ClickEvent>(evt => OnDeleteClicked?.Invoke());
            
            deleteButton.Toggle(false);
        }

        private void OnDisable()
        {
            OnCancelClicked = null;
            OnDeleteClicked = null;
        }

        public void ToggleDeleteButton(bool enable) => deleteButton?.Toggle(enable);
    }
}