using Assets.Scripts.States;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class TakeOffPositionUI : MonoBehaviour
{
    public GameObject takeoffPositionHighligher; 

    private Button cancelButton;

    public void Initialize()
    {
        var uiDocument = GetComponent<UIDocument>();
        cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
        cancelButton.RegisterCallback<ClickEvent>(CancelClicked);

        // Subscribe to the event to toggle the grid highlighter
        TakeOffPositioningState.TakeOffPositionHighlighterToggle += SetGridHighlighterActive;
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
        TakeOffPositioningState.TakeOffPositionHighlighterToggle -= SetGridHighlighterActive;
    }

    private void SetGridHighlighterActive(bool value)
    {       
        takeoffPositionHighligher.SetActive(value);        
    }

    private void CancelClicked(ClickEvent evt)
    {
        StateController.Instance.ChangeState(new DefaultState());
    }    
}
