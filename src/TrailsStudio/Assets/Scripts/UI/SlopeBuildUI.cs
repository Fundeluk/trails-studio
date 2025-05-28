using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using Assets.Scripts.Managers;
using Assets.Scripts.Builders;

namespace Assets.Scripts.UI
{   

    public class SlopeBuildUI : MonoBehaviour
	{
        public const float MAX_HEIGHT_DIFFERENCE = 10;
        public const float MIN_HEIGHT_DIFFERENCE = -10;
        public const float MAX_LENGTH = 100;
        public const float MIN_LENGTH = 0;

        private Button cancelButton;
        private Button returnButton;

        private Button buildButton;

        private SlopeChangeBuilder slopeBuilder;

        private ObstacleValueControl<SlopeChangeBuilder> slopeHeightControl;
        private ObstacleValueControl<SlopeChangeBuilder> slopeLengthControl;        

        public void Init(SlopeChangeBuilder slopeBuilder)
        {
            this.slopeBuilder = slopeBuilder;
            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            returnButton = uiDocument.rootVisualElement.Q<Button>("ReturnButton");
            buildButton = uiDocument.rootVisualElement.Q<Button>("BuildButton");
            buildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            returnButton.RegisterCallback<ClickEvent>(ReturnClicked);

            List<BoundDependency> noDeps = new();

            VisualElement slopeHeight = uiDocument.rootVisualElement.Q<VisualElement>("SlopeHeightControl");
            slopeHeightControl = new ObstacleValueControl<SlopeChangeBuilder>(slopeHeight, 0.1f, MIN_HEIGHT_DIFFERENCE, MAX_HEIGHT_DIFFERENCE, ValueControl.MeterUnit, noDeps, slopeBuilder,
                (slopeChange, newVal) => slopeChange.SetHeightDifference(newVal),
                (slopeChange) => slopeChange.GetHeightDifference());

            VisualElement slopeLength = uiDocument.rootVisualElement.Q<VisualElement>("SlopeLengthControl");
            slopeLengthControl = new ObstacleValueControl<SlopeChangeBuilder>(slopeLength, 0.2f, MIN_LENGTH, MAX_LENGTH, ValueControl.MeterUnit, noDeps, slopeBuilder,
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
        }

        private void BuildClicked(ClickEvent evt)
        {
            if (slopeHeightControl.GetCurrentValue() == 0 || slopeLengthControl.GetCurrentValue() == 0)
            {
                UIManager.Instance.ShowMessage("Slope height and Length must be greater than 0", 2);
                return;
            }

            slopeBuilder.Build();
            StateController.Instance.ChangeState(new DefaultState());
        }

        private void CancelClicked(ClickEvent evt)
        {
            Destroy(slopeBuilder.gameObject);
            StateController.Instance.ChangeState(new DefaultState());
        }

        private void ReturnClicked(ClickEvent evt)
        {
            Destroy(slopeBuilder.gameObject);
            StateController.Instance.ChangeState(new SlopePositioningState());
        }

        private void OnDisable()
        {
            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
            returnButton.UnregisterCallback<ClickEvent>(ReturnClicked);
            buildButton.UnregisterCallback<ClickEvent>(BuildClicked);
        }        
	}
}