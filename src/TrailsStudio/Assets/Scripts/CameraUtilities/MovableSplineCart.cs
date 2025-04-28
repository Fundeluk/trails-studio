using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using Assets.Scripts;

/// <summary>
/// This class allows the player to move a spline cart along a spline using <see cref="MoveAction"/>.
/// The direction of movement is determined by the camera's forward direction.
/// </summary>
[RequireComponent(typeof(CinemachineSplineCart))]
public class MovableSplineCart : MonoBehaviour
{
    [SerializeField]
    CinemachineCamera splineCam;

    [SerializeField]
    CinemachineSplineCart splineCart;        


    [SerializeField, Tooltip("World units per second")]
    float maxSpeed;

    [SerializeField, Tooltip("How much the speed increases in world units per second")]
    float acceleration;

    public float DefaultSplinePosition { get; private set; } = 0.9f;

    float lastSplinePosition = 0.9f;

    public InputAction MoveAction { get; private set; }

    float speed = 0f;

    Vector2 rawInput = Vector2.zero;

    private void OnEnable()
    {
        splineCart.SplinePosition = lastSplinePosition;
        MoveAction = InputSystem.actions.FindAction("Move");
    }

    private void OnDisable()
    {
        lastSplinePosition = splineCart.SplinePosition;
    }

    private void Update()
    {
        rawInput = MoveAction.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        MoveAlongSpline();            
    }        

    void MoveAlongSpline()
    {
        if (rawInput.magnitude < 0.1f) 
        {
            speed = 0f;
            return;
        }
            
        Vector3 cameraForward = splineCam.transform.forward.normalized;

        Vector3 relativeMoveDir = CameraManager.CameraRelativeFlatten(rawInput, cameraForward);

        // get the lineSpline's forward direction at the current position
        Vector3 splineForwardDirection = splineCart.Spline.EvaluateTangent(splineCart.SplinePosition);
        splineForwardDirection.Normalize();
            
        Vector3 onSplineProjection = Vector3.Project(relativeMoveDir, splineForwardDirection);

        bool isMovingForward = Vector3.Dot(onSplineProjection, splineForwardDirection) > 0;

        float splineLength = splineCart.Spline.CalculateLength(0);

        if (speed < maxSpeed)
        {
            speed += acceleration * Time.deltaTime;
        }
        else
        {
            speed = maxSpeed;
        }

        float normalizedSpeed = speed / splineLength;

        float movementDelta = normalizedSpeed * Time.deltaTime * (isMovingForward ? 1f : -1f);

        // limit the position so that it wont get to 0 - encountered a bug where the cart gets stuck
        float targetPosition = Mathf.Clamp(splineCart.SplinePosition + movementDelta, 0.00001f, 1f);

        splineCart.SplinePosition = targetPosition;        
    }
}
