using Assets.Scripts.Builders;
using Assets.Scripts.Builders.Slope;
using Assets.Scripts.Managers;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class SlopeBuildUI : PositionUI
	{
        private Button cancelButton;

        public Button BuildButton { get; private set; }

        private SlopeChangeBuilder slopeBuilder;

        private BuilderValueControl<SlopeChangeBuilder> slopeHeightControl;
        private BuilderValueControl<SlopeChangeBuilder> slopeLengthControl;        

        protected override void OnEnable()
        {
            if (TerrainManager.Instance.ActiveSlope == null)
            {
                throw new System.Exception("Slope must be created from the terrain first.");
            }

            base.OnEnable();

            slopeBuilder = TerrainManager.Instance.ActiveSlope.GetComponent<SlopeChangeBuilder>();
            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            BuildButton = uiDocument.rootVisualElement.Q<Button>("BuildButton");
            BuildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);

            List<BoundDependency> noDeps = new();

            VisualElement slopeHeight = uiDocument.rootVisualElement.Q<VisualElement>("SlopeHeightControl");
            slopeHeightControl = new BuilderValueControl<SlopeChangeBuilder>(slopeHeight, 0.1f, SlopeSettings.MIN_HEIGHT_DIFFERENCE, SlopeSettings.MAX_HEIGHT_DIFFERENCE, ValueControl.MeterUnit, noDeps, slopeBuilder,
                (slopeChange, newVal) => slopeChange.SetHeightDifference(newVal),
                (slopeChange) => slopeChange.HeightDifference,
                valueValidator: HeightDiffValidator);

            VisualElement slopeLength = uiDocument.rootVisualElement.Q<VisualElement>("SlopeLengthControl");
            slopeLengthControl = new BuilderValueControl<SlopeChangeBuilder>(slopeLength, 0.2f, SlopeSettings.MIN_LENGTH, SlopeSettings.MAX_LENGTH, ValueControl.MeterUnit, noDeps, slopeBuilder,
                (slopeChange, newVal) => slopeChange.SetLength(newVal),
                (slopeChange) => slopeChange.Length);

            VisualElement angleDisplay = uiDocument.rootVisualElement.Q<VisualElement>("AngleDisplay");
            ValueDisplay angleValueDisplay = new(angleDisplay, slopeBuilder.Angle, ValueControl.DegreeUnit, "0");

            slopeHeightControl.ValueChanged += (s, e) =>
            {
                angleValueDisplay.SetCurrentValue(slopeBuilder.Angle * Mathf.Rad2Deg);
            };
            slopeLengthControl.ValueChanged += (s, e) =>
            {
                angleValueDisplay.SetCurrentValue(slopeBuilder.Angle * Mathf.Rad2Deg);
            };

            StudioUIManager.ToggleButton(BuildButton, false);

            slopeBuilder.HeightDiffChanged += OnParamChanged;
            slopeBuilder.LengthChanged += OnParamChanged;
            slopeBuilder.PositionChanged += OnParamChanged;

        }

        private bool HeightDiffValidator(float newValue)
        {
            float newGlobalHeight = TerrainManager.Instance.GlobalHeightLevel + newValue;
            float maxHeight = TerrainManager.maxHeight;
            if (newGlobalHeight < -maxHeight)
            {
                StudioUIManager.Instance.ShowMessage($"Cannot make the terrain go lower than {-maxHeight}m.", 3f);
                return false;
            }
            else if (newGlobalHeight > maxHeight)
            {
                StudioUIManager.Instance.ShowMessage($"Cannot make the terrain go higher than {maxHeight}m.", 3f);
                return false;
            }

            return true;
        }

        private void OnParamChanged<T>(object sender, ParamChangeEventArgs<T> args)
        {
            if (slopeBuilder.IsValid())
            {
                StudioUIManager.ToggleButton(BuildButton, true);
            }
            else
            {
                StudioUIManager.ToggleButton(BuildButton, false);
            }

            slopeHeightControl?.SetShownValue(slopeBuilder.HeightDifference);
            slopeLengthControl?.SetShownValue(slopeBuilder.Length);
        }
        
        
        private void BuildClicked(ClickEvent evt)
        {            
            slopeBuilder.Build();
            StateController.Instance.ChangeState(new DefaultState());
        }

        private void CancelClicked(ClickEvent evt)
        {
            slopeBuilder.DestroyUnderlyingGameObject();
            StateController.Instance.ChangeState(new DefaultState());
        }
        

        private void OnDisable()
        {
            slopeBuilder.HeightDiffChanged -= OnParamChanged;
            slopeBuilder.LengthChanged -= OnParamChanged;
            slopeBuilder.PositionChanged -= OnParamChanged;

            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
            BuildButton.UnregisterCallback<ClickEvent>(BuildClicked);
        }        
	}
}