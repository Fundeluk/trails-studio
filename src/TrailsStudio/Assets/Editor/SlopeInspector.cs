using Assets.Scripts.Builders;
using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using Unity.VisualScripting;

[CustomEditor(typeof(SlopeChange))]
public class SlopeInspector : Editor
{
    public VisualTreeAsset m_InspectorXML;

    private ObjectField previousElementField;

    private FloatField distanceFromPreviousInput;
    private FloatField endHeightInput;

    private FloatField lengthInput;
    private const float MIN_LENGTH = 1;
    private const float MAX_LENGTH = 30;

    
    private Button initButton;

    private ObjectField waypointField;

    private Button addWaypointButton;

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new();

        m_InspectorXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/SlopeChangeInspector.uxml");

        inspector = m_InspectorXML.Instantiate();

        initButton = inspector.Q<Button>("InitButton");
        initButton.RegisterCallback<ClickEvent>(ev => Init());

        previousElementField = inspector.Q<ObjectField>("PreviousElementField");

        distanceFromPreviousInput = inspector.Q<FloatField>("DistanceFromPreviousInput");

        endHeightInput = inspector.Q<FloatField>("EndHeightInput");

        lengthInput = inspector.Q<FloatField>("LengthInput");

        waypointField = inspector.Q<ObjectField>("WaypointField");

        addWaypointButton = inspector.Q<Button>("AddWaypointButton");
        addWaypointButton.RegisterCallback<ClickEvent>(ev => AddWaypoint());

        return inspector;
    }

    private void Init()
    {
        ((SlopeChange)target).InitializeForTesting(distanceFromPreviousInput.value, endHeightInput.value, lengthInput.value, previousElementField.value.GetComponent<ILineElement>());
        initButton.SetEnabled(false);
    }

    private void AddWaypoint()
    {
        if (waypointField.value != null)
        {
            GameObject obj = (waypointField.value as GameObject);
            if (obj.TryGetComponent<Takeoff>(out var takeoff))
            {
                ((SlopeChange)target).AddWaypoint(takeoff);
            }
            else if (obj.TryGetComponent<Landing>(out var landing))
            {
                ((SlopeChange)target).AddWaypoint(landing);
            }
            waypointField.SetValueWithoutNotify(null);
        }
    }
}
