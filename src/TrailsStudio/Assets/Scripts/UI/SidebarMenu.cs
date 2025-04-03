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

    // Start is called before the first frame update
    void Awake()
    {
        var uiDocument = GetComponent<UIDocument>();

        newJumpButton = uiDocument.rootVisualElement.Q<Button>("NewJumpButton");
        measureButton = uiDocument.rootVisualElement.Q<Button>("MeasureButton");
        deleteButton = uiDocument.rootVisualElement.Q<Button>("DeleteButton");
        slopeButton = uiDocument.rootVisualElement.Q<Button>("SlopeButton");
    }

    private void OnEnable()
    {
        newJumpButton.RegisterCallback<ClickEvent>(NewJumpClicked);
        measureButton.RegisterCallback<ClickEvent>(MeasureClicked);
        deleteButton.RegisterCallback<ClickEvent>(DeleteClicked);
        slopeButton.RegisterCallback<ClickEvent>(SlopeClicked);
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

    public void ToggleSlopeButton(bool enable)
    {
        Debug.Log("Toggling slope button: " + enable);

        if (enable)
        {
            slopeButton.RemoveFromClassList("sidebar-button__disabled");
        }
        else
        {
            slopeButton.AddToClassList("sidebar-button__disabled");
        }

        slopeButton.SetEnabled(enable);

        Debug.Log("Button enabled: " + slopeButton.enabledInHierarchy);
    }

    void DeleteClicked(ClickEvent evt)
    {
        if (Line.Instance.line.Count == 1)
        {
            // cannot delete rollin
            return;
        }

        StateController.Instance.ChangeState(new DeleteState());
    }    
}
