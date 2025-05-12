using Assets.Scripts.States;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.Builders;
using Assets.Scripts.Managers;

namespace Assets.Scripts.UI
{
    public class TakeoffControl : ValueControl
    {
        private readonly TakeoffBuilder builder;
        public TakeoffControl(VisualElement root, float increment, float minValue, float maxValue, float currentValue, string unit, List<BoundDependency> dependencies, TakeoffBuilder builder,
            TakeoffValueSetter takeoffSetter)
            : base(root, increment, minValue, maxValue, unit, dependencies)
        {
            this.builder = builder;
            this.takeoffSetter = takeoffSetter;
            this.currentValue = currentValue;
            UpdateShownValue();
        }

        public delegate void TakeoffValueSetter(TakeoffBuilder builder, float value);
        public readonly TakeoffValueSetter takeoffSetter;

        public override void SetCurrentValue(float value)
        {
            base.SetCurrentValue(value);

            takeoffSetter(builder, currentValue);
        }
    }



    public class TakeOffBuildUI : MonoBehaviour
    {
        public const string MeterUnit = "m";

        private Button cancelButton;

        private Button buildButton;

        private const float MIN_RADIUS = 1;
        private const float MAX_RADIUS = 10;        

        private TakeoffControl radiusControl;

        private TakeoffControl heightControl;

        private TakeoffControl thicknessControl;

        private TakeoffControl widthControl;

        private ValueDisplay endAngleDisplay;

        private TakeoffBuilder builder;

        private void OnEnable()
        {
            if (BuildManager.Instance.activeBuilder is not TakeoffBuilder)
            {
                throw new Exception("Active builder is not a TakeoffBuilder while in takeoff building phase.");
            }
            else
            {
                builder = BuildManager.Instance.activeBuilder as TakeoffBuilder;
            }


            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            buildButton = uiDocument.rootVisualElement.Q<Button>("BuildButton");
            buildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);

            List<BoundDependency> noDeps = new();

            VisualElement thickness = uiDocument.rootVisualElement.Q<VisualElement>("ThicknessControl");
            thicknessControl = new TakeoffControl(thickness, 0.1f, 0.5f, MAX_RADIUS / 4, builder.GetThickness(), MeterUnit, noDeps, builder, (builder, newVal) => builder.SetThickness(newVal));
            
            VisualElement width = uiDocument.rootVisualElement.Q<VisualElement>("WidthControl");
            widthControl = new TakeoffControl(width, 0.1f, MIN_RADIUS / 7 / 1.5f, MAX_RADIUS, builder.GetWidth(), MeterUnit, noDeps, builder, (builder, newVal) => builder.SetWidth(newVal));

            List<BoundDependency> onHeightDeps = new() { new(widthControl, (newHeight) => newHeight / 1.5f, (newHeight) => newHeight * 5), new(thicknessControl, (newHeight) => newHeight / 3, (newHeight) => newHeight) };
            VisualElement height = uiDocument.rootVisualElement.Q<VisualElement>("HeightControl");
            heightControl = new TakeoffControl(height, 0.1f, MIN_RADIUS / 7, MAX_RADIUS, builder.GetHeight(), MeterUnit, onHeightDeps, builder, (builder, newVal) => builder.SetHeight(newVal));

            List<BoundDependency> onRadiusDeps = new() { new(heightControl, (newRadius) => newRadius / 7, (newRadius) => newRadius) };
            VisualElement radius = uiDocument.rootVisualElement.Q<VisualElement>("RadiusControl");
            radiusControl = new TakeoffControl(radius, 0.1f, MIN_RADIUS, MAX_RADIUS, builder.GetRadius(), MeterUnit, onRadiusDeps, builder, (builder, newVal) => builder.SetRadius(newVal));

            VisualElement endAngle = uiDocument.rootVisualElement.Q<VisualElement>("EndAngleDisplay");
            endAngleDisplay = new(endAngle, builder.GetEndAngle() * Mathf.Rad2Deg, "°");

            radiusControl.ValueChanged += (s, e) => { endAngleDisplay.SetCurrentValue(builder.GetEndAngle() * Mathf.Rad2Deg); };
            heightControl.ValueChanged += (s, e) => { endAngleDisplay.SetCurrentValue(builder.GetEndAngle() * Mathf.Rad2Deg); };
        }         

        void OnDisable()
        {
            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
            buildButton.UnregisterCallback<ClickEvent>(BuildClicked);         
        }

        private void BuildClicked(ClickEvent evt)
        {
            builder.Build();
            StateController.Instance.ChangeState(new LandingPositioningState());
        }

        private void CancelClicked(ClickEvent evt)
        {
            // destroy the takeoff currently being built
            builder.Cancel();
            StateController.Instance.ChangeState(new DefaultState());
        }
    }
}
