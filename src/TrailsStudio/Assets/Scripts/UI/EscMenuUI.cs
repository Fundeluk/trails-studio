using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class EscMenuUI : MonoBehaviour
    {
        private Button settingsButton;
        private Button saveButton;
        private Button exitButton;
        private Button loadButton;

        private Button quitButton;
        private Button resumeButton;

        private VisualElement menuBox;

        private VisualElement saveLoadBox;
        private SaveLoadUI saveLoadUI;

        private VisualElement settingsBox;
        private SettingsUI settingsUI;


        private void OnEnable()
        {
            settingsUI = GetComponent<SettingsUI>();

            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            menuBox = root.Q<VisualElement>("MenuBox");
            menuBox.style.display = DisplayStyle.Flex;

            settingsBox = root.Q<VisualElement>("SettingsContainer");
            settingsBox.style.display = DisplayStyle.None;

            saveLoadUI = GetComponent<SaveLoadUI>();
            saveLoadBox = root.Q<VisualElement>("SaveLoadContainer");
            saveLoadBox.style.display = DisplayStyle.None;

            settingsButton = root.Q<Button>("SettingsButton");
            settingsButton.RegisterCallback<ClickEvent>(SettingsClicked);

            saveButton = root.Q<Button>("OpenSaveMenuButton");
            saveButton.RegisterCallback<ClickEvent>(SaveClicked);

            loadButton = root.Q<Button>("OpenLoadMenuButton");
            loadButton.RegisterCallback<ClickEvent>(LoadClicked);

            exitButton = root.Q<Button>("ExitButton");
            exitButton.RegisterCallback<ClickEvent>(ExitClicked);

            quitButton = root.Q<Button>("QuitButton");
            quitButton.RegisterCallback<ClickEvent>(QuitClicked);

            resumeButton = root.Q<Button>("ResumeButton");
            resumeButton.RegisterCallback<ClickEvent>(ResumeClicked);

        }

        private void SettingsClicked(ClickEvent evt)
        {
            settingsBox.style.display = DisplayStyle.Flex;
            settingsUI.enabled = true;
        }
        

        private void LoadClicked(ClickEvent evt)
        {
            saveLoadBox.style.display = DisplayStyle.Flex;
            saveLoadUI.enabled = true;
            saveLoadUI.ShowLoadPanel();
        }

        private void SaveClicked(ClickEvent evt)
        {
            saveLoadBox.style.display = DisplayStyle.Flex;
            saveLoadUI.enabled = true;
            saveLoadUI.ShowSavePanel();
        }

        private void ExitClicked(ClickEvent evt)
        {
            SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
        }

        private void QuitClicked(ClickEvent evt)
        {            
            Application.Quit();
        }

        private void ResumeClicked(ClickEvent evt)
        {
            StudioUIManager.Instance.HideESCMenu();
        }
    }
}