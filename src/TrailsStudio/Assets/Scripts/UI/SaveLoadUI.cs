using Assets.Scripts.Managers;
using Assets.Scripts.States;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class SaveLoadUI : MonoBehaviour
    {

        private Button saveButton;
        private Button loadButton;
        private Button cancelButton;
        private TextField saveNameField;
        private ListView savesList;
        private VisualElement savePanel;
        private VisualElement loadPanel;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            // Find UI elements
            saveButton = root.Q<Button>("SaveButton");
            loadButton = root.Q<Button>("LoadButton");
            cancelButton = root.Q<Button>("CancelButton");
            saveNameField = root.Q<TextField>("SaveNameField");
            savesList = root.Q<ListView>("SavesList");
            savePanel = root.Q<VisualElement>("SavePanel");
            loadPanel = root.Q<VisualElement>("LoadPanel");

            // Register callbacks
            saveButton.clicked += OnSaveButtonClicked;
            loadButton.clicked += OnLoadButtonClicked;
            cancelButton.clicked += OnCancelButtonClicked;

            // Setup save list
            RefreshSavesList();

            // Initial state
            savePanel.style.display = DisplayStyle.None;
            loadPanel.style.display = DisplayStyle.None;
        }

        private void OnDisable()
        {
            saveButton.clicked -= OnSaveButtonClicked;
            loadButton.clicked -= OnLoadButtonClicked;
            cancelButton.clicked -= OnCancelButtonClicked;

            var root = GetComponent<UIDocument>().rootVisualElement;

            VisualElement menuBox = root.Q<VisualElement>("MenuBox");
            VisualElement saveLoadBox = root.Q<VisualElement>("SaveLoadBox");

            if (menuBox != null && saveLoadBox != null)
            {
                menuBox.style.display = DisplayStyle.Flex;
                saveLoadBox.style.display = DisplayStyle.None;
            }
        }

        public void ShowSavePanel()
        {
            savePanel.style.display = DisplayStyle.Flex;
            loadPanel.style.display = DisplayStyle.None;
            saveNameField.value = $"Line_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        public void ShowLoadPanel()
        {
            savePanel.style.display = DisplayStyle.None;
            loadPanel.style.display = DisplayStyle.Flex;
            RefreshSavesList();
        }

        private void OnSaveButtonClicked()
        {
            string saveName = saveNameField.value;
            if (string.IsNullOrWhiteSpace(saveName))
            {
                UIManager.Instance.ShowMessage("Please enter a save name", 2f);
                return;
            }

            DataManager.Instance.SaveLine(saveName);

            // Return to default state
            savePanel.style.display = DisplayStyle.None;
        }

        private void OnLoadButtonClicked()
        {
            if (savesList.selectedItem == null)
            {
                UIManager.Instance.ShowMessage("Please select a save to load", 2f);
                return;
            }

            string saveName = savesList.selectedItem.ToString();
            if (DataManager.Instance.LoadLine(saveName))
            {
                // Return to default state
                loadPanel.style.display = DisplayStyle.None;
                StateController.Instance.ChangeState(new DefaultState());
            }
        }

        private void OnCancelButtonClicked()
        {
            savePanel.style.display = DisplayStyle.None;
            loadPanel.style.display = DisplayStyle.None;
            enabled = false;
        }

        private void RefreshSavesList()
        {
            string[] saveFiles = DataManager.Instance.GetSaveFiles();
            savesList.itemsSource = saveFiles;
            savesList.Rebuild();
        }
    }
}