using LineSystem;
using Managers;
using States;
using TerrainEditing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class SidebarMenu : MonoBehaviour
    {
        private Button newJumpButton;
        private Button deleteSlopeButton;
        private Button deleteButton;
        private Button slopeButton;

        private Toggle slopeInfoToggle;

        private bool slopeButtonEnabled = true;
        public bool SlopeButtonEnabled
        {
            get => slopeButtonEnabled;
            set
            {
                slopeButtonEnabled = value;

                if (isActiveAndEnabled)
                {
                    slopeButton.Toggle(value);
                }
            }
        }

        private bool deleteButtonEnabled = true;
        public bool DeleteButtonEnabled
        {
            get => deleteButtonEnabled;
            set
            {
                deleteButtonEnabled = value;

                if (isActiveAndEnabled)
                {
                    deleteButton.Toggle(value);            
                }
            }
        }


        private bool deleteSlopeButtonEnabled = false;
        public bool DeleteSlopeButtonEnabled
        {
            get => deleteSlopeButtonEnabled;
            set
            {
                deleteSlopeButtonEnabled = value;

                if (isActiveAndEnabled)
                {
                    deleteSlopeButton.Toggle(value);
                }

            }
        }

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();

            newJumpButton = uiDocument.rootVisualElement.Q<Button>("NewJumpButton");
            deleteSlopeButton = uiDocument.rootVisualElement.Q<Button>("DeleteSlopeButton");
            deleteButton = uiDocument.rootVisualElement.Q<Button>("DeleteButton");
            slopeButton = uiDocument.rootVisualElement.Q<Button>("SlopeButton");
            slopeInfoToggle = uiDocument.rootVisualElement.Q<Toggle>("SlopeInfoToggle");

            newJumpButton.RegisterCallback<ClickEvent>(NewJumpClicked);
            deleteSlopeButton.RegisterCallback<ClickEvent>(DeleteSlopeClicked);
            deleteButton.RegisterCallback<ClickEvent>(DeleteClicked);
            slopeButton.RegisterCallback<ClickEvent>(SlopeClicked);
            slopeInfoToggle.RegisterCallback<ChangeEvent<bool>>(SlopeInfoToggleChanged);

            // update button states after reenabling
            if (TerrainManager.Instance.ActiveSlope == null && Line.Instance.Count <= 1)
            {
                deleteButtonEnabled = false;
            }
            else if (TerrainManager.Instance.ActiveSlope != null)
            {
                slopeButtonEnabled = false;
            }

            SlopeButtonEnabled = slopeButtonEnabled;
            DeleteButtonEnabled = deleteButtonEnabled;
            DeleteSlopeButtonEnabled = deleteSlopeButtonEnabled;
        }

        void OnDisable()
        {
            newJumpButton.UnregisterCallback<ClickEvent>(NewJumpClicked);
            deleteSlopeButton.UnregisterCallback<ClickEvent>(DeleteSlopeClicked);
            deleteButton.UnregisterCallback<ClickEvent>(DeleteClicked);
            slopeButton.UnregisterCallback<ClickEvent>(SlopeClicked);
        }

        void NewJumpClicked(ClickEvent evt)
        {
            InternalDebug.Log("New jump clicked");
            StateController.Instance.ChangeState(new TakeOffBuildState());
        }

        void SlopeClicked(ClickEvent evt)
        {        
            StateController.Instance.ChangeState(new SlopeBuildState());
        }

        private void DeleteSlopeClicked(ClickEvent evt)
        {
            if (TerrainManager.Instance.ActiveSlope == null)
            {
                StudioUIManager.Instance.ShowMessage("No slope to delete.", 2f);
                return;
            }

            TerrainManager.Instance.ActiveSlope.Delete();
            
            TerrainManager.Instance.ClearUnusedTerrains();
        }

        void DeleteClicked(ClickEvent evt)
        {
            StateController.Instance.ChangeState(new DeleteState());
        }    

        void SlopeInfoToggleChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                TerrainManager.Instance.ShowSlopeInfo();
            }
            else
            {
                TerrainManager.Instance.HideSlopeInfo();
            }
        }
    }
}
