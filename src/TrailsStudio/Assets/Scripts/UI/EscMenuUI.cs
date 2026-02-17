using Assets.Scripts.Managers;
using SFB;
using System.Collections;
using System.Threading.Tasks;
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
        private Button saveTextButton;

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

            saveTextButton = root.Q<Button>("SaveTextButton");
            saveTextButton.RegisterCallback<ClickEvent>(SaveTextClicked);

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

        private async void SaveTextClicked(ClickEvent evt)
        {
            var path = StandaloneFileBrowser.SaveFilePanel(title: "Save Textual Representation", directory: "", defaultName: Line.Instance.Name, extension: "pdf");

            if (!string.IsNullOrEmpty(path))
            {
                saveTextButton.Toggle(false);

                var textInfo = Line.Instance.GenerateLineTextInfo();
                string lineName = Line.Instance.Name;

                StudioUIManager.Instance.ShowMessage("Generating PDF...", 2f);

                try
                {
                    await LineReportGenerator.GeneratePdfAsync(textInfo, lineName, path);

                    StudioUIManager.Instance.ShowMessage("PDF report saved to " + path, 2f);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Error generating PDF: " + ex.Message);
                    StudioUIManager.Instance.ShowMessage("Failed to generate PDF: " + ex.Message, 3f);
                }
                finally
                {
                    saveTextButton.Toggle(true);
                }

            }
        }
    }
}