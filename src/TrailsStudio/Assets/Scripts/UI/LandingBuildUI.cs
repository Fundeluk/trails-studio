using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.Builders;
using Assets.Scripts.States;
using System.Collections.Generic;
using Assets.Scripts.Managers;
using System;

namespace Assets.Scripts.UI
{    

    public class LandingBuildUI : MonoBehaviour
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

        private ObstacleValueControl<LandingBuilder> slopeControl;

        private ObstacleValueControl<LandingBuilder> heightControl;

        private ObstacleValueControl<LandingBuilder> widthControl;

        private ObstacleValueControl<LandingBuilder> thicknessControl;

        private ObstacleValueControl<LandingBuilder> rotationControl;

        private LandingBuilder builder;

        private LandingPositioner positioner;

        void OnEnable()
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

            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            returnButton = uiDocument.rootVisualElement.Q<Button>("ReturnButton");
            buildButton = uiDocument.rootVisualElement.Q<Button>("BuildButton");
            buildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            returnButton.RegisterCallback<ClickEvent>(ReturnClicked);

            List<BoundDependency> noDeps = new();

            VisualElement slope = uiDocument.rootVisualElement.Q<VisualElement>("SlopeControl");
            slopeControl = new ObstacleValueControl<LandingBuilder>(slope, 1, MIN_SLOPE, MAX_SLOPE, ValueControl.DegreeUnit, noDeps, builder,
            (builder, value) =>
            {
                builder.SetSlope(value * Mathf.Deg2Rad);
            },
            (builder) => builder.GetSlope() * Mathf.Rad2Deg);


            List<BoundDependency> onHeightDeps = new() { new (slopeControl, (newHeight) => MIN_SLOPE + newHeight*6, (newHeight) => Mathf.Min(MIN_SLOPE + newHeight * 15, MAX_SLOPE) )};
            VisualElement height = uiDocument.rootVisualElement.Q<VisualElement>("HeightControl");
            heightControl = new ObstacleValueControl<LandingBuilder>(height, 0.1f, MIN_HEIGHT, MAX_HEIGHT, ValueControl.MeterUnit, onHeightDeps, builder,
            (builder, value) =>
            {
                builder.SetHeight(value);
            },
            (builder) => builder.GetHeight());

            List<BoundDependency> onWidthDeps = new() { new(heightControl, (newWidth) => Mathf.Max(newWidth / 1.5f, MIN_HEIGHT), (newWidth) => Mathf.Min(newWidth * 5, MAX_HEIGHT)) };
            VisualElement width = uiDocument.rootVisualElement.Q<VisualElement>("WidthControl");
            widthControl = new ObstacleValueControl<LandingBuilder>(width, 0.1f, MIN_WIDTH, MAX_WIDTH, ValueControl.MeterUnit, noDeps, builder,
            (builder, value) =>
            {
                builder.SetWidth(value);
            },
            (builder) => builder.GetWidth());

            VisualElement thickness = uiDocument.rootVisualElement.Q<VisualElement>("ThicknessControl");
            thicknessControl = new ObstacleValueControl<LandingBuilder>(thickness, 0.1f, MIN_THICKNESS, MAX_THICKNESS, ValueControl.MeterUnit, noDeps, builder,
            (builder, value) =>
            {
                builder.SetThickness(value);
            },
            (builder) => builder.GetThickness());

            VisualElement rotation = uiDocument.rootVisualElement.Q<VisualElement>("RotationControl");
            rotationControl = new ObstacleValueControl<LandingBuilder>(rotation, 1, -90, 90, ValueControl.DegreeUnit, noDeps, builder,
            (builder, value) =>
            {
                positioner.TrySetRotation(value);
            },
            (builder) => builder.GetRotation(),
            "0.#");
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