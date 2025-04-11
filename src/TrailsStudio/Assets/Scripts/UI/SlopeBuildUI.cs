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
    public class SlopeControl : ValueControl
    {
        readonly SlopeChangeBuilder slopeBuilder;
        public SlopeControl(VisualElement root, SlopeChangeBuilder slopeBuilder, float increment, float minValue, float maxValue, float currentValue, string unit, List<BoundDependency> dependencies,
            SlopeValueSetter slopeSetter)
            : base(root, increment, minValue, maxValue, unit, dependencies)
        {
            this.slopeSetter = slopeSetter;
            this.currentValue = currentValue;
            this.slopeBuilder = slopeBuilder;
            UpdateShownValue();
        }

        public delegate void SlopeValueSetter(SlopeChangeBuilder slopeBuilder, float value);
        public readonly SlopeValueSetter slopeSetter;

        public override void SetCurrentValue(float value)
        {
            base.SetCurrentValue(value);

            slopeSetter(slopeBuilder, currentValue);
        }
    }

    public class SlopeBuildUI : MonoBehaviour
	{
        public const string MeterUnit = "m";
        public const float MAX_HEIGHT_DIFFERENCE = 10;
        public const float MIN_HEIGHT_DIFFERENCE = -10;
        public const float MAX_LENGTH = 100;
        public const float MIN_LENGTH = 0;

        private Button cancelButton;
        private Button returnButton;

        private Button buildButton;

        private SlopeChangeBuilder slopeBuilder;

        private SlopeControl slopeHeightControl;
        private SlopeControl slopeLengthControl;        

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
            slopeHeightControl = new SlopeControl(slopeHeight, slopeBuilder, 0.1f, MIN_HEIGHT_DIFFERENCE, MAX_HEIGHT_DIFFERENCE, 0, MeterUnit, noDeps, (slopeChange, newVal) => slopeChange.SetHeightDifference(newVal));

            VisualElement slopeLength = uiDocument.rootVisualElement.Q<VisualElement>("SlopeLengthControl");
            slopeLengthControl = new SlopeControl(slopeLength, slopeBuilder, 0.2f, MIN_LENGTH, MAX_LENGTH, 0, MeterUnit, noDeps, (slopeChange, newVal) => slopeChange.SetLength(newVal));
        }

        private void BuildClicked(ClickEvent evt)
        {
            if (slopeHeightControl.GetCurrentValue() == 0 || slopeLengthControl.GetCurrentValue() == 0)
            {
                UIManager.Instance.ShowMessage("Slope height and length must be greater than 0", 2);
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