using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Cinemachine.CinemachineInputAxisController.Reader;


public class ClickAndDragInputHandler : MonoBehaviour
{
    private CinemachineInputAxisController inputAxisController;

    void Start()
    {
        inputAxisController = GetComponent<CinemachineInputAxisController>();
        if (inputAxisController != null)
        {
            inputAxisController.ReadControlValueOverride = CustomReadControlValue;
        }
    }

    private float CustomReadControlValue(
    InputAction action, IInputAxisOwner.AxisDescriptor.Hints hint, UnityEngine.Object context,
    ControlValueReader defaultReader)
    {
        var control = action.activeControl;
        if (control != null)
        {
            try
            {
                // If we can read as a Vector2, do so
                if (control.valueType == typeof(Vector2) || action.expectedControlType == "Vector2")
                {
                    var value = action.ReadValue<Vector2>();
                    return hint == IInputAxisOwner.AxisDescriptor.Hints.Y ? value.y : value.x;
                }
                // Default: assume type is float
                return action.ReadValue<float>();
            }
            catch (InvalidOperationException)
            {
                Debug.LogError($"An action in {context.name} is mapped to a {control.valueType.Name} "
                    + "control.  CinemachineInputAxisController.Reader can only handle float or Vector2 types.  "
                    + "To handle other types you can install a custom handler for "
                    + "CinemachineInputAxisController.ReadControlValueOverride.");
            }
        }
        return 0f;
    }
}
