using System;
using System.Collections.Generic;
using LineSystem;
using Managers;
using Obstacles;
using Obstacles.Landing;
using Obstacles.TakeOff;
using States;
using TerrainEditing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{    

    public class LandingBuildUI : PositionUI
    {        
        private Button cancelButton;
        private Button returnButton;

        public Button BuildButton { get; private set; }       

        private ValueDisplay slopeDisplay;

        private BuilderValueControl<LandingBuilder> heightControl;

        private BuilderValueControl<LandingBuilder> widthControl;

        private BuilderValueControl<LandingBuilder> thicknessControl;

        private BuilderValueControl<LandingBuilder> rotationControl;

        private LandingBuilder builder;

        private LandingPositioner positioner;

        protected override void OnEnable()
        {
            if (BuildManager.Instance.ActiveBuilder is not LandingBuilder)
            {
                throw new Exception("Active builder is not a LandingBuilder while in landing building phase.");
            }
            else
            {
                builder = BuildManager.Instance.ActiveBuilder as LandingBuilder;
                positioner = builder.GetComponent<LandingPositioner>();
            }

            base.OnEnable();

            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            returnButton = uiDocument.rootVisualElement.Q<Button>("ReturnButton");
            BuildButton = uiDocument.rootVisualElement.Q<Button>("BuildButton");
            BuildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            returnButton.RegisterCallback<ClickEvent>(ReturnClicked);

            List<BoundDependency> noDeps = new();

            VisualElement slope = uiDocument.rootVisualElement.Q<VisualElement>("SlopeDisplay");
            slopeDisplay = new(slope, builder.GetSlopeAngle(), ValueControl.DegreeUnit);

            builder.SlopeChanged += OnSlopeChanged;

            VisualElement height = uiDocument.rootVisualElement.Q<VisualElement>("HeightControl");
            heightControl = new BuilderValueControl<LandingBuilder>(height, 0.1f, LandingSettings.MinHeight,
                LandingSettings.MaxHeight, ValueControl.MeterUnit, noDeps, builder,
            (builder, value) =>
            {
                builder.SetHeight(value);
            },
            (builder) => builder.GetHeight(),
            valueValidator: HeightValidator);

            builder.HeightChanged += OnHeightChanged;
            
            VisualElement width = uiDocument.rootVisualElement.Q<VisualElement>("WidthControl");
            widthControl = new BuilderValueControl<LandingBuilder>(width, 0.1f, LandingSettings.MinWidth,
                LandingSettings.MaxWidth, ValueControl.MeterUnit, noDeps, builder,
            (builder, value) =>
            {
                builder.SetWidth(value);
            },
            (builder) => builder.GetWidth());

            builder.WidthChanged += OnWidthChanged;

            VisualElement thickness = uiDocument.rootVisualElement.Q<VisualElement>("ThicknessControl");
            thicknessControl = new BuilderValueControl<LandingBuilder>(thickness, 0.1f, LandingSettings.MinThickness,
                LandingSettings.MaxThickness, ValueControl.MeterUnit, noDeps, builder,
            (builder, value) =>
            {
                builder.SetThickness(value);
            },
            (builder) => builder.GetThickness(),
            valueValidator: ThicknessValidator);

            builder.ThicknessChanged += OnThicknessChanged;

            VisualElement rotation = uiDocument.rootVisualElement.Q<VisualElement>("RotationControl");
            rotationControl = new BuilderValueControl<LandingBuilder>(rotation, 1, -90, 90, ValueControl.DegreeUnit,
                noDeps, builder,
            (_, value) =>
            {
                positioner.TrySetRotation(value);                
            },
            (builder) => builder.GetRotation(),
            "0.#");
        }

        private bool HeightValidator(float newValue)
        {
            builder.SetHeight(newValue);

            positioner.UpdateValidPositionList();

            if (positioner.AllowedTrajectoryPositions.Count == 0)
            {                
                StudioUIManager.Instance.ShowMessage($"No valid positions for new height value. Try lowering it or change the takeoff parameters.", 2f);
                builder.CanBuild(false);
            }
            else
            {
                builder.CanBuild(true);
            }

            return true;
        }

        private bool ThicknessValidator(float newValue)
        {
            Vector3 takeoffRideDir = builder.PairedTakeoff.GetRideDirection();

            Vector3 takeoffEdgeToBackEdge = builder.GetLandingPoint() - builder.GetRideDirection() * newValue - builder.PairedTakeoff.GetTransitionEnd();

            if (Vector3.Dot(takeoffRideDir, takeoffEdgeToBackEdge.normalized) <= 0)
            {
                StudioUIManager.Instance.ShowMessage("Cannot set new landing thickness. Its back edge would occupy the takeoffs transition.", 2f);
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
            BuildButton.UnregisterCallback<ClickEvent>(BuildClicked);

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
                TerrainManager.Instance.ActiveSlope.CancelPlacement();
            }

            Line.Instance.RemoveLastLineElement(); // has to be the takeoff, destroy it as well

            if (builder.PairedTakeoff != null)
            {
                builder.PairedTakeoff.DestroyUnderlyingGameObject();
            }

            StateController.Instance.ChangeState(new DefaultState());
        }

        private void ReturnClicked(ClickEvent evt)
        {            
            builder.Cancel();

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.CancelPlacement();
            }

            TakeoffBuilder takeoffBuilder = (Line.Instance.GetLastLineElement() as Takeoff).Revert();

            StateController.Instance.ChangeState(new TakeOffBuildState(takeoffBuilder.GetComponent<TakeoffPositioner>()));
        }        
    }
}