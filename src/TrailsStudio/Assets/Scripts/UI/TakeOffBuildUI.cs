using Assets.Scripts.States;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class TakeoffControl : ValueControl
    {
        private readonly TakeoffMeshGenerator.Takeoff takeoff;
        public TakeoffControl(VisualElement root, float increment, float minValue, float maxValue, float currentValue, string unit, List<BoundDependency> dependencies, TakeoffMeshGenerator.Takeoff takeoff,
            TakeoffValueSetter takeoffSetter)
            : base(root, increment, minValue, maxValue, unit, dependencies)
        {
            this.takeoff = takeoff;
            this.takeoffSetter = takeoffSetter;
            this.currentValue = currentValue;
            UpdateShownValue();
        }

        public delegate void TakeoffValueSetter(TakeoffMeshGenerator.Takeoff takeoff, float value);
        public readonly TakeoffValueSetter takeoffSetter;

        public override void SetCurrentValue(float value)
        {
            base.SetCurrentValue(value);

            takeoffSetter(takeoff, currentValue);
        }
    }



    public class TakeOffBuildUI : MonoBehaviour
    {
        public const string MeterUnit = "m";

        private Button cancelButton;
        private Button returnButton;

        private Button buildButton;

        private const float MIN_RADIUS = 1;
        private const float MAX_RADIUS = 10;        

        private TakeoffControl radiusControl;

        private TakeoffControl heightControl;

        private TakeoffControl thicknessControl;

        private TakeoffControl widthControl;

        private TakeoffMeshGenerator.Takeoff takeoff;

        private void Initialize()
        {
            if (Line.Instance.GetLastLineElement() is not TakeoffMeshGenerator.Takeoff)
            {
                Debug.LogError("The last element in the line is not a takeoff.");
            }

            takeoff = Line.Instance.GetLastLineElement() as TakeoffMeshGenerator.Takeoff;

            var uiDocument = GetComponent<UIDocument>();
            cancelButton = uiDocument.rootVisualElement.Q<Button>("CancelButton");
            returnButton = uiDocument.rootVisualElement.Q<Button>("ReturnButton");
            buildButton = uiDocument.rootVisualElement.Q<Button>("BuildButton");
            buildButton.RegisterCallback<ClickEvent>(BuildClicked);
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            returnButton.RegisterCallback<ClickEvent>(ReturnClicked);

            List<BoundDependency> noDeps = new();

            VisualElement thickness = uiDocument.rootVisualElement.Q<VisualElement>("ThicknessControl");
            thicknessControl = new TakeoffControl(thickness, 0.1f, 0.5f, MAX_RADIUS / 4, takeoff.GetThickness(), MeterUnit, noDeps, takeoff, (takeoff, newVal) => takeoff.SetThickness(newVal));
            
            VisualElement width = uiDocument.rootVisualElement.Q<VisualElement>("WidthControl");
            widthControl = new TakeoffControl(width, 0.1f, MIN_RADIUS / 7 / 1.5f, MAX_RADIUS, takeoff.GetWidth(), MeterUnit, noDeps, takeoff, (takeoff, newVal) => takeoff.SetWidth(newVal));

            List<BoundDependency> onHeightDeps = new() { new(widthControl, (newHeight) => newHeight / 1.5f, (newHeight) => newHeight * 5), new(thicknessControl, (newHeight) => newHeight / 3, (newHeight) => newHeight) };
            VisualElement height = uiDocument.rootVisualElement.Q<VisualElement>("HeightControl");
            heightControl = new TakeoffControl(height, 0.1f, MIN_RADIUS / 7, MAX_RADIUS, takeoff.GetHeight(), MeterUnit, onHeightDeps, takeoff, (takeoff, newVal) => takeoff.SetHeight(newVal));

            List<BoundDependency> onRadiusDeps = new() { new(heightControl, (newRadius) => newRadius / 7, (newRadius) => newRadius) };
            VisualElement radius = uiDocument.rootVisualElement.Q<VisualElement>("RadiusControl");
            radiusControl = new TakeoffControl(radius, 0.1f, MIN_RADIUS, MAX_RADIUS, takeoff.GetRadius(), MeterUnit, onRadiusDeps, takeoff, (takeoff, newVal) => takeoff.SetRadius(newVal));
        }        

        void Start()
        {
            Initialize();
        }

        void OnEnable()
        {
            Initialize();
        }

        void OnDisable()
        {
            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
            returnButton.UnregisterCallback<ClickEvent>(ReturnClicked);
            buildButton.UnregisterCallback<ClickEvent>(BuildClicked);         
        }

        private void BuildClicked(ClickEvent evt)
        {            
            StateController.Instance.ChangeState(new LandingPositioningState());
        }

        private void CancelClicked(ClickEvent evt)
        {
            // destroy the takeoff currently being built
            Line.Instance.DestroyLastLineElement();
            StateController.Instance.ChangeState(new DefaultState());
        }

        private void ReturnClicked(ClickEvent evt)
        {
            // destroy the takeoff currently being built
            Line.Instance.DestroyLastLineElement();
            StateController.Instance.ChangeState(new TakeOffPositioningState());
        }       

    }
}
