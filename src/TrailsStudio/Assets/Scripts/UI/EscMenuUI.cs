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
        private Button quitButton;
        private Button resumeButton;

        private VisualElement menuBox;
        private VisualElement saveLoadBox;

        private SaveLoadUI saveLoadUI;

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            menuBox = root.Q<VisualElement>("MenuBox");
            menuBox.style.display = DisplayStyle.Flex;

            saveLoadUI = GetComponent<SaveLoadUI>();
            saveLoadBox = root.Q<VisualElement>("SaveLoadBox");
            saveLoadBox.style.display = DisplayStyle.None;

            settingsButton = root.Q<Button>("SettingsButton");
            settingsButton.RegisterCallback<ClickEvent>(SettingsClicked);

            saveButton = root.Q<Button>("OpenSaveMenuButton");
            saveButton.RegisterCallback<ClickEvent>(SaveClicked);

            exitButton = root.Q<Button>("ExitButton");
            exitButton.RegisterCallback<ClickEvent>(ExitClicked);

            quitButton = root.Q<Button>("QuitButton");
            quitButton.RegisterCallback<ClickEvent>(QuitClicked);

            resumeButton = root.Q<Button>("ResumeButton");
            resumeButton.RegisterCallback<ClickEvent>(ResumeClicked);

        }
        

        private void SettingsClicked(ClickEvent evt)
        {
            Debug.Log("Settings button clicked");

            //TODO later remove, just for testing
            menuBox.style.display = DisplayStyle.None;

            saveLoadBox.style.display = DisplayStyle.Flex;
            saveLoadUI.enabled = true;
            saveLoadUI.ShowLoadPanel();
        }

        private void SaveClicked(ClickEvent evt)
        {
            menuBox.style.display = DisplayStyle.None;

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
            UIManager.Instance.HideESCMenu();
        }
    }
}