using Assets.Scripts.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{

    //TODO add sliders for thickness and height
    public class TakeOffBuildUI : MonoBehaviour
    {
        private Button cancelButton;
        private Button returnButton;

        private Slider radiusSlider;
        private const float MIN_RADIUS = 1;
        private const float MAX_RADIUS = 10;
        
        private Slider heightSlider;

        private Slider thicknessSlider;

        private Slider widthSlider;

        private TakeoffMeshGenerator.Takeoff takeoff;

        private void Initialize()
        {
            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            returnButton = uiDocument.rootVisualElement.Q<Button>("ReturnButton");
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            returnButton.RegisterCallback<ClickEvent>(ReturnClicked);

            radiusSlider = uiDocument.rootVisualElement.Q<Slider>("RadiusSlider");
            radiusSlider.lowValue = MIN_RADIUS;
            radiusSlider.highValue = MAX_RADIUS;
            radiusSlider.RegisterCallback<ChangeEvent<float>>(OnRadiusChanged);

            heightSlider = uiDocument.rootVisualElement.Q<Slider>("HeightSlider");
            heightSlider.highValue = radiusSlider.value;
            heightSlider.lowValue = radiusSlider.value / 7;
            heightSlider.RegisterCallback<ChangeEvent<float>>(OnHeightChanged);

            widthSlider = uiDocument.rootVisualElement.Q<Slider>("WidthSlider");
            widthSlider.lowValue = heightSlider.value / 1.5f;
            widthSlider.highValue = MathF.Min(heightSlider.value * 5, 5);
            widthSlider.RegisterCallback<ChangeEvent<float>>(OnWidthChanged);

            thicknessSlider = uiDocument.rootVisualElement.Q<Slider>("ThicknessSlider");
            thicknessSlider.lowValue = 0.5f;
            thicknessSlider.highValue = MathF.Min(heightSlider.value / 2, 2);
            thicknessSlider.RegisterCallback<ChangeEvent<float>>(OnThicknessChanged);
        }

        public void SetTakeoffElement(TakeoffMeshGenerator.Takeoff element)
        {
            takeoff = element;
        }

        void Start()
        {
            Initialize();
        }

        void OnEnable()
        {
            Initialize();
        }

        void OnDisable()
        {
            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
            returnButton.UnregisterCallback<ClickEvent>(ReturnClicked);

            radiusSlider.UnregisterCallback<ChangeEvent<float>>(OnRadiusChanged);
            heightSlider.UnregisterCallback<ChangeEvent<float>>(OnHeightChanged);
            widthSlider.UnregisterCallback<ChangeEvent<float>>(OnWidthChanged);
            thicknessSlider.UnregisterCallback<ChangeEvent<float>>(OnThicknessChanged);
        }

        private void CancelClicked(ClickEvent evt)
        {
            // destroy the takeoff currently being built
            Line.Instance.DestroyLastLineElement();

            StateController.Instance.ChangeState(new DefaultState());
        }

        private void ReturnClicked(ClickEvent evt)
        {
            // destroy the takeoff currently being built
            Line.Instance.DestroyLastLineElement();

            StateController.Instance.ChangeState(new TakeOffPositioningState());
        }

        private void ValidateHeight(float value)
        {
            float radius = radiusSlider.value;
            float maxHeight = radius;
            float minHeight = Math.Max(radius / 7, 1);

            heightSlider.lowValue = minHeight;
            heightSlider.highValue = maxHeight;
            heightSlider.value = Mathf.Clamp(value, minHeight, maxHeight);
        }

        private void ValidateWidth(float value)
        {
            float height = heightSlider.value;
            float maxWidth = MathF.Min(height * 5, 5);
            float minWidth = height / 1.5f;
            widthSlider.lowValue = minWidth;
            widthSlider.highValue = maxWidth;
            widthSlider.value = Mathf.Clamp(value, minWidth, maxWidth);
        }

        private void ValidateThickness(float value)
        {
            float height = heightSlider.value;
            float maxHeight = MathF.Min(height / 2, 2);
            float minHeight = 0.5f;
            thicknessSlider.lowValue = minHeight;
            thicknessSlider.highValue = maxHeight;
            thicknessSlider.value = Mathf.Clamp(value, minHeight, maxHeight);
        }

        private void OnRadiusChanged(ChangeEvent<float> evt)
        {
            takeoff.SetRadius(evt.newValue);
            ValidateHeight(heightSlider.value);
            ValidateWidth(widthSlider.value);
            ValidateThickness(thicknessSlider.value);
        }

        private void OnHeightChanged(ChangeEvent<float> evt)
        {
            ValidateHeight(evt.newValue);
            ValidateWidth(widthSlider.value);
            ValidateThickness(thicknessSlider.value);
            takeoff.SetHeight(heightSlider.value);
        }

        private void OnWidthChanged(ChangeEvent<float> evt)
        {
            ValidateWidth(evt.newValue);
            takeoff.SetWidth(widthSlider.value);
        }

        private void OnThicknessChanged(ChangeEvent<float> evt)
        {
            ValidateThickness(evt.newValue);
            takeoff.SetThickness(thicknessSlider.value);
        }

    }
}
