using Assets.Scripts.Managers;
using Assets.Scripts.States;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.Builders;

[RequireComponent(typeof(UIDocument))]
public class TakeOffPositionUI : MonoBehaviour
{
    private Button cancelButton;

    public void Initialize()
    {
        var uiDocument = GetComponent<UIDocument>();
        cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
        cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
    }

    // Start is called before the first frame update
    void Start()
    {
        Initialize();
    }


    private void OnEnable()
    {
        Initialize();
    }
    private void OnDisable()
    {
        cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
    }

    private void CancelClicked(ClickEvent evt)
    {
        (BuildManager.Instance.activeBuilder as TakeoffBuilder).Cancel();
        StateController.Instance.ChangeState(new DefaultState());
    }    
}
