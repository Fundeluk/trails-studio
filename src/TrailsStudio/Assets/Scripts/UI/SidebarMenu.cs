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

    // Start is called before the first frame update
    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();

        newJumpButton = uiDocument.rootVisualElement.Q<Button>("NewJumpButton");
        newObstacleButton = uiDocument.rootVisualElement.Q<Button>("NewObstacleButton");
        measureButton = uiDocument.rootVisualElement.Q<Button>("MeasureButton");

        newJumpButton.RegisterCallback<ClickEvent>(NewJumpClicked);
        newObstacleButton.RegisterCallback<ClickEvent>(NewObstacleClicked);
        measureButton.RegisterCallback<ClickEvent>(MeasureClicked);
    }

    private void OnDisable()
    {
        newJumpButton.UnregisterCallback<ClickEvent>(NewJumpClicked);
        newObstacleButton.UnregisterCallback<ClickEvent>(NewObstacleClicked);
        measureButton.UnregisterCallback<ClickEvent>(MeasureClicked);
    }

    private void NewJumpClicked(ClickEvent evt)
    {
        Debug.Log("New Jump clicked");
        StateController.Instance.ChangeState(new TakeOffPositioningState());
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
}
