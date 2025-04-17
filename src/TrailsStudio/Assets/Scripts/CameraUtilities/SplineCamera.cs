using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using Assets.Scripts;

[RequireComponent(typeof(CinemachineCamera), typeof(CinemachinePanTilt))]
public class SplineCamera : MonoBehaviour
{
    [SerializeField]
    MovableSplineCart splineCart;

    [SerializeField, Tooltip("Zoom script for the camera. Not necessary.")]
    ZoomableCamera zoomScript;

    [SerializeField]
    GameObject trackingTarget;

    [SerializeField]
    float inactivityThreshold = 10f;

    float inactivityTimer = 0f; // Timer to track inactivity duration

    public InputAction ClickNDragAction { get; private set; }


    /// <summary>
    /// Updates this camera tracking target's look direction to the last line element.
    /// </summary>
    /// <returns>Vector that points from the camera to the target in world coordinates.</returns>
    public Vector3 UpdateTrackingTarget()
    {
        Vector3 worldToTarget = (Line.Instance.GetLastLineElement().GetCameraTarget().transform.position - transform.position).normalized;
        var rotation = Quaternion.LookRotation(worldToTarget);
        trackingTarget.transform.rotation = rotation;
        return worldToTarget;
    }

    public void RecenterCamera()
    {
        UpdateTrackingTarget();
        var panTilt = GetComponent<CinemachinePanTilt>();
        panTilt.PanAxis.TriggerRecentering();
        panTilt.TiltAxis.TriggerRecentering();
    }
        
    private void OnEnable()
    {
        RecenterCamera();

        ClickNDragAction = InputSystem.actions.FindAction("Click&Drag");

        if (zoomScript != null)
        {
            zoomScript.enabled = true;
        }
        
        splineCart.enabled = true;
    }

    private void OnDisable()
    {
        if (zoomScript != null)
        {
            zoomScript.enabled = false;
        }
        splineCart.enabled = false;
    }

    bool IsAnyInputPressed()
    {
        if (ClickNDragAction.IsPressed())
        {
            return true;
        }

        if (zoomScript != null && zoomScript.ZoomAction.IsPressed())
        {
            return true;
        }

        if (splineCart.MoveAction.IsPressed())
        {
            return true;
        }

        return false;
    }


    // Update is called once per frame
    void Update()
    {
        inactivityTimer += Time.deltaTime;

        if (IsAnyInputPressed())
        {
            // reset the inactivity timer on user input
            inactivityTimer = 0f;
        }

        if (inactivityTimer > inactivityThreshold)
        {
            // switch to rotating camera if the user is inactive for too long
            CameraManager.Instance.RotateAroundView();
        }
    }
}
