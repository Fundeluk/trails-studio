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

        inspector.Q<Button>("RedrawButton").RegisterCallback<ClickEvent>(ev => Redraw());

        slopeInput = inspector.Q<FloatField>("SlopeInput");
        slopeInput.value = 45;
        slopeInput.RegisterValueChangedCallback(evt => OnSlopeInputChanged(evt.newValue));

        heightInput = inspector.Q<FloatField>("HeightInput");
        heightInput.value = 2;

        widthInput = inspector.Q<FloatField>("WidthInput");
        widthInput.value = 1.5f;

        thicknessInput = inspector.Q<FloatField>("ThicknessInput");
        thicknessInput.value = 0.5f;

        resolutionInput = inspector.Q<IntegerField>("ResolutionInput");
        resolutionInput.value = 20;

        return inspector;
    }

    private void OnSlopeInputChanged(float newValue)
    {
        float slopeInRad = newValue * Mathf.Deg2Rad;
        LandingMeshGenerator generator = (LandingMeshGenerator)target;
        generator.slope = slopeInRad;
    }

    private void Redraw()
    {
        Validate();
        ((LandingMeshGenerator)target).GenerateLandingMesh();
    }

    private bool ValidateSlope()
    {
        if (slopeInput.value < MIN_SLOPE)
        {
            slopeInput.value = MIN_SLOPE;
            return false;
        }
        else if (slopeInput.value > MAX_SLOPE)
        {
            slopeInput.value = MAX_SLOPE;
            return false;
        }
        return true;
    }

    private bool ValidateHeight()
    {

        if (heightInput.value < MIN_HEIGHT)
        {
            heightInput.value = MIN_HEIGHT;
            return false;
        }
        else if (heightInput.value > MAX_HEIGHT)
        {
            heightInput.value = MAX_HEIGHT;
            return false;
        }

        return true;
    }

    private bool ValidateWidth()
    {
        float maxWidth = heightInput.value;

        float minWidth = 1;

        if (widthInput.value < minWidth)
        {
            widthInput.value = minWidth;
            return false;
        }
        else if (widthInput.value > maxWidth)
        {
            widthInput.value = maxWidth;
            return false;
        }

        return true;
    }

    private bool ValidateThickness()
    {
        float maxThickness = MathF.Min(heightInput.value / 2, 2);
        float minThickness = 1f;

        if (thicknessInput.value < minThickness)
        {
            thicknessInput.value = minThickness;
            return false;
        }
        else if (thicknessInput.value > maxThickness)
        {
            thicknessInput.value = maxThickness;
            return false;
        }
        return true;
    }

    private bool ValidateResolution()
    {
        int maxResolution = 100;
        int minResolution = 10;

        if (resolutionInput.value < minResolution)
        {
            resolutionInput.value = minResolution;
            return false;
        }
        else if (resolutionInput.value > maxResolution)
        {
            resolutionInput.value = maxResolution;
            return false;
        }
        return true;
    }

    private void Validate()
    {
        ValidateSlope();
        ValidateHeight();
        ValidateWidth();
        ValidateThickness();
        ValidateResolution();
    }
}
