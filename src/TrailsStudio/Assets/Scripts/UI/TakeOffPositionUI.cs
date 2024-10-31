using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TakeOffPositionUI : MonoBehaviour
{
    public MonoBehaviour gridHighlighter;

    private Button cancelButton;

    // Start is called before the first frame update
    void Start()
    {
        var uiDocument = GetComponent<UIDocument>();
        cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
        cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
        gridHighlighter.enabled = true;
    }

    private void OnDisable()
    {
        Debug.Log("TakeOffPositioner disabled. gridhighlighter disabled as well.");
        cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
        gridHighlighter.enabled = false;
    }

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
        cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
        gridHighlighter.enabled = true;
    }

    private void CancelClicked(ClickEvent evt)
    {
        StateController.Instance.ChangeState(StateController.defaultState);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
