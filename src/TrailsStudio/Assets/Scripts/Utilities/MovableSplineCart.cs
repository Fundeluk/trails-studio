using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

namespace Assets.Scripts.Utilities
{
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

        private InputAction moveAction;        


        private void OnEnable()
        {
            // Get the input actions
            moveAction = InputSystem.actions.FindAction("Move");

            splineCart.SplinePosition = 0.9f;

            splineCam.transform.LookAt(Line.Instance.GetLastLineElement().GetCameraTarget().transform);
        }
                
        void FixedUpdate()
        {
            MoveAlongSpline();
        }

        // https://gamedev.stackexchange.com/questions/188430/how-to-get-the-correct-input-direction-based-on-the-camera-angle-to-use-in-a-roo
        Vector3 CameraRelativeFlatten(Vector2 input, Vector3 camForward)
        {
            Vector3 flattened = Vector3.ProjectOnPlane(camForward, Vector3.up);
            Quaternion camOrientation = Quaternion.LookRotation(flattened);

            return camOrientation * new Vector3(input.x, 0, input.y);
        }



        void MoveAlongSpline()
        {
            if (splineCart == null || splineCam == null) return;

            // Get raw input
            Vector2 rawInput = moveAction.ReadValue<Vector2>();

            if (rawInput.magnitude < 0.1f) return;

            Vector3 cameraForward = splineCam.transform.forward.normalized;

            Vector3 relativeMoveDir = CameraRelativeFlatten(rawInput, cameraForward);

            // Get the lineSpline's forward direction at the current position
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
}