using Assets.Scripts.Builders;
using Assets.Scripts.Managers;
using System;
using System.Collections;
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
            dependentControl = control ?? throw new ArgumentException("Control cannot be null.");
            getLowerBound = lowerBound;
            getUpperBound = upperBound;
        }
    }

    public class ValueChangedEventArgs : EventArgs
    {
        public float NewValue { get; }

        public ValueChangedEventArgs(float newValue)
        {
            NewValue = newValue;
        }
    }

    /// <summary>
    /// Base class for all value controls. This class handles the logic for incrementing and decrementing values, as well as displaying them and setting bounds.
    /// </summary>
    public abstract class ValueControl
    {
        public const string MeterUnit = "m";
        public const string DegreeUnit = "°";

        const string defaultFormatString = "0.##"; // Default format for displaying values

        string formatString = defaultFormatString; // Format string for displaying values

        public readonly Button PlusButton;
        public readonly Button MinusButton;
        public readonly Label NameLabel;
        public readonly Label ValueLabel;

        protected List<BoundDependency> boundDependencies = new();

        public event EventHandler<ValueChangedEventArgs> ValueChanged;

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
            ValueChanged?.Invoke(this, new ValueChangedEventArgs(currentValue));
        }

        protected void InvokeValueChanged(float newValue)
        {
            ValueChanged?.Invoke(this, new ValueChangedEventArgs(newValue));
        }

        public void SetLowerBound(float value)
        {
            MinValue = RoundToNearest(value, Increment);
            if (currentValue < MinValue)
            {
                SetCurrentValue(MinValue);
            }
        }

        public void SetUpperBound(float value)
        {
            MaxValue = RoundToNearest(value, Increment);
            if (currentValue > MaxValue)
            {
                SetCurrentValue(MaxValue);
            }
        }

        protected virtual void UpdateShownValue()
        {            
            ValueLabel.text = currentValue.ToString(formatString) + unit;
        }

        protected virtual void OnPlusClicked(ClickEvent evt)
        {
           SetCurrentValue(currentValue + Increment);
        }

        private Coroutine plusHoldCoroutine = null;
        private Coroutine minusHoldCoroutine = null;
        private bool isPlusButtonHeld = false;
        private bool isMinusButtonHeld = false;
        private readonly float holdDelay = 0.5f; // Initial delay before repeating
        private readonly float holdInterval = 0.1f; // Interval for value change while

        private IEnumerator HoldButtonCoroutine(bool isPlus)
        {
            // Wait for initial delay before starting continuous adjustment
            yield return new WaitForSeconds(holdDelay);


            // Continue adjusting the value as long as the button is held
            while (isPlus ? isPlusButtonHeld : isMinusButtonHeld)
            {
                if (isPlus)
                {
                    SetCurrentValue(currentValue + Increment);
                }
                else
                {
                    SetCurrentValue(currentValue - Increment);
                }

                yield return new WaitForSeconds(holdInterval);
            }
        }



        protected virtual void OnPlusPointerDown(PointerDownEvent evt)
        {
            SetCurrentValue(currentValue + Increment);

            isPlusButtonHeld = true;
            plusHoldCoroutine = UIManager.Instance.StartCoroutineFromInstance(HoldButtonCoroutine(true));
        }

        protected virtual void OnPlusPointerUp(PointerUpEvent evt)
        {
            isPlusButtonHeld = false;
            if (plusHoldCoroutine != null)
            {
                UIManager.Instance.StopCoroutineFromInstance(plusHoldCoroutine);
                plusHoldCoroutine = null;
            }
        }

        protected virtual void OnMinusPointerDown(PointerDownEvent evt)
        {
            SetCurrentValue(currentValue - Increment);

            isMinusButtonHeld = true;
            minusHoldCoroutine = UIManager.Instance.StartCoroutineFromInstance(HoldButtonCoroutine(false));
        }

        protected virtual void OnMinusPointerUp(PointerUpEvent evt)
        {
            isMinusButtonHeld = false;
            if (minusHoldCoroutine != null)
            {
                UIManager.Instance.StopCoroutineFromInstance(minusHoldCoroutine);
                minusHoldCoroutine = null;
            }
        }

        protected virtual void OnMinusClicked(ClickEvent evt)
        {
            SetCurrentValue(currentValue - Increment);
        }

        public ValueControl(VisualElement control, float increment, float minValue, float maxValue, string unit, List<BoundDependency> dependencies, string formatString = null)
        {
            if (formatString != null)
            {
                this.formatString = formatString;
            }

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

            //PlusButton.RegisterCallback<ClickEvent>(OnPlusClicked);
            //MinusButton.RegisterCallback<ClickEvent>(OnMinusClicked);
            PlusButton.RegisterCallback<PointerDownEvent>(OnPlusPointerDown, TrickleDown.TrickleDown);
            PlusButton.RegisterCallback<PointerUpEvent>(OnPlusPointerUp);
            MinusButton.RegisterCallback<PointerDownEvent>(OnMinusPointerDown, TrickleDown.TrickleDown);
            MinusButton.RegisterCallback<PointerUpEvent>(OnMinusPointerUp);
        }

        protected static float RoundToNearest(float value, float nearest)
        {
            return MathF.Round(value / nearest) * nearest;
        }

    }

    public class ObstacleValueControl<BuilderT> : ValueControl where BuilderT : IBuilder
    {
        private readonly BuilderT builder;

        private readonly Action<BuilderT, float> valueSetter;

        private readonly Func<BuilderT, float> valueGetter;


        public ObstacleValueControl(VisualElement root, float increment, float minValue, float maxValue, string unit, List<BoundDependency> dependencies, BuilderT builder,
            Action<BuilderT, float> setter, Func<BuilderT, float> getter, string formatString = null)
            : base(root, increment, minValue, maxValue, unit, dependencies, formatString)
        {
            this.builder = builder;
            valueSetter = setter;
            valueGetter = getter;
            currentValue = valueGetter(builder);
            UpdateShownValue();
        }

        public override void SetCurrentValue(float value)
        {
            value = RoundToNearest(value, Increment);
            if (value < MinValue)
            {
                value = MinValue;
            }
            else if (value > MaxValue)
            {
                value = MaxValue;
            }
           
            valueSetter(builder, value);
            currentValue = valueGetter(builder);

            UpdateShownValue();
            UpdateDependentBounds();

            InvokeValueChanged(currentValue);
        }
    }

    /// <summary>
    /// Base class for all value displays. This class handles the logic for displaying values in a label.
    /// </summary>
    public class ValueDisplay
    {
        public readonly Label NameLabel;
        public readonly Label ValueLabel;

        const string defaultFormatString = "0.##"; // Default format for displaying values

        string formatString = defaultFormatString; // Format string for displaying values

        protected float currentValue;

        public string unit;

        protected void UpdateShownValue()
        {
            ValueLabel.text = currentValue.ToString(formatString) + unit;
        }

        public virtual void SetCurrentValue(float value)
        {
            currentValue = value;
            UpdateShownValue();            
        }

        public ValueDisplay(VisualElement control, float value, string unit, string formatString = null)
        {
            NameLabel = control.Q<Label>("ValueNameLabel");
            ValueLabel = control.Q<Label>("CurrentValueLabel");
            this.unit = unit;
            SetCurrentValue(value);
            if (formatString != null)
            {
                this.formatString = formatString;
            }
        }
    }
}
