using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public struct BoundDependency
    {
        public ValueControl dependentControl;

        public delegate float LowerBoundGetter(float driverControlNewValue);
        public delegate float UpperBoundGetter(float driverControlNewValue);

        private readonly LowerBoundGetter getLowerBound;
        private readonly UpperBoundGetter getUpperBound;

        public readonly void SetLowerBound(float driverControlNewValue) => dependentControl.SetLowerBound(getLowerBound(driverControlNewValue));
        public readonly void SetUpperBound(float driverControlNewValue) => dependentControl.SetUpperBound(getUpperBound(driverControlNewValue));

        public BoundDependency(ValueControl control, LowerBoundGetter lowerBound, UpperBoundGetter upperBound)
        {
            if (control == null)
            {
                Debug.LogError("Dependent control cannot be null.");
            }
            dependentControl = control;
            getLowerBound = lowerBound;
            getUpperBound = upperBound;
        }
    }

    // TODO implement holding the button to increase/decrease the value
    public abstract class ValueControl
    {
        public readonly Button PlusButton;
        public readonly Button MinusButton;
        public readonly Label NameLabel;
        public readonly Label ValueLabel;

        protected List<BoundDependency> boundDependencies = new();

        public float Increment { get; protected set; }
        public float MinValue { get; protected set; }
        public float MaxValue { get; protected set; }

        protected float currentValue;

        public string unit;

        protected virtual void UpdateDependentBounds()
        {
            foreach (BoundDependency dependency in boundDependencies)
            {
                dependency.SetLowerBound(currentValue);
                dependency.SetUpperBound(currentValue);
            }
        }

        public float GetCurrentValue() => currentValue;

        public virtual void SetCurrentValue(float value)
        {
            value = RoundToNearest(value, Increment);
            if (value < MinValue)
            {
                currentValue = MinValue;
            }
            else if (value > MaxValue)
            {
                currentValue = MaxValue;
            }
            else
            {
                currentValue = value;
            }

            UpdateShownValue();
            UpdateDependentBounds();
        }

        public void SetLowerBound(float value)
        {
            MinValue = RoundToNearest(value, Increment);
            if (currentValue < MinValue)
            {
                currentValue = MinValue;
                UpdateShownValue();
            }
        }

        public void SetUpperBound(float value)
        {
            MaxValue = RoundToNearest(value, Increment);
            if (currentValue > MaxValue)
            {
                currentValue = MaxValue;
                UpdateShownValue();
            }
        }

        protected void UpdateShownValue()
        {            
            ValueLabel.text = currentValue.ToString() + unit;
        }

        protected virtual void OnPlusClicked(ClickEvent evt)
        {
           SetCurrentValue(currentValue + Increment);
        }

        protected virtual void OnMinusClicked(ClickEvent evt)
        {
            SetCurrentValue(currentValue - Increment);
        }

        public ValueControl(VisualElement control, float increment, float minValue, float maxValue, string unit, List<BoundDependency> dependencies)
        {
            PlusButton = control.Q<Button>("PlusButton");
            MinusButton = control.Q<Button>("MinusButton");
            NameLabel = control.Q<Label>("ValueNameLabel");
            ValueLabel = control.Q<Label>("CurrentValueLabel");
            Increment = increment;
            MinValue = RoundToNearest(minValue, increment);
            MaxValue = RoundToNearest(maxValue, increment);
            this.unit = unit;
            boundDependencies = dependencies;
            currentValue = 0;

            PlusButton.RegisterCallback<ClickEvent>(OnPlusClicked);
            MinusButton.RegisterCallback<ClickEvent>(OnMinusClicked);
        }

        private static float RoundToNearest(float value, float nearest)
        {
            return MathF.Round(value / nearest) * nearest;
        }

    }
}
