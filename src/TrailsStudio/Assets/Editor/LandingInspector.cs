using Assets.Scripts.Builders;
using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(LandingMeshGenerator))]
public class LandingInspector : Editor
{
    public VisualTreeAsset m_InspectorXML;

    private FloatField slopeInput;
    private const float MIN_SLOPE = 30;
    private const float MAX_SLOPE = 80;

    private FloatField heightInput;
    private const float MIN_HEIGHT = 1;
    private const float MAX_HEIGHT = 10;

    private FloatField widthInput;
    private FloatField thicknessInput;
    private IntegerField resolutionInput;

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new();

        m_InspectorXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/Landing_Inspector_UXML.uxml");

        inspector = m_InspectorXML.Instantiate();

        VisualElement defaultInspector = inspector.Q<VisualElement>("DefaultInspector");
        InspectorElement.FillDefaultInspector(defaultInspector, serializedObject, this);

        inspector.Q<Button>("RedrawButton").RegisterCallback<ClickEvent>(ev => Redraw());

        slopeInput = inspector.Q<FloatField>("SlopeInput");
        slopeInput.RegisterValueChangedCallback(evt => OnSlopeChanged());

        heightInput = inspector.Q<FloatField>("HeightInput");

        widthInput = inspector.Q<FloatField>("WidthInput");

        thicknessInput = inspector.Q<FloatField>("ThicknessInput");

        resolutionInput = inspector.Q<IntegerField>("ResolutionInput");

        return inspector;
    }

    private void Redraw()
    {
        ((LandingMeshGenerator)target).SetBatch(heightInput.value, widthInput.value, thicknessInput.value, slopeInput.value * Mathf.Deg2Rad, resolutionInput.value);
    }

    private void OnSlopeChanged()
    {
        if (slopeInput.value < MIN_SLOPE)
        {
            slopeInput.value = MIN_SLOPE;
        }
        else if (slopeInput.value > MAX_SLOPE)
        {
            slopeInput.value = MAX_SLOPE;
        }

        ((LandingMeshGenerator)target).Slope = slopeInput.value * Mathf.Deg2Rad;
    }

    private void OnHeightChanged()
    {

        if (heightInput.value < MIN_HEIGHT)
        {
            heightInput.value = MIN_HEIGHT;
        }
        else if (heightInput.value > MAX_HEIGHT)
        {
            heightInput.value = MAX_HEIGHT;
        }

        ((LandingMeshGenerator)target).Height = heightInput.value;
    }

    private void OnWidthChanged()
    {
        float maxWidth = heightInput.value;

        float minWidth = 1;

        if (widthInput.value < minWidth)
        {
            widthInput.value = minWidth;
        }
        else if (widthInput.value > maxWidth)
        {
            widthInput.value = maxWidth;
        }

        ((LandingMeshGenerator)target).Width = widthInput.value;
    }

    private void OnThicknessChanged()
    {
        float maxThickness = MathF.Min(heightInput.value / 2, 2);
        float minThickness = 1f;

        if (thicknessInput.value < minThickness)
        {
            thicknessInput.value = minThickness;
        }
        else if (thicknessInput.value > maxThickness)
        {
            thicknessInput.value = maxThickness;
        }

        ((LandingMeshGenerator)target).Thickness = thicknessInput.value;
    }

    private void OnResolutionChanged()
    {
        int maxResolution = 100;
        int minResolution = 10;

        if (resolutionInput.value < minResolution)
        {
            resolutionInput.value = minResolution;
        }
        else if (resolutionInput.value > maxResolution)
        {
            resolutionInput.value = maxResolution;
        }

        ((LandingMeshGenerator)target).Resolution = resolutionInput.value;
    }    
}
