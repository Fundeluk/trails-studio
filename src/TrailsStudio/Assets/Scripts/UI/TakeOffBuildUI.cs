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
    public class TakeOffBuildUI : PositionUI
    {
        private Button cancelButton;

        private Button buildButton;

        private const float MIN_RADIUS = 1;
        private const float MAX_RADIUS = 10;        

        private BuilderValueControl<TakeoffBuilder> radiusControl;

        private BuilderValueControl<TakeoffBuilder> heightControl;

        private BuilderValueControl<TakeoffBuilder> thicknessControl;

        private BuilderValueControl<TakeoffBuilder> widthControl;

        private ValueDisplay endAngleDisplay;

        private TakeoffBuilder builder;

        protected override void OnEnable()
        {
            if (BuildManager.Instance.activeBuilder is not TakeoffBuilder)
            {
                throw new Exception("Active builder is not a TakeoffBuilder while in takeoff building phase.");
            }
            else
            {
                builder = BuildManager.Instance.activeBuilder as TakeoffBuilder;
            }

            base.OnEnable();

            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            buildButton = uiDocument.rootVisualElement.Q<Button>("BuildButton");
            buildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);

            List<BoundDependency> noDeps = new();

            VisualElement thickness = uiDocument.rootVisualElement.Q<VisualElement>("ThicknessControl");
            thicknessControl = new BuilderValueControl<TakeoffBuilder>(thickness, 0.1f, 0.5f, MAX_RADIUS / 4, ValueControl.MeterUnit, noDeps, builder,
                (builder, newVal) => builder.SetThickness(newVal),
                (builder) => builder.GetThickness());

            builder.ThicknessChanged += OnThicknessChanged;

            VisualElement width = uiDocument.rootVisualElement.Q<VisualElement>("WidthControl");
            widthControl = new BuilderValueControl<TakeoffBuilder>(width, 0.1f, MIN_RADIUS / 7 / 1.5f, MAX_RADIUS, ValueControl.MeterUnit, noDeps, builder,
                (builder, newVal) => builder.SetWidth(newVal),
                (builder) => builder.GetWidth());

            builder.WidthChanged += OnWidthChanged;

            List<BoundDependency> onHeightDeps = new() { new(widthControl, (newHeight) => newHeight / 1.5f, (newHeight) => newHeight * 5), new(thicknessControl, (newHeight) => newHeight / 3, (newHeight) => newHeight) };
            VisualElement height = uiDocument.rootVisualElement.Q<VisualElement>("HeightControl");
            heightControl = new BuilderValueControl<TakeoffBuilder>(height, 0.1f, MIN_RADIUS / 7, MAX_RADIUS, ValueControl.MeterUnit, onHeightDeps, builder,
                (builder, newVal) => builder.SetHeight(newVal),
                (builder) => builder.GetHeight());

            builder.HeightChanged += OnHeightChanged;

            List<BoundDependency> onRadiusDeps = new() { new(heightControl, (newRadius) => newRadius / 7, (newRadius) => newRadius) };
            VisualElement radius = uiDocument.rootVisualElement.Q<VisualElement>("RadiusControl");
            radiusControl = new BuilderValueControl<TakeoffBuilder>(radius, 0.1f, MIN_RADIUS, MAX_RADIUS, ValueControl.MeterUnit, onRadiusDeps, builder,
                (builder, newVal) => builder.SetRadius(newVal),
                (builder) => builder.GetRadius());

            builder.RadiusChanged += OnRadiusChanged;

            VisualElement endAngle = uiDocument.rootVisualElement.Q<VisualElement>("EndAngleDisplay");
            endAngleDisplay = new(endAngle, builder.GetEndAngle() * Mathf.Rad2Deg, ValueControl.DegreeUnit, "0.#");

            radiusControl.ValueChanged += (s, e) => { endAngleDisplay.SetCurrentValue(builder.GetEndAngle() * Mathf.Rad2Deg); };
            heightControl.ValueChanged += (s, e) => { endAngleDisplay.SetCurrentValue(builder.GetEndAngle() * Mathf.Rad2Deg); };
        }

        private void OnHeightChanged(object sender, ParamChangeEventArgs<float> eventArgs) => heightControl?.SetShownValue(eventArgs.NewValue);

        private void OnWidthChanged(object sender, ParamChangeEventArgs<float> eventArgs) => widthControl?.SetShownValue(eventArgs.NewValue);

        private void OnThicknessChanged(object sender, ParamChangeEventArgs<float> eventArgs) => thicknessControl?.SetShownValue(eventArgs.NewValue);

        private void OnRadiusChanged(object sender, ParamChangeEventArgs<float> eventArgs) => radiusControl?.SetShownValue(eventArgs.NewValue);

        void OnDisable()
        {
            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
            buildButton.UnregisterCallback<ClickEvent>(BuildClicked);    
            
            builder.HeightChanged -= OnHeightChanged;
            builder.WidthChanged -= OnWidthChanged;
            builder.ThicknessChanged -= OnThicknessChanged;
            builder.RadiusChanged -= OnRadiusChanged;
        }

        private void BuildClicked(ClickEvent evt)
        {
            builder.Build();
            StateController.Instance.ChangeState(new LandingBuildState());
        }

        private void CancelClicked(ClickEvent evt)
        {
            // destroy the takeoff currently being built
            builder.Cancel();
            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.LastConfirmedSnapshot.Revert();
            }
            StateController.Instance.ChangeState(new DefaultState());
        }
    }
}
