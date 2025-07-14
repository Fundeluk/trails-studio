using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    // main menu buttons
    private Button newLineButton;
    private Button loadLineButton;
    private Button settingsButton;
    private Button exitButton;

    //roll-in setup controls
    private FloatField heightInput;
    private IntegerField angleInput;

    private Button buildButton;
    private Button cancelButton;

    // roll in setup values
    private const float MIN_HEIGHT = 2;
    private const float MAX_HEIGHT = 10;
    private readonly string INVALID_HEIGHT_MESSAGE = "Height must be between " + MIN_HEIGHT + " and " + MAX_HEIGHT + " meters";


    private const int MIN_ANGLE = 30;
    private const int MAX_ANGLE = 70;
    private readonly string INVALID_ANGLE_MESSAGE = "Angle must be between " + MIN_ANGLE + " and " + MAX_ANGLE + " degrees";


    // set placeholder values
    public static float height = MIN_HEIGHT;
    public static int angle = MIN_ANGLE;

    private VisualElement menuRoot;
    private VisualElement rollInSetUpRoot;
    private VisualElement loadMenuRoot;
    private VisualElement settingsRoot;

    private SettingsUI settingsUI;
    private SaveLoadUI saveLoadUI;

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
        heightInput.value = MIN_HEIGHT;
        angleInput.value = MIN_ANGLE;
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
        SceneManager.sceneLoaded += OnStudioSceneLoaded;
        SceneManager.LoadScene("StudioScene", LoadSceneMode.Single);

        void OnStudioSceneLoaded(Scene scene, LoadSceneMode mode)
        {            
            // Unsubscribe from the event
            SceneManager.sceneLoaded -= OnStudioSceneLoaded;           
        }        
    }

    public void NewSpotClicked(ClickEvent evt)
    {
        ToRollInSetUp();
    }

    public void ExitClicked(ClickEvent evt)
    {
        Application.Quit();
    }    

    private bool ValidateInput()
    {
        if (heightInput.value < MIN_HEIGHT || heightInput.value > MAX_HEIGHT)
        {
            MainMenuUIManager.Instance.ShowMessage(INVALID_HEIGHT_MESSAGE, 3f);
            heightInput.Focus();
            return false;
        }

        if (angleInput.value < MIN_ANGLE || angleInput.value > MAX_ANGLE)
        {
            MainMenuUIManager.Instance.ShowMessage(INVALID_ANGLE_MESSAGE, 3f);
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
