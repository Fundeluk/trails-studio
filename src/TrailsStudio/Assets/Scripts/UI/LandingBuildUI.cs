using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.Builders;
using Assets.Scripts.States;

namespace Assets.Scripts.UI
{
    public class LandingBuildUI : MonoBehaviour
    {
        private Button cancelButton;
        private Button returnButton;

        private Button buildButton;

        private Slider slopeSlider;
        private const float MIN_SLOPE = 30;
        private const float MAX_SLOPE = 70;

        private Slider widthSlider;

        private Slider heightSlider;

        private Slider thicknessSlider;

        private SliderInt rotationSlider;

        private LandingMeshGenerator.Landing landing;

        private void Initialize()
        {
            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            returnButton = uiDocument.rootVisualElement.Q<Button>("ReturnButton");
            buildButton = uiDocument.rootVisualElement.Q<Button>("BuildButton");
            buildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            returnButton.RegisterCallback<ClickEvent>(ReturnClicked);

            slopeSlider = uiDocument.rootVisualElement.Q<Slider>("SlopeSlider");
            slopeSlider.lowValue = MIN_SLOPE;
            slopeSlider.highValue = MAX_SLOPE;
            slopeSlider.RegisterCallback<ChangeEvent<float>>(OnSlopeChanged);

            heightSlider = uiDocument.rootVisualElement.Q<Slider>("HeightSlider");
            heightSlider.lowValue = 1;
            heightSlider.highValue = 10;
            heightSlider.RegisterCallback<ChangeEvent<float>>(OnHeightChanged);

            widthSlider = uiDocument.rootVisualElement.Q<Slider>("WidthSlider");
            widthSlider.lowValue = heightSlider.value;
            widthSlider.highValue = 5;
            widthSlider.RegisterCallback<ChangeEvent<float>>(OnWidthChanged);

            thicknessSlider = uiDocument.rootVisualElement.Q<Slider>("ThicknessSlider");
            thicknessSlider.lowValue = 1;
            thicknessSlider.highValue = 2.5f;
            thicknessSlider.RegisterCallback<ChangeEvent<float>>(OnThicknessChanged);

            rotationSlider = uiDocument.rootVisualElement.Q<SliderInt>("RotationSlider");
            rotationSlider.lowValue = -90;
            rotationSlider.highValue = 90;
            rotationSlider.value = 0;
            rotationSlider.RegisterCallback<ChangeEvent<int>>(evt =>
            {
                landing.SetRotation(evt.newValue);
            });
        }

        // Use this for initialization
        void Start()
        {
            Initialize();
        }

        void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
            returnButton.UnregisterCallback<ClickEvent>(ReturnClicked);
            buildButton.UnregisterCallback<ClickEvent>(BuildClicked);

            slopeSlider.UnregisterCallback<ChangeEvent<float>>(OnSlopeChanged);
            heightSlider.UnregisterCallback<ChangeEvent<float>>(OnHeightChanged);
            widthSlider.UnregisterCallback<ChangeEvent<float>>(OnWidthChanged);
            thicknessSlider.UnregisterCallback<ChangeEvent<float>>(OnThicknessChanged);
        }

        public void SetLandingElement(LandingMeshGenerator.Landing element)
        {
            landing = element;
        }

        private void BuildClicked(ClickEvent evt)
        {
            StateController.Instance.ChangeState(new DefaultState());
        }

        private void CancelClicked(ClickEvent evt)
        {
            // TODO may need to check if the last line element is really a landing
            Line.Instance.line.RemoveAt(Line.Instance.line.Count - 1);
            
            StateController.Instance.ChangeState(new TakeOffBuildState());
        }

        private void ReturnClicked(ClickEvent evt)
        {
            Line.Instance.DestroyLastLineElement();

            StateController.Instance.ChangeState(new LandingPositioningState());
        }

        private void ValidateHeight(float value)
        {
            float min = 1;
            float max = 10;
            heightSlider.lowValue = min;
            heightSlider.highValue = max;
            heightSlider.value = Mathf.Clamp(value, min, max);
        }

        private void ValidateWidth(float value)
        {
            float min = heightSlider.value;
            float max = 5;
            widthSlider.lowValue = min;
            widthSlider.highValue = max;
            widthSlider.value = Mathf.Clamp(value, min, max);
        }

        private void ValidateThickness(float value)
        {
            float min = 1;
            float max = 2.5f;
            thicknessSlider.lowValue = min;
            thicknessSlider.highValue = max;
            thicknessSlider.value = Mathf.Clamp(value, min, max);
        }

        private void OnSlopeChanged(ChangeEvent<float> evt)
        {
            landing.SetSlope(evt.newValue);
            ValidateHeight(heightSlider.value);
            ValidateWidth(widthSlider.value);
            ValidateThickness(thicknessSlider.value);
        }

        private void OnHeightChanged(ChangeEvent<float> evt)
        {
            ValidateHeight(evt.newValue);
            landing.SetHeight(heightSlider.value);

            ValidateWidth(widthSlider.value);
            ValidateThickness(thicknessSlider.value);
        }

        private void OnWidthChanged(ChangeEvent<float> evt)
        {
            ValidateWidth(evt.newValue);
            landing.SetWidth(widthSlider.value);
        }

        private void OnThicknessChanged(ChangeEvent<float> evt)
        {
            ValidateThickness(evt.newValue);
            landing.SetThickness(thicknessSlider.value);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}