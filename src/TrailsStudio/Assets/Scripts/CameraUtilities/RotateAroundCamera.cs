using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Assets.Scripts;

/// <summary>
/// This script drives the rotating camera, controlling its rotation script and transitions to the spline camera in case of user input.
/// </summary>
[RequireComponent(typeof(ConstantRotation))]
public class RotateAroundCamera : MonoBehaviour
{
    [SerializeField]
    ConstantRotation rotation;

    InputAction moveAction;
    InputAction clickNDragAction;

    private void OnEnable()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        clickNDragAction = InputSystem.actions.FindAction("Click&Drag");
        rotation.enabled = true;
    }

    private void OnDisable()
    {
        rotation.enabled = false;
    }

    private void Update()
    {
        if (moveAction.IsPressed() || clickNDragAction.IsPressed())
        {
            Debug.Log("Changing from constantRotation to Spline Cam");
            enabled = false;
            CameraManager.Instance.SplineCamView();
        }
    }
}
