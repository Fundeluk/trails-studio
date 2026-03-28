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

        private Button buildButton;
        private Button cancelButton;

        // roll in setup values
        private readonly string invalidHeightMessage = "Height must be between " + RollInSettings.MinHeight + " and " + RollInSettings.MaxHeight + " meters";
        
        private readonly string invalidAngleMessage = "Angle must be between " + RollInSettings.MinAngleDeg + " and " + RollInSettings.MaxAngleDeg + " degrees";


        // set placeholder values
        public static string lineName = "New Line"; // TODO make this settable during creation
        public static float height = (int)RollInSettings.MinHeight;
        public static int angle = (int)RollInSettings.MinAngleDeg;

        private VisualElement menuRoot;
        private VisualElement rollInSetUpRoot;
        private VisualElement loadMenuRoot;
        private VisualElement settingsRoot;

        private SettingsUI settingsUI;
        private SaveLoadUI saveLoadUI;

        public static bool StartedFromMainMenu {get; private set;} = false;

        // TODO separate rollin setup from main menu controller AND show its exit speed when building it
        public void OnEnable()
        {        
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            menuRoot = root.Q<VisualElement>("menuContainer");
            rollInSetUpRoot = root.Q<VisualElement>("rollInSetUpContainer");
            loadMenuRoot = root.Q<VisualElement>("SaveLoadContainer");
            settingsRoot = root.Q<VisualElement>("SettingsContainer");

            settingsUI = GetComponent<SettingsUI>();
            saveLoadUI = GetComponent<SaveLoadUI>();

            // get main menu buttons and register callbacks
            newLineButton = menuRoot.Q<Button>("NewSpotButton");
            newLineButton.RegisterCallback<ClickEvent>(NewSpotClicked);

            loadLineButton = menuRoot.Q<Button>("LoadSpotButton");
            loadLineButton.RegisterCallback<ClickEvent>(LoadClicked);

            settingsButton = menuRoot.Q<Button>("SettingsButton");
            settingsButton.RegisterCallback<ClickEvent>(SettingsClicked);

            exitButton = menuRoot.Q<Button>("ExitButton");
            exitButton.RegisterCallback<ClickEvent>(ExitClicked);


            // get roll in setup controls
            nameInput = rollInSetUpRoot.Q<TextField>("NameInput");
            heightInput = rollInSetUpRoot.Q<FloatField>("heightInput");
            angleInput = rollInSetUpRoot.Q<IntegerField>("angleInput");
            buildButton = rollInSetUpRoot.Q<Button>("buildButton");
            cancelButton = rollInSetUpRoot.Q<Button>("cancelButton");


            // register roll in setup button callbacks
            buildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
        }

        private void ToRollInSetUp()
        {
            heightInput.value = height;
            angleInput.value = angle;
            rollInSetUpRoot.style.display = DisplayStyle.Flex;
            menuRoot.style.display = DisplayStyle.None;        
        }

        private void ToMainMenu()
        {
            menuRoot.style.display = DisplayStyle.Flex;
            rollInSetUpRoot.style.display = DisplayStyle.None;
        }

        private void ToStudio()
        {
            StartedFromMainMenu = true;
            
            SceneManager.sceneLoaded += OnStudioSceneLoaded;
            SceneManager.LoadScene("StudioScene", LoadSceneMode.Single);

            void OnStudioSceneLoaded(Scene scene, LoadSceneMode mode)
            {            
                // Unsubscribe from the event
                SceneManager.sceneLoaded -= OnStudioSceneLoaded;           
            }        
        }

        private void NewSpotClicked(ClickEvent evt)
        {
            ToRollInSetUp();
        }

        private void ExitClicked(ClickEvent evt)
        {
            Application.Quit();
        }    

        private bool ValidateInput()
        {
            if (nameInput.value == "")
            {
                MainMenuUIManager.Instance.ShowMessage("Name cannot be empty", 3f);
                nameInput.Focus();
                return false;
            }

            if (heightInput.value < RollInSettings.MinHeight || heightInput.value > RollInSettings.MaxHeight)
            {
                MainMenuUIManager.Instance.ShowMessage(invalidHeightMessage, 3f);
                heightInput.Focus();
                return false;
            }

            if (angleInput.value < RollInSettings.MinAngleDeg || angleInput.value > RollInSettings.MaxAngleDeg)
            {
                MainMenuUIManager.Instance.ShowMessage(invalidAngleMessage, 3f);
                angleInput.Focus();
                return false;
            }

            MainMenuUIManager.Instance.HideMessage();
            return true;
        }

        private void BuildClicked(ClickEvent evt)
        {
            // validate input
            bool valid = ValidateInput();

            if (!valid)
            {
                return;
            }

            name = nameInput.value;
            height = heightInput.value;
            angle = angleInput.value;

            ToStudio();
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


        private void CancelClicked(ClickEvent evt)
        {
            ToMainMenu();
        }    
    }
}
