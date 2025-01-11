using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.Builders;
using Assets.Scripts.States;
using System.Collections.Generic;

namespace Assets.Scripts.UI
{
    public class LandingControl : ValueControl
    {
        private readonly LandingMeshGenerator.Landing landing;
        public LandingControl(VisualElement root, float increment, float minValue, float maxValue, float currentValue, string unit, List<BoundDependency> dependencies, LandingMeshGenerator.Landing landing,
            LandingValueSetter landingSetter)
            : base(root, increment, minValue, maxValue, unit, dependencies)
        {
            this.landing = landing;
            this.landingSetter = landingSetter;
            this.currentValue = currentValue;
            UpdateShownValue();
        }
        public delegate void LandingValueSetter(LandingMeshGenerator.Landing landing, float value);
        public readonly LandingValueSetter landingSetter;
        public override void SetCurrentValue(float value)
        {
            base.SetCurrentValue(value);
            landingSetter(landing, currentValue);
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

        private LandingMeshGenerator.Landing landing;

        private void Initialize()
        {
            if (Line.Instance.line[^1] is not LandingMeshGenerator.Landing)
            {
                Debug.LogError("The last element in the line is not a landing.");
            }

            landing = Line.Instance.line[^1] as LandingMeshGenerator.Landing;

            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            returnButton = uiDocument.rootVisualElement.Q<Button>("ReturnButton");
            buildButton = uiDocument.rootVisualElement.Q<Button>("BuildButton");
            buildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            returnButton.RegisterCallback<ClickEvent>(ReturnClicked);

            List<BoundDependency> noDeps = new();

            VisualElement slope = uiDocument.rootVisualElement.Q<VisualElement>("SlopeControl");
            slopeControl = new LandingControl(slope, 1, MIN_SLOPE, MAX_SLOPE, landing.GetSlope(), DegreeUnit, noDeps, landing, (landing, value) =>
            {
                landing.SetSlope(value);
            });


            List<BoundDependency> onHeightDeps = new() { new (slopeControl, (newHeight) => MIN_SLOPE + newHeight*6, (newHeight) => Mathf.Min(MIN_SLOPE + newHeight * 15, MAX_SLOPE) )};
            VisualElement height = uiDocument.rootVisualElement.Q<VisualElement>("HeightControl");
            heightControl = new LandingControl(height, 0.1f, MIN_HEIGHT, MAX_HEIGHT, landing.GetHeight(), MeterUnit, onHeightDeps, landing, (landing, value) =>
            {
                landing.SetHeight(value);
            });

            List<BoundDependency> onWidthDeps = new() { new(heightControl, (newWidth) => Mathf.Max(newWidth / 1.5f, MIN_HEIGHT), (newWidth) => Mathf.Min(newWidth * 5, MAX_HEIGHT)) };
            VisualElement width = uiDocument.rootVisualElement.Q<VisualElement>("WidthControl");
            widthControl = new LandingControl(width, 0.1f, MIN_WIDTH, MAX_WIDTH, landing.GetWidth(), MeterUnit, noDeps, landing, (landing, value) =>
            {
                landing.SetWidth(value);
            });

            VisualElement thickness = uiDocument.rootVisualElement.Q<VisualElement>("ThicknessControl");
            thicknessControl = new LandingControl(thickness, 0.1f, MIN_THICKNESS, MAX_THICKNESS, landing.GetThickness(), MeterUnit, noDeps, landing, (landing, value) =>
            {
                landing.SetThickness(value);
            });

            VisualElement rotation = uiDocument.rootVisualElement.Q<VisualElement>("RotationControl");
            rotationControl = new LandingControl(rotation, 1, -90, 90, landing.GetRotation(), DegreeUnit, noDeps, landing, (landing, value) =>
            {
                landing.SetRotation((int)value);
            });
        }

        // Use this for initialization
        void Start()
        {
            Initialize();
        }

        void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
            returnButton.UnregisterCallback<ClickEvent>(ReturnClicked);
            buildButton.UnregisterCallback<ClickEvent>(BuildClicked);
        }

        private void BuildClicked(ClickEvent evt)
        {
            StateController.Instance.ChangeState(new DefaultState());
        }

        private void CancelClicked(ClickEvent evt)
        {
            // TODO may need to check if the last line element is really a landing
            Line.Instance.line.RemoveAt(Line.Instance.line.Count - 1);
            
            StateController.Instance.ChangeState(new TakeOffBuildState());
        }

        private void ReturnClicked(ClickEvent evt)
        {
            Line.Instance.DestroyLastLineElement();

            StateController.Instance.ChangeState(new LandingPositioningState());
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}