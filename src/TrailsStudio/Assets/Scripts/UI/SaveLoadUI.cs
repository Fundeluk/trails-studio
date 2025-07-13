using Assets.Scripts.Managers;
using Assets.Scripts.States;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class SaveLoadUI : MonoBehaviour
    {

        private Button saveButton;
        private Button loadButton;
        private Button deleteButton;
        private Button cancelButton;
        private TextField saveNameField;
        private ListView savesList;

        private VisualElement root;
        private VisualElement savePanel;
        private VisualElement loadPanel;


        [SerializeField]
        VisualTreeAsset listEntryTemplate;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("SaveLoadContainer");

            // Find UI elements
            saveButton = root.Q<Button>("SaveButton");
            loadButton = root.Q<Button>("LoadButton");
            cancelButton = root.Q<Button>("CancelButton");
            deleteButton = root.Q<Button>("DeleteButton");
            saveNameField = root.Q<TextField>("SaveNameField");
            savesList = root.Q<ListView>("SavesList");
            savePanel = root.Q<VisualElement>("SavePanel");
            loadPanel = root.Q<VisualElement>("LoadPanel");

            // Register callbacks
            saveButton.clicked += OnSaveButtonClicked;
            loadButton.clicked += OnLoadButtonClicked;
            cancelButton.clicked += OnCancelButtonClicked;
            deleteButton.clicked += OnDeleteButtonClicked;

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
            deleteButton.clicked -= OnDeleteButtonClicked;

            var root = GetComponent<UIDocument>().rootVisualElement;

            if (root == null)
                return;

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
                StudioUIManager.Instance.ShowMessage("Please enter a save name", 2f);
                return;
            }

            DataManager.Instance.SaveLine(saveName);

            // Return to default state
            enabled = false;
        }

        private void OnLoadButtonClicked()
        {
            if (savesList.selectedItem == null)
            {
                StudioUIManager.Instance.ShowMessage("Please select a save to load", 2f);
                return;
            }

            string sceneName = SceneManager.GetActiveScene().name;           

            string saveName = savesList.selectedItem.ToString();

            if (sceneName == "StudioScene")
            {
                if (DataManager.Instance.LoadLine(saveName))
                {
                    // Return to default state
                    loadPanel.style.display = DisplayStyle.None;
                    StateController.Instance.ChangeState(new DefaultState());
                }
            }
            else if (sceneName == "MenuScene")
            {
                SceneManager.sceneLoaded += OnStudioSceneLoaded;
                SceneManager.LoadScene("StudioScene", LoadSceneMode.Single);

                void OnStudioSceneLoaded(Scene scene, LoadSceneMode mode)
                {
                    if (scene.name == "StudioScene")
                    {
                        // Unsubscribe from the event
                        SceneManager.sceneLoaded -= OnStudioSceneLoaded;

                        bool success = DataManager.Instance.LoadLine(saveName);

                        // if load was unsuccessful, return back to menu
                        if (!success)
                        {
                            SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
                            return;
                        }
                    }
                }                
            }
            else
            {
                StudioUIManager.Instance.ShowMessage("Cannot load line in this scene.", 3f);
                return;
            }

        }

        private void OnCancelButtonClicked()
        {
            root.style.display = DisplayStyle.None;
            savePanel.style.display = DisplayStyle.None;
            loadPanel.style.display = DisplayStyle.None;
            enabled = false;
        }

        private void OnDeleteButtonClicked()
        {
            if (savesList.selectedItem == null)
            {
                StudioUIManager.Instance.ShowMessage("Please select a save to delete", 2f);
                return;
            }

            string saveName = savesList.selectedItem.ToString();
            DataManager.Instance.DeleteSave(saveName);  
            RefreshSavesList();
        }

        private void RefreshSavesList()
        {
            savesList.makeItem = () => listEntryTemplate.Instantiate();

            savesList.bindItem = (element, i) =>
            {
                var label = element.Q<Label>("EntryName");
                label.text = savesList.itemsSource[i].ToString();
            };

            savesList.fixedItemHeight = 45;

            string[] saveFiles = DataManager.Instance.GetSaveFiles();
            savesList.itemsSource = saveFiles;
            savesList.Rebuild();
        }
    }
}