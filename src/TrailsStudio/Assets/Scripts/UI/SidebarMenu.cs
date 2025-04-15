using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.States;
using Assets.Scripts.Managers;
using Assets.Scripts.Builders;

public class SidebarMenu : MonoBehaviour
{
    public Button newJumpButton;
    public Button measureButton;
    public Button deleteButton;
    public Button slopeButton;

    private bool _slopeButtonEnabled = true;
    public bool SlopeButtonEnabled
    {
        get => _slopeButtonEnabled;
        set
        {
            _slopeButtonEnabled = value;

            if (isActiveAndEnabled)
            {
                UIManager.ToggleButton(slopeButton, value);
            }
        }
    }

    private bool _deleteButtonEnabled = true;
    public bool DeleteButtonEnabled
    {
        get => _deleteButtonEnabled;
        set
        {
            _deleteButtonEnabled = value;

            if (isActiveAndEnabled)
            {
                UIManager.ToggleButton(deleteButton, value);            
            }
        }
    }


    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();

        newJumpButton = uiDocument.rootVisualElement.Q<Button>("NewJumpButton");
        measureButton = uiDocument.rootVisualElement.Q<Button>("MeasureButton");
        deleteButton = uiDocument.rootVisualElement.Q<Button>("DeleteButton");
        slopeButton = uiDocument.rootVisualElement.Q<Button>("SlopeButton");

        newJumpButton.RegisterCallback<ClickEvent>(NewJumpClicked);
        measureButton.RegisterCallback<ClickEvent>(MeasureClicked);
        deleteButton.RegisterCallback<ClickEvent>(DeleteClicked);
        slopeButton.RegisterCallback<ClickEvent>(SlopeClicked);

        // update button states after reenabling
        SlopeButtonEnabled = _slopeButtonEnabled;
        DeleteButtonEnabled = _deleteButtonEnabled;
    }

    void OnDisable()
    {
        newJumpButton.UnregisterCallback<ClickEvent>(NewJumpClicked);
        measureButton.UnregisterCallback<ClickEvent>(MeasureClicked);
        deleteButton.UnregisterCallback<ClickEvent>(DeleteClicked);
        slopeButton.UnregisterCallback<ClickEvent>(SlopeClicked);
    }

    void NewJumpClicked(ClickEvent evt)
    {
        Debug.Log("New jump clicked");
        StateController.Instance.ChangeState(new TakeOffPositioningState());
    }

    void SlopeClicked(ClickEvent evt)
    {        
        StateController.Instance.ChangeState(new SlopePositioningState());
    }

    void MeasureClicked(ClickEvent evt)
    {
        Debug.Log("Measure clicked");
        //StateController.Instance.ChangeState(new MeasureState());
    }     

    void DeleteClicked(ClickEvent evt)
    {
        StateController.Instance.ChangeState(new DeleteState());
    }    
}
