using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(TakeoffMeshGenerator))]
public class TakeOffInspector : Editor
{
    public VisualTreeAsset m_InspectorXML;

    private FloatField radiusInput;
    private const float MIN_RADIUS = 1;
    private const float MAX_RADIUS = 10;

    private FloatField heightInput;

    private FloatField widthInput;
    private FloatField thicknessInput;
    private IntegerField resolutionInput;

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new();

        m_InspectorXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/TakeOff_Inspector_UXML.uxml");

        inspector = m_InspectorXML.Instantiate();

        inspector.Q<Button>("RedrawButton").RegisterCallback<ClickEvent>(ev => ((TakeoffMeshGenerator)target).GenerateTakeoffMesh());

        radiusInput = inspector.Q<FloatField>("RadiusInput");
        radiusInput.value = 6;
        radiusInput.RegisterValueChangedCallback(ev => ValidateRadius());

        heightInput = inspector.Q<FloatField>("HeightInput");
        heightInput.value = 2;
        heightInput.RegisterValueChangedCallback(ev => ValidateHeight());

        widthInput = inspector.Q<FloatField>("WidthInput");
        widthInput.value = 1.5f;
        widthInput.RegisterValueChangedCallback(ev => ValidateWidth());

        thicknessInput = inspector.Q<FloatField>("ThicknessInput");
        thicknessInput.value = 0.5f;
        thicknessInput.RegisterValueChangedCallback(ev => ValidateThickness());

        resolutionInput = inspector.Q<IntegerField>("ResolutionInput");
        resolutionInput.value = 20;
        resolutionInput.RegisterValueChangedCallback(ev => ValidateResolution());

        return inspector;
    }

    private bool ValidateRadius()
    {
        if (radiusInput.value < MIN_RADIUS)
        {
            radiusInput.value = MIN_RADIUS;
            return false;
        }
        else if (radiusInput.value > MAX_RADIUS)
        {
            radiusInput.value = MAX_RADIUS;
            return false;
        }

        return true;
    }

    private bool ValidateHeight()
    {
        // this prevents the takeoff angle from becoming greater than 90 degrees
        float maxHeight = radiusInput.value;

        float minHeight = MathF.Max(radiusInput.value / 7, 1);

        if (heightInput.value < minHeight)
        {
            heightInput.value = minHeight;
            return false;
        }
        else if (heightInput.value > maxHeight)
        {
            heightInput.value = maxHeight;
            return false;
        }

        return true;
    }

    private bool ValidateWidth()
    {
        float maxWidth = MathF.Min(heightInput.value * 5, 5);

        float minWidth = heightInput.value / 1.5f;

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
        float minThickness = 0.5f;

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

        float radiusLength = TakeoffMeshGenerator.GetEndAngle(radiusInput.value, heightInput.value) * radiusInput.value;
        int minResolution = (int)(radiusLength / 10);

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
}
