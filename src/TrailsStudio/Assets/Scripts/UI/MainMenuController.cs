using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    // main menu buttons
    private Button newSpotButton;
    private Button exitButton;
    
    //  TODO implement
    private Button loadSpotButton;
    private Button settingsButton;


    //roll-in setup controls
    private FloatField heightInput;
    private IntegerField angleInput;

    private Button setButton;
    private Button cancelButton;

    private TextElement errorMessage;

    // roll in setup values
    private const float MIN_HEIGHT = 1;
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

    public void OnEnable()
    {        
        menuRoot = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("menuContainer");
        rollInSetUpRoot = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("rollInSetUpContainer");

        // get main menu buttons
        newSpotButton = menuRoot.Q<Button>("NewSpotButton");
        loadSpotButton = menuRoot.Q<Button>("LoadSpotButton");
        settingsButton = menuRoot.Q<Button>("SettingsButton");
        exitButton = menuRoot.Q<Button>("ExitButton");

        // register main menu button callbacks
        newSpotButton.RegisterCallback<ClickEvent>(NewSpotClicked);
        exitButton.RegisterCallback<ClickEvent>(ExitClicked);

        // get roll in setup controls
        heightInput = rollInSetUpRoot.Q<FloatField>("heightInput");
        angleInput = rollInSetUpRoot.Q<IntegerField>("angleInput");
        setButton = rollInSetUpRoot.Q<Button>("setButton");
        cancelButton = rollInSetUpRoot.Q<Button>("cancelButton");
        errorMessage = rollInSetUpRoot.Q<TextElement>("errorMessage");


        // register roll in setup button callbacks
        setButton.RegisterCallback<ClickEvent>(SetClicked);
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
        SceneManager.LoadScene("StudioScene", LoadSceneMode.Single);
    }

    public void NewSpotClicked(ClickEvent evt)
    {
        ToRollInSetUp();
    }

    public void ExitClicked(ClickEvent evt)
    {
        Application.Quit();
    }

    private IEnumerator ShowError(string message)
    {
        errorMessage.visible = true;
        errorMessage.text = message;

        // after some time, hide the error message
        yield return new WaitForSeconds(5);
        errorMessage.visible = false;
    }

    private bool ValidateInput()
    {
        if (heightInput.value < MIN_HEIGHT || heightInput.value > MAX_HEIGHT)
        {
            StartCoroutine(ShowError(INVALID_HEIGHT_MESSAGE));
            heightInput.Focus();
            return false;
        }

        if (angleInput.value < MIN_ANGLE || angleInput.value > MAX_ANGLE)
        {
            StartCoroutine(ShowError(INVALID_ANGLE_MESSAGE));
            angleInput.Focus();
            return false;
        }

        errorMessage.visible = false;
        return true;
    }

    public void SetClicked(ClickEvent evt)
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

    public void CancelClicked(ClickEvent evt)
    {
        ToMainMenu();
    }

    private void OnDisable()
    {
        newSpotButton.UnregisterCallback<ClickEvent>(NewSpotClicked);
        exitButton.UnregisterCallback<ClickEvent>(ExitClicked);
        setButton.UnregisterCallback<ClickEvent>(SetClicked);
        cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
        // TODO add rest of buttons after implementation

    }
}
