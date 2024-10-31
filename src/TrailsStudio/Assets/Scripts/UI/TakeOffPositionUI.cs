using Assets.Scripts.States;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TakeOffPositionUI : MonoBehaviour
{
    public GameObject gridHighlighter; 

    private Button cancelButton;

    public void Initialize()
    {
        var uiDocument = GetComponent<UIDocument>();
        cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
        cancelButton.RegisterCallback<ClickEvent>(CancelClicked);

        // Subscribe to the event to toggle the grid highlighter
        TakeOffPositioningState.GridHighlighterToggle += SetGridHighlighterActive;
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
        TakeOffPositioningState.GridHighlighterToggle -= SetGridHighlighterActive;
    }

    private void SetGridHighlighterActive(bool value)
    {       
        gridHighlighter.SetActive(value);        
    }

    private void CancelClicked(ClickEvent evt)
    {
        StateController.Instance.ChangeState(new DefaultState());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
