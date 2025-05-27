using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.Builders;
using Assets.Scripts.States;
using System.Collections.Generic;
using Assets.Scripts.Managers;
using System;

namespace Assets.Scripts.UI
{
    public class LandingControl : ValueControl
    {
        private readonly LandingBuilder builder;
        public LandingControl(VisualElement root, float increment, float minValue, float maxValue, float currentValue, string unit, List<BoundDependency> dependencies, LandingBuilder builder,
            LandingValueSetter landingSetter)
            : base(root, increment, minValue, maxValue, unit, dependencies)
        {
            this.builder = builder;
            this.landingSetter = landingSetter;
            this.currentValue = currentValue;
            UpdateShownValue();
        }
        public delegate void LandingValueSetter(LandingBuilder builder, float value);
        public readonly LandingValueSetter landingSetter;
        public override void SetCurrentValue(float value)
        {
            base.SetCurrentValue(value);
            landingSetter(builder, currentValue);
        }
    }

    public class LandingBuildUI : MonoBehaviour
    {
        public const string MeterUnit = "m";
        public const string DegreeUnit = "°";

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

        private LandingControl slopeControl;

        private LandingControl heightControl;

        private LandingControl widthControl;

        private LandingControl thicknessControl;

        private LandingControl rotationControl;

        private LandingBuilder builder;

        void OnEnable()
        {
            if (BuildManager.Instance.activeBuilder is not LandingBuilder)
            {
                throw new Exception("Active builder is not a LandingBuilder while in landing building phase.");
            }
            else
            {
                builder = BuildManager.Instance.activeBuilder as LandingBuilder;
            }

            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            returnButton = uiDocument.rootVisualElement.Q<Button>("ReturnButton");
            buildButton = uiDocument.rootVisualElement.Q<Button>("BuildButton");
            buildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            returnButton.RegisterCallback<ClickEvent>(ReturnClicked);

            List<BoundDependency> noDeps = new();

            VisualElement slope = uiDocument.rootVisualElement.Q<VisualElement>("SlopeControl");
            slopeControl = new LandingControl(slope, 1, MIN_SLOPE, MAX_SLOPE, builder.GetSlope() * Mathf.Rad2Deg, DegreeUnit, noDeps, builder, (builder, value) =>
            {
                builder.SetSlope(value * Mathf.Deg2Rad);
            });


            List<BoundDependency> onHeightDeps = new() { new (slopeControl, (newHeight) => MIN_SLOPE + newHeight*6, (newHeight) => Mathf.Min(MIN_SLOPE + newHeight * 15, MAX_SLOPE) )};
            VisualElement height = uiDocument.rootVisualElement.Q<VisualElement>("HeightControl");
            heightControl = new LandingControl(height, 0.1f, MIN_HEIGHT, MAX_HEIGHT, builder.GetHeight(), MeterUnit, onHeightDeps, builder, (builder, value) =>
            {
                builder.SetHeight(value);
            });

            List<BoundDependency> onWidthDeps = new() { new(heightControl, (newWidth) => Mathf.Max(newWidth / 1.5f, MIN_HEIGHT), (newWidth) => Mathf.Min(newWidth * 5, MAX_HEIGHT)) };
            VisualElement width = uiDocument.rootVisualElement.Q<VisualElement>("WidthControl");
            widthControl = new LandingControl(width, 0.1f, MIN_WIDTH, MAX_WIDTH, builder.GetWidth(), MeterUnit, noDeps, builder, (builder, value) =>
            {
                builder.SetWidth(value);
            });

            VisualElement thickness = uiDocument.rootVisualElement.Q<VisualElement>("ThicknessControl");
            thicknessControl = new LandingControl(thickness, 0.1f, MIN_THICKNESS, MAX_THICKNESS, builder.GetThickness(), MeterUnit, noDeps, builder, (builder, value) =>
            {
                builder.SetThickness(value);
            });

            VisualElement rotation = uiDocument.rootVisualElement.Q<VisualElement>("RotationControl");
            rotationControl = new LandingControl(rotation, 1, -90, 90, builder.GetRotation(), DegreeUnit, noDeps, builder, (builder, value) =>
            {
                builder.SetRotation(value);
            });
        }       
        

        private void OnDisable()
        {
            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
            returnButton.UnregisterCallback<ClickEvent>(ReturnClicked);
            buildButton.UnregisterCallback<ClickEvent>(BuildClicked);
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