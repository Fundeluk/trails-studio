using Assets.Scripts.Builders;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(TakeoffMeshGenerator))]
public class TakeOffInspector : Editor
{
    private FloatField radiusInput;
    private const float MIN_RADIUS = 1;
    private const float MAX_RADIUS = 10;

    public VisualTreeAsset m_InspectorXML;

    private FloatField heightInput;

    private FloatField widthInput;
    private FloatField thicknessInput;
    private IntegerField resolutionInput;

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new();

        m_InspectorXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/TakeOff_Inspector_UXML.uxml");

        inspector = m_InspectorXML.Instantiate();

        VisualElement defaultInspector = inspector.Q<VisualElement>("DefaultInspector");
        InspectorElement.FillDefaultInspector(defaultInspector, serializedObject, this);

        inspector.Q<Button>("RedrawButton").RegisterCallback<ClickEvent>(ev => Redraw());

        radiusInput = inspector.Q<FloatField>("RadiusInput");
        radiusInput.RegisterValueChangedCallback(ev => OnRadiusChanged());

        heightInput = inspector.Q<FloatField>("HeightInput");
        heightInput.RegisterValueChangedCallback(ev => OnHeightChanged());

        widthInput = inspector.Q<FloatField>("WidthInput");
        widthInput.RegisterValueChangedCallback(ev => OnWidthChanged());

        thicknessInput = inspector.Q<FloatField>("ThicknessInput");
        thicknessInput.RegisterValueChangedCallback(ev => OnThicknessChanged());

        resolutionInput = inspector.Q<IntegerField>("ResolutionInput");
        resolutionInput.RegisterValueChangedCallback(ev => OnResolutionChanged());

        return inspector;
    }

    private void Redraw()
    {
        ((TakeoffMeshGenerator)target).SetBatch(height: heightInput.value, width: widthInput.value, thickness: thicknessInput.value, radius: radiusInput.value, resolution: resolutionInput.value);
    }

    private void OnRadiusChanged()
    {
        if (radiusInput.value < MIN_RADIUS)
        {
            radiusInput.value = MIN_RADIUS;            
        }
        else if (radiusInput.value > MAX_RADIUS)
        {
            radiusInput.value = MAX_RADIUS;            
        }

        ((TakeoffMeshGenerator)target).Radius = radiusInput.value;
    }

    private void OnHeightChanged()
    {
        // this prevents the takeoff Angle from becoming greater than 90 degrees
        float maxHeight = radiusInput.value;

        float minHeight = MathF.Max(radiusInput.value / 7, 1);

        if (heightInput.value < minHeight)
        {
            heightInput.value = minHeight;
        }
        else if (heightInput.value > maxHeight)
        {
            heightInput.value = maxHeight;
        }

        ((TakeoffMeshGenerator)target).Height = heightInput.value;
    }

    private void OnWidthChanged()
    {
        float maxWidth = MathF.Min(heightInput.value * 5, 5);

        float minWidth = heightInput.value / 1.5f;

        if (widthInput.value < minWidth)
        {
            widthInput.value = minWidth;
        }
        else if (widthInput.value > maxWidth)
        {
            widthInput.value = maxWidth;
        }

        ((TakeoffMeshGenerator)target).Width = widthInput.value;
    }

    private void OnThicknessChanged()
    {
        float maxThickness = MathF.Min(heightInput.value / 2, 2);
        float minThickness = 0.5f;

        if (thicknessInput.value < minThickness)
        {
            thicknessInput.value = minThickness;
        }
        else if (thicknessInput.value > maxThickness)
        {
            thicknessInput.value = maxThickness;
        }

        ((TakeoffMeshGenerator)target).Thickness = thicknessInput.value;
    }

    private void OnResolutionChanged()
    {
        int maxResolution = 100;

        float radiusLength = ((TakeoffMeshGenerator)target).CalculateRadiusLength();
        int minResolution = (int)(radiusLength / 10);

        if (resolutionInput.value < minResolution)
        {
            resolutionInput.value = minResolution;
        }
        else if (resolutionInput.value > maxResolution)
        {
            resolutionInput.value = maxResolution;
        }

        ((TakeoffMeshGenerator)target).Resolution = resolutionInput.value;
    }
}
