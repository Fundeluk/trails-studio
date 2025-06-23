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

        private const float MIN_SLOPE = 30;
        private const float MAX_SLOPE = 70;

        private const float MIN_HEIGHT = 1;
        private const float MAX_HEIGHT = 6;

        private const float MIN_WIDTH = 2;
        private const float MAX_WIDTH = 7;

        private const float MIN_THICKNESS = 1;
        private const float MAX_THICKNESS = 2.5f;

        private BuilderValueControl<LandingBuilder> slopeControl;

        private BuilderValueControl<LandingBuilder> heightControl;

        private BuilderValueControl<LandingBuilder> widthControl;

        private BuilderValueControl<LandingBuilder> thicknessControl;

        private BuilderValueControl<LandingBuilder> rotationControl;

        private LandingBuilder builder;

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

            base.OnEnable();

            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            returnButton = uiDocument.rootVisualElement.Q<Button>("ReturnButton");
            buildButton = uiDocument.rootVisualElement.Q<Button>("BuildButton");
            buildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            returnButton.RegisterCallback<ClickEvent>(ReturnClicked);

            List<BoundDependency> noDeps = new();

            VisualElement slope = uiDocument.rootVisualElement.Q<VisualElement>("SlopeControl");
            slopeControl = new BuilderValueControl<LandingBuilder>(slope, 1, MIN_SLOPE, MAX_SLOPE, ValueControl.DegreeUnit, noDeps, builder,
            (builder, value) =>
            {
                builder.SetSlope(value * Mathf.Deg2Rad);
            },
            (builder) => builder.GetSlope() * Mathf.Rad2Deg);

            builder.SlopeChanged += OnSlopeChanged;



            List<BoundDependency> onHeightDeps = new() { new (slopeControl, (newHeight) => MIN_SLOPE + newHeight*6, (newHeight) => Mathf.Min(MIN_SLOPE + newHeight * 15, MAX_SLOPE) )};
            VisualElement height = uiDocument.rootVisualElement.Q<VisualElement>("HeightControl");
            heightControl = new BuilderValueControl<LandingBuilder>(height, 0.1f, MIN_HEIGHT, MAX_HEIGHT, ValueControl.MeterUnit, onHeightDeps, builder,
            (builder, value) =>
            {
                builder.SetHeight(value);
            },
            (builder) => builder.GetHeight());

            builder.HeightChanged += OnHeightChanged;

            List<BoundDependency> onWidthDeps = new() { new(heightControl, (newWidth) => Mathf.Max(newWidth / 1.5f, MIN_HEIGHT), (newWidth) => Mathf.Min(newWidth * 5, MAX_HEIGHT)) };
            VisualElement width = uiDocument.rootVisualElement.Q<VisualElement>("WidthControl");
            widthControl = new BuilderValueControl<LandingBuilder>(width, 0.1f, MIN_WIDTH, MAX_WIDTH, ValueControl.MeterUnit, noDeps, builder,
            (builder, value) =>
            {
                builder.SetWidth(value);
            },
            (builder) => builder.GetWidth());

            builder.WidthChanged += OnWidthChanged;

            VisualElement thickness = uiDocument.rootVisualElement.Q<VisualElement>("ThicknessControl");
            thicknessControl = new BuilderValueControl<LandingBuilder>(thickness, 0.1f, MIN_THICKNESS, MAX_THICKNESS, ValueControl.MeterUnit, noDeps, builder,
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
        
        private void OnHeightChanged(object sender, ParamChangeEventArgs<float> eventArgs) => heightControl?.SetShownValue(eventArgs.NewValue );

        private void OnWidthChanged(object sender, ParamChangeEventArgs<float> eventArgs) => widthControl?.SetShownValue(eventArgs.NewValue );

        private void OnThicknessChanged(object sender, ParamChangeEventArgs<float> eventArgs) => thicknessControl?.SetShownValue(eventArgs.NewValue );

        private void OnSlopeChanged(object sender, ParamChangeEventArgs<float> eventArgs) => slopeControl?.SetShownValue(eventArgs.NewValue * Mathf.Rad2Deg);



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