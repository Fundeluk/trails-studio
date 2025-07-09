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
    private Button deleteSlopeButton;
    public Button deleteButton;
    public Button slopeButton;

    public Toggle slopeInfoToggle;

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


    private bool _deleteSlopeButtonEnabled = false;
    public bool DeleteSlopeButtonEnabled
    {
        get => _deleteSlopeButtonEnabled;
        set
        {
            _deleteSlopeButtonEnabled = value;

            if (isActiveAndEnabled)
            {
                UIManager.ToggleButton(deleteSlopeButton, value);
            }

        }
    }

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();

        newJumpButton = uiDocument.rootVisualElement.Q<Button>("NewJumpButton");
        deleteSlopeButton = uiDocument.rootVisualElement.Q<Button>("DeleteSlopeButton");
        deleteButton = uiDocument.rootVisualElement.Q<Button>("DeleteButton");
        slopeButton = uiDocument.rootVisualElement.Q<Button>("SlopeButton");
        slopeInfoToggle = uiDocument.rootVisualElement.Q<Toggle>("SlopeInfoToggle");

        newJumpButton.RegisterCallback<ClickEvent>(NewJumpClicked);
        deleteSlopeButton.RegisterCallback<ClickEvent>(DeleteSlopeClicked);
        deleteButton.RegisterCallback<ClickEvent>(DeleteClicked);
        slopeButton.RegisterCallback<ClickEvent>(SlopeClicked);
        slopeInfoToggle.RegisterCallback<ChangeEvent<bool>>(SlopeInfoToggleChanged);

        // update button states after reenabling
        if (TerrainManager.Instance.ActiveSlope == null && Line.Instance.Count <= 1)
        {
            _deleteButtonEnabled = false;
        }
        else if (TerrainManager.Instance.ActiveSlope != null)
        {
            _slopeButtonEnabled = false;
        }

        SlopeButtonEnabled = _slopeButtonEnabled;
        DeleteButtonEnabled = _deleteButtonEnabled;
        DeleteSlopeButtonEnabled = _deleteSlopeButtonEnabled;
    }

    void OnDisable()
    {
        newJumpButton.UnregisterCallback<ClickEvent>(NewJumpClicked);
        deleteSlopeButton.UnregisterCallback<ClickEvent>(DeleteSlopeClicked);
        deleteButton.UnregisterCallback<ClickEvent>(DeleteClicked);
        slopeButton.UnregisterCallback<ClickEvent>(SlopeClicked);
    }

    void NewJumpClicked(ClickEvent evt)
    {
        Debug.Log("New jump clicked");
        StateController.Instance.ChangeState(new TakeOffBuildState());
    }

    void SlopeClicked(ClickEvent evt)
    {        
        StateController.Instance.ChangeState(new SlopeBuildState());
    }

    private void DeleteSlopeClicked(ClickEvent evt)
    {
        if (TerrainManager.Instance.ActiveSlope == null)
        {
            UIManager.Instance.ShowMessage("No slope to delete.", 2f);
            return;
        }

        TerrainManager.Instance.ActiveSlope.Delete();
    }

    void DeleteClicked(ClickEvent evt)
    {
        StateController.Instance.ChangeState(new DeleteState());
    }    

    void SlopeInfoToggleChanged(ChangeEvent<bool> evt)
    {
        if (evt.newValue)
        {
            TerrainManager.Instance.ShowSlopeInfo();
        }
        else
        {
            TerrainManager.Instance.HideSlopeInfo();
        }
    }
}
