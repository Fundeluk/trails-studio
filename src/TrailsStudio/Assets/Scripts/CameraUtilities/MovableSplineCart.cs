using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using Assets.Scripts;


public class MovableSplineCart : MonoBehaviour
{
    [SerializeField]
    CinemachineCamera splineCam;

    [SerializeField]
    CinemachineSplineCart splineCart;        

    [SerializeField]
    float moveSpeed = 3f;

    [SerializeField, Tooltip("How quickly the position changes when keys are pressed")]
    float acceleration = 7f;
    public InputAction MoveAction { get; private set; }

    private void OnEnable()
    {
        MoveAction = InputSystem.actions.FindAction("Move");
    }        

    void FixedUpdate()
    {
        MoveAlongSpline();            
    }        

    void MoveAlongSpline()
    {
        Vector2 rawInput = MoveAction.ReadValue<Vector2>();

        if (rawInput.magnitude < 0.1f) return;
            
        Vector3 cameraForward = splineCam.transform.forward.normalized;

        Vector3 relativeMoveDir = CameraManager.CameraRelativeFlatten(rawInput, cameraForward);

        // get the lineSpline's forward direction at the current position
        Vector3 splineForwardDirection = splineCart.Spline.EvaluateTangent(splineCart.SplinePosition);
        splineForwardDirection.Normalize();
            
        Vector3 onSplineProjection = Vector3.Project(relativeMoveDir, splineForwardDirection);

        bool isMovingForward = Vector3.Dot(onSplineProjection, splineForwardDirection) > 0;

        float movementDelta = moveSpeed * Time.deltaTime * (isMovingForward ? 1f : -1f);
        float targetPosition = Mathf.Clamp(splineCart.SplinePosition + movementDelta, 0f, 1f);

        // Apply the new position to the lineSpline cart
        splineCart.SplinePosition = Mathf.Lerp(
            splineCart.SplinePosition,
            targetPosition,
            Time.deltaTime * acceleration
        );            
    }
}
