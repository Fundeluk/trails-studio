using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.Builders;
using Assets.Scripts.States;
using System.Collections.Generic;
using Assets.Scripts.Managers;
using System;

namespace Assets.Scripts.UI
{    

    public class LandingBuildUI : PositionUI
    {        
        private Button cancelButton;
        private Button returnButton;

        private Button buildButton;       

        // TODO slope is set by the trajectory, just show its value

        private ValueDisplay slopeDisplay;

        private BuilderValueControl<LandingBuilder> heightControl;

        private BuilderValueControl<LandingBuilder> widthControl;

        private BuilderValueControl<LandingBuilder> thicknessControl;

        private BuilderValueControl<LandingBuilder> rotationControl;

        private LandingBuilder builder;

        private LandingBuilder invisibleBuilder;

        private LandingPositioner positioner;

        protected override void OnEnable()
        {
            if (BuildManager.Instance.activeBuilder is not LandingBuilder)
            {
                throw new Exception("Active builder is not a LandingBuilder while in landing building phase.");
            }
            else
            {
                builder = BuildManager.Instance.activeBuilder as LandingBuilder;
                positioner = builder.GetComponent<LandingPositioner>();
            }

            invisibleBuilder = builder.InvisibleClone;

            base.OnEnable();

            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            returnButton = uiDocument.rootVisualElement.Q<Button>("ReturnButton");
            buildButton = uiDocument.rootVisualElement.Q<Button>("BuildButton");
            buildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            returnButton.RegisterCallback<ClickEvent>(ReturnClicked);

            List<BoundDependency> noDeps = new();

            VisualElement slope = uiDocument.rootVisualElement.Q<VisualElement>("SlopeDisplay");
            slopeDisplay = new(slope, builder.GetSlopeAngle(), ValueControl.DegreeUnit);

            builder.SlopeChanged += OnSlopeChanged;

            VisualElement height = uiDocument.rootVisualElement.Q<VisualElement>("HeightControl");
            heightControl = new BuilderValueControl<LandingBuilder>(height, 0.1f, LandingConstants.MIN_HEIGHT, LandingConstants.MAX_HEIGHT, ValueControl.MeterUnit, noDeps, builder,
            (builder, value) =>
            {
                builder.SetHeight(value);
            },
            (builder) => builder.GetHeight(),
            valueValidator: HeightValidator);

            builder.HeightChanged += OnHeightChanged;

            List<BoundDependency> onWidthDeps = new() { new(heightControl, (newWidth) => Mathf.Max(newWidth / 1.5f, LandingConstants.MIN_HEIGHT), (newWidth) => Mathf.Min(newWidth * 5, LandingConstants.MAX_HEIGHT)) };
            VisualElement width = uiDocument.rootVisualElement.Q<VisualElement>("WidthControl");
            widthControl = new BuilderValueControl<LandingBuilder>(width, 0.1f, LandingConstants.MIN_WIDTH, LandingConstants.MAX_WIDTH, ValueControl.MeterUnit, noDeps, builder,
            (builder, value) =>
            {
                builder.SetWidth(value);
            },
            (builder) => builder.GetWidth());

            builder.WidthChanged += OnWidthChanged;

            VisualElement thickness = uiDocument.rootVisualElement.Q<VisualElement>("ThicknessControl");
            thicknessControl = new BuilderValueControl<LandingBuilder>(thickness, 0.1f, LandingConstants.MIN_THICKNESS, LandingConstants.MAX_THICKNESS, ValueControl.MeterUnit, noDeps, builder,
            (builder, value) =>
            {
                builder.SetThickness(value);
            },
            (builder) => builder.GetThickness());

            builder.ThicknessChanged += OnThicknessChanged;

            VisualElement rotation = uiDocument.rootVisualElement.Q<VisualElement>("RotationControl");
            rotationControl = new BuilderValueControl<LandingBuilder>(rotation, 1, -90, 90, ValueControl.DegreeUnit, noDeps, builder,
            (builder, value) =>
            {
                builder.CanBuild(positioner.TrySetRotation(value));                
            },
            (builder) => builder.GetRotation(),
            "0.#");
        }

        private bool HeightValidator(float newValue)
        {
            float oldHeight = builder.GetHeight();
            invisibleBuilder.SetHeight(newValue);

            if (positioner.CalculateValidLandingPositions().Count == 0)
            {
                // revert the height change
                invisibleBuilder.SetHeight(oldHeight);
                UIManager.Instance.ShowMessage($"Cannot set new height value. The landing would not fit for the trajectory.", 2f);
                return false;
            }
            else
            {
                return true;
            }
        }
        
        private void OnHeightChanged(object sender, ParamChangeEventArgs<float> eventArgs) => heightControl?.SetShownValue(eventArgs.NewValue );

        private void OnWidthChanged(object sender, ParamChangeEventArgs<float> eventArgs) => widthControl?.SetShownValue(eventArgs.NewValue );

        private void OnThicknessChanged(object sender, ParamChangeEventArgs<float> eventArgs) => thicknessControl?.SetShownValue(eventArgs.NewValue );

        private void OnSlopeChanged(object sender, ParamChangeEventArgs<float> eventArgs) => slopeDisplay?.SetCurrentValue(eventArgs.NewValue * Mathf.Rad2Deg);

        private void OnDisable()
        {
            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
            returnButton.UnregisterCallback<ClickEvent>(ReturnClicked);
            buildButton.UnregisterCallback<ClickEvent>(BuildClicked);

            builder.SlopeChanged -= OnSlopeChanged;
            builder.HeightChanged -= OnHeightChanged;
            builder.WidthChanged -= OnWidthChanged;
            builder.ThicknessChanged -= OnThicknessChanged;
        }

        private void BuildClicked(ClickEvent evt)
        {
            builder.Build();            
            StateController.Instance.ChangeState(new DefaultState());
        }

        private void CancelClicked(ClickEvent evt)
        {
            builder.Cancel();
            builder.DestroyUnderlyingGameObject();

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.LastConfirmedSnapshot.Revert();
            }

            Line.Instance.DestroyLastLineElement(); // has to be the takeoff, destroy it as well

            StateController.Instance.ChangeState(new DefaultState());
        }

        private void ReturnClicked(ClickEvent evt)
        {            
            builder.Cancel();

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.LastConfirmedSnapshot.Revert();
            }

            TakeoffBuilder takeoffBuilder = (Line.Instance.GetLastLineElement() as Takeoff).Revert();

            StateController.Instance.ChangeState(new TakeOffBuildState(takeoffBuilder.GetComponent<TakeoffPositioner>()));
        }        
    }
}