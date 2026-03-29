using Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Obstacles;

namespace UI
{
    public class MainMenuController : MonoBehaviour
    {
        // main menu buttons
        private Button newLineButton;
        private Button loadLineButton;
        private Button settingsButton;
        private Button exitButton;

        //roll-in setup controls
        private TextField nameInput;
        private FloatField heightInput;
        private IntegerField angleInput;
        private Label exitSpeedLabel;

        private Button buildButton;
        private Button cancelButton;

        public static string LineName = "New Line";
        public static float Height;
        public static int Angle;

        private VisualElement menuRoot;
        private VisualElement rollInSetUpRoot;
        private VisualElement loadMenuRoot;
        private VisualElement settingsRoot;

        private SettingsUI settingsUI;
        private SaveLoadUI saveLoadUI;
        private RollInSetupUI rollInSetupUI;

        public static bool StartedFromMainMenu {get; private set;} = false;

        public void OnEnable()
        {        
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            menuRoot = root.Q<VisualElement>("menuContainer");
            rollInSetUpRoot = root.Q<VisualElement>("rollInSetUpContainer");
            loadMenuRoot = root.Q<VisualElement>("SaveLoadContainer");
            settingsRoot = root.Q<VisualElement>("SettingsContainer");

            settingsUI = GetComponent<SettingsUI>();
            saveLoadUI = GetComponent<SaveLoadUI>();
            rollInSetupUI = GetComponent<RollInSetupUI>();

            // get main menu buttons and register callbacks
            newLineButton = menuRoot.Q<Button>("NewSpotButton");
            newLineButton.RegisterCallback<ClickEvent>(NewSpotClicked);

            loadLineButton = menuRoot.Q<Button>("LoadSpotButton");
            loadLineButton.RegisterCallback<ClickEvent>(LoadClicked);

            settingsButton = menuRoot.Q<Button>("SettingsButton");
            settingsButton.RegisterCallback<ClickEvent>(SettingsClicked);

            exitButton = menuRoot.Q<Button>("ExitButton");
            exitButton.RegisterCallback<ClickEvent>(ExitClicked);

            Height = (int)RollInSettings.MinHeight;
            Angle = (int)RollInSettings.MinAngleDeg;
            
            StartedFromMainMenu = true;
        }

        private void ToRollInSetUp()
        {
            rollInSetUpRoot.style.display = DisplayStyle.Flex;
            rollInSetupUI.enabled = true;
        }

        private void NewSpotClicked(ClickEvent evt)
        {
            ToRollInSetUp();
        }

        private void ExitClicked(ClickEvent evt)
        {
            Application.Quit();
        }


        private void LoadClicked(ClickEvent evt)
        {
            loadMenuRoot.style.display = DisplayStyle.Flex;
            saveLoadUI.enabled = true;
            saveLoadUI.ShowLoadPanel();
        }

        private void SettingsClicked(ClickEvent evt)
        {
            settingsRoot.style.display = DisplayStyle.Flex;
            settingsUI.enabled = true;
        }
    }
}
