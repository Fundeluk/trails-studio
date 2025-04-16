using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Utilities
{
    public class MovableSplineCart : MonoBehaviour
    {
        [SerializeField]
        CinemachineCamera splineCam;

        [SerializeField]
        CinemachineSplineCart splineCart;

        [SerializeField]
        float moveSpeed = 5f;

        [SerializeField, Tooltip("How quickly the position changes when keys are pressed")]
        float acceleration = 10f;

        private InputAction moveAction;
        

        private void OnEnable()
        {
            // Get the input actions
            moveAction = InputSystem.actions.FindAction("Move");

            Debug.Log($"moveAction: {moveAction}");
        }

        void FixedUpdate()
        {
            MoveAlongSpline();
        }

        void MoveAlongSpline()
        {
            if (splineCart == null || splineCam == null) return;

            Vector2 moveInput = moveAction.ReadValue<Vector2>();

            float verticalInput = moveInput.y;
            float horizontalInput = moveInput.x;

            // Get the forward direction of the camera
            Vector3 cameraForward = splineCam.transform.forward;
            cameraForward.y = 0; // Remove any vertical component
            cameraForward.Normalize();

            // Get the spline's forward direction at the current position
            Vector3 splineForward = splineCart.Spline.EvaluateTangent(splineCart.SplinePosition);
            splineForward.y = 0; // Remove any vertical component
            splineForward.Normalize();

            // Get the right vector of the spline
            Vector3 splineRight = Vector3.Cross(Vector3.up, splineForward).normalized;

            // Calculate dot products to determine alignment
            float forwardAlignment = Vector3.Dot(cameraForward, splineForward);
            float rightAlignment = Vector3.Dot(cameraForward, splineRight);
            
            float movementValue;
            // If camera is more aligned with the spline's forward/backward direction
            if (Mathf.Abs(forwardAlignment) > Mathf.Abs(rightAlignment))
            {
                // Use W/S keys (vertical input)
                movementValue = verticalInput * Mathf.Sign(forwardAlignment);
            }
            // If camera is more aligned with the spline's right/left direction
            else
            {
                // Use A/D keys (horizontal input)
                movementValue = horizontalInput * Mathf.Sign(rightAlignment);
            }
            
            // Apply the new position to the spline cart
            splineCart.SplinePosition = Mathf.Lerp(splineCart.SplinePosition, splineCart.SplinePosition + movementValue * moveSpeed * Time.deltaTime, Time.deltaTime * acceleration);
        }
    }
}