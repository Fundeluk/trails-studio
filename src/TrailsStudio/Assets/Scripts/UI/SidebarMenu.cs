using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.States;

public class SidebarMenu : MonoBehaviour
{
    private Button newJumpButton;
    private Button newObstacleButton;
    private Button measureButton;
    private Button deleteButton;
    private Button slopeButton;

    // Start is called before the first frame update
    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();

        newJumpButton = uiDocument.rootVisualElement.Q<Button>("NewJumpButton");
        newObstacleButton = uiDocument.rootVisualElement.Q<Button>("NewObstacleButton");
        measureButton = uiDocument.rootVisualElement.Q<Button>("MeasureButton");
        deleteButton = uiDocument.rootVisualElement.Q<Button>("DeleteButton");
        slopeButton = uiDocument.rootVisualElement.Q<Button>("SlopeButton");

        newJumpButton.RegisterCallback<ClickEvent>(NewJumpClicked);
        newObstacleButton.RegisterCallback<ClickEvent>(NewObstacleClicked);
        measureButton.RegisterCallback<ClickEvent>(MeasureClicked);
        deleteButton.RegisterCallback<ClickEvent>(DeleteClicked);
        slopeButton.RegisterCallback<ClickEvent>(SlopeClicked);

    }

    private void OnDisable()
    {
        newJumpButton.UnregisterCallback<ClickEvent>(NewJumpClicked);
        newObstacleButton.UnregisterCallback<ClickEvent>(NewObstacleClicked);
        measureButton.UnregisterCallback<ClickEvent>(MeasureClicked);
        deleteButton.UnregisterCallback<ClickEvent>(DeleteClicked);
        slopeButton.UnregisterCallback<ClickEvent>(SlopeClicked);
    }

    private void NewJumpClicked(ClickEvent evt)
    {
        Debug.Log("New Jump clicked");
        StateController.Instance.ChangeState(new TakeOffPositioningState());
    }

    private void SlopeClicked(ClickEvent evt)
    {
        Debug.Log("Slope clicked");
        StateController.Instance.ChangeState(new SlopePositioningState());
    }

    private void NewObstacleClicked(ClickEvent evt)
    {
        Debug.Log("New Obstacle clicked");
        //StateController.Instance.ChangeState(new NewObstacleState());
    }

    private void MeasureClicked(ClickEvent evt)
    {
        Debug.Log("Measure clicked");
        //StateController.Instance.ChangeState(new MeasureState());
    }

    private void DeleteClicked(ClickEvent evt)
    {
        //if (Line.Instance.line.Count == 1)
        //{
        //    // cannot delete rollin
        //    return;
        //}

        StateController.Instance.ChangeState(new DeleteState());
    }
}
