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

        public Button BuildButton { get; private set; }        

        private BuilderValueControl<TakeoffBuilder> radiusControl;

        private BuilderValueControl<TakeoffBuilder> heightControl;

        private BuilderValueControl<TakeoffBuilder> thicknessControl;

        private BuilderValueControl<TakeoffBuilder> widthControl;

        private ValueDisplay endAngleDisplay;

        private TakeoffBuilder builder;

        private TakeoffBuilder invisibleBuilder;

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

            invisibleBuilder = builder.InvisibleClone;            

            base.OnEnable();

            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            BuildButton = uiDocument.rootVisualElement.Q<Button>("BuildButton");
            BuildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);

            List<BoundDependency> noDeps = new();

            VisualElement thickness = uiDocument.rootVisualElement.Q<VisualElement>("ThicknessControl");
            thicknessControl = new BuilderValueControl<TakeoffBuilder>(thickness, 0.1f, 0.5f, TakeoffConstants.MAX_RADIUS / 4, ValueControl.MeterUnit, noDeps, builder,
                (builder, newVal) => builder.SetThickness(newVal),
                (builder) => builder.GetThickness());

            builder.ThicknessChanged += OnThicknessChanged;

            VisualElement width = uiDocument.rootVisualElement.Q<VisualElement>("WidthControl");
            widthControl = new BuilderValueControl<TakeoffBuilder>(width, 0.1f, TakeoffConstants.MIN_RADIUS / 7 / 1.5f, TakeoffConstants.MAX_RADIUS, ValueControl.MeterUnit, noDeps, builder,
                (builder, newVal) => builder.SetWidth(newVal),
                (builder) => builder.GetWidth());

            builder.WidthChanged += OnWidthChanged;

            List<BoundDependency> onHeightDeps = new() { new(widthControl, (newHeight) => newHeight / 1.5f, (newHeight) => newHeight * 5), new(thicknessControl, (newHeight) => newHeight / 3, (newHeight) => newHeight) };
            VisualElement height = uiDocument.rootVisualElement.Q<VisualElement>("HeightControl");
            heightControl = new BuilderValueControl<TakeoffBuilder>(height, 0.1f, TakeoffConstants.MIN_HEIGHT, TakeoffConstants.MAX_HEIGHT, ValueControl.MeterUnit, onHeightDeps, builder,
                (builder, newVal) => builder.SetHeight(newVal),
                (builder) => builder.GetHeight(),
                valueValidator: HeightValidator);

            builder.HeightChanged += OnHeightChanged;

            List<BoundDependency> onRadiusDeps = new() { new(heightControl, (newRadius) => newRadius / 7, (newRadius) => newRadius) };
            VisualElement radius = uiDocument.rootVisualElement.Q<VisualElement>("RadiusControl");
            radiusControl = new BuilderValueControl<TakeoffBuilder>(radius, 0.1f, TakeoffConstants.MIN_RADIUS, TakeoffConstants.MAX_RADIUS, ValueControl.MeterUnit, onRadiusDeps, builder,
                (builder, newVal) => builder.SetRadius(newVal),
                (builder) => builder.GetRadius(),
                valueValidator: RadiusValidator);

            builder.RadiusChanged += OnRadiusChanged;

            VisualElement endAngle = uiDocument.rootVisualElement.Q<VisualElement>("EndAngleDisplay");
            endAngleDisplay = new(endAngle, builder.GetEndAngle() * Mathf.Rad2Deg, ValueControl.DegreeUnit, "0.#");

            builder.EndAngleChanged += OnEndAngleChanged;
        }

        private void OnEndAngleChanged(object sender, ParamChangeEventArgs<float> eventArgs) => endAngleDisplay?.SetCurrentValue(eventArgs.NewValue * Mathf.Rad2Deg);

        private void OnHeightChanged(object sender, ParamChangeEventArgs<float> eventArgs) => heightControl?.SetShownValue(eventArgs.NewValue);

        private void OnWidthChanged(object sender, ParamChangeEventArgs<float> eventArgs) => widthControl?.SetShownValue(eventArgs.NewValue);

        private void OnThicknessChanged(object sender, ParamChangeEventArgs<float> eventArgs) => thicknessControl?.SetShownValue(eventArgs.NewValue);

        private void OnRadiusChanged(object sender, ParamChangeEventArgs<float> eventArgs) => radiusControl?.SetShownValue(eventArgs.NewValue);

        private bool RadiusValidator(float newValue)
        {
            float oldRadius = invisibleBuilder.GetRadius();
            invisibleBuilder.SetRadius(newValue);

            if (invisibleBuilder.GetExitSpeed() == 0)
            {
                builder.SetRadius(newValue);
                builder.CanBuild(false);
                UIManager.ToggleButton(BuildButton, false);
                UIManager.Instance.ShowMessage($"Insufficient speed to ride up the takeoffs transition with this radius. Try a greater value.", 2f);

                return true;
            }
            else if (invisibleBuilder.GetFlightDistanceXZ() < LandingConstants.MIN_DISTANCE_FROM_TAKEOFF)
            {
                // revert the radius change
                invisibleBuilder.SetRadius(oldRadius);
                UIManager.Instance.ShowMessage($"Cannot set new radius value. The flight trajectory would be shorter than {LandingConstants.MIN_DISTANCE_FROM_TAKEOFF}m.", 2f);
                return false;
            }            
            else
            {
                UIManager.ToggleButton(BuildButton, true);
                return true;
            }
        }

        private bool HeightValidator(float newValue)
        {
            float oldHeight = invisibleBuilder.GetHeight();
            invisibleBuilder.SetHeight(newValue);

            if (invisibleBuilder.GetExitSpeed() == 0)
            {

                builder.SetHeight(newValue);
                builder.CanBuild(false);
                UIManager.ToggleButton(BuildButton, false);
                UIManager.Instance.ShowMessage($"Insufficient speed to ride up the takeoffs transition with this height. Try lowering it.", 2f);

                return true;
            }
            else if (invisibleBuilder.GetFlightDistanceXZ() < LandingConstants.MIN_DISTANCE_FROM_TAKEOFF)
            {
                // revert the height change
                invisibleBuilder.SetHeight(oldHeight);
                UIManager.Instance.ShowMessage($"Cannot set new height value. The flight trajectory would be shorter than {LandingConstants.MIN_DISTANCE_FROM_TAKEOFF}m.", 2f);

                return false;
            }            
            else
            {
                UIManager.ToggleButton(BuildButton, true);
                return true;
            }
        }       


        void OnDisable()
        {
            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
            BuildButton.UnregisterCallback<ClickEvent>(BuildClicked);    
            
            builder.HeightChanged -= OnHeightChanged;
            builder.WidthChanged -= OnWidthChanged;
            builder.ThicknessChanged -= OnThicknessChanged;
            builder.RadiusChanged -= OnRadiusChanged;  
            builder.EndAngleChanged -= OnEndAngleChanged;
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
