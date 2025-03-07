using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;
using static TakeoffMeshGenerator;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using Assets.Scripts.Managers;

namespace Assets.Scripts.UI
{
    public class SlopeControl : ValueControl
    {
        readonly SlopeChange slopeChange;
        public SlopeControl(VisualElement root, SlopeChange slopeChange, float increment, float minValue, float maxValue, float currentValue, string unit, List<BoundDependency> dependencies,
            SlopeValueSetter slopeSetter)
            : base(root, increment, minValue, maxValue, unit, dependencies)
        {
            this.slopeSetter = slopeSetter;
            this.currentValue = currentValue;
            this.slopeChange = slopeChange;
            UpdateShownValue();
        }

        public delegate void SlopeValueSetter(SlopeChange slopeChange, float value);
        public readonly SlopeValueSetter slopeSetter;

        public override void SetCurrentValue(float value)
        {
            base.SetCurrentValue(value);

            slopeSetter(slopeChange, currentValue);
        }
    }

    public class SlopeBuildUI : MonoBehaviour
	{
        public const string MeterUnit = "m";
        public const float MAX_HEIGHT_DIFFERENCE = 10;
        public const float MIN_HEIGHT_DIFFERENCE = -10;

        private Button cancelButton;
        private Button returnButton;

        private Button buildButton;

        private SlopeChange slopeChange;

        private SlopeControl slopeControl;

        // Use this for initialization
        void Start()
		{
            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            returnButton = uiDocument.rootVisualElement.Q<Button>("ReturnButton");
            buildButton = uiDocument.rootVisualElement.Q<Button>("BuildButton");
            buildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            returnButton.RegisterCallback<ClickEvent>(ReturnClicked);

            slopeChange = TerrainManager.Instance.slopeChanges[^1];

            List<BoundDependency> noDeps = new();

            VisualElement slope = uiDocument.rootVisualElement.Q<VisualElement>("SlopeControl");
            slopeControl = new SlopeControl(slope, slopeChange, 0.1f, MIN_HEIGHT_DIFFERENCE, MAX_HEIGHT_DIFFERENCE, 0, MeterUnit, noDeps, (slopeChange, newVal) => slopeChange.SetHeightDifference(newVal));
        }

        private void BuildClicked(ClickEvent evt)
        {
            slopeChange.ChangeTerrain();

            StateController.Instance.ChangeState(new DefaultState());
        }

        private void CancelClicked(ClickEvent evt)
        {
            StateController.Instance.ChangeState(new DefaultState());
        }

        private void ReturnClicked(ClickEvent evt)
        {
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