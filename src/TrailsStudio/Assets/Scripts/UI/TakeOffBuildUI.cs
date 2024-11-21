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
    public class TakeOffBuildUI : MonoBehaviour
    {
        private Button cancelButton;
        private Button returnButton;

        private Slider heightSlider;

        private Slider radiusSlider;
        private const float MIN_RADIUS = 1;
        private const float MAX_RADIUS = 10;

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
            radiusSlider.UnregisterValueChangedCallback(OnRadiusChanged);
            heightSlider.UnregisterValueChangedCallback(OnHeightChanged);
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

        private void OnRadiusChanged(ChangeEvent<float> evt)
        {
            takeoff.SetRadius(evt.newValue);
            ValidateHeight(heightSlider.value);
        }

        private void OnHeightChanged(ChangeEvent<float> evt)
        {
            ValidateHeight(evt.newValue);
            takeoff.SetHeight(heightSlider.value);
        }

    }
}
