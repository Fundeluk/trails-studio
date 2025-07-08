using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using Assets.Scripts;
using Assets.Scripts.CameraUtilities;

[RequireComponent(typeof(CinemachineCamera), typeof(CinemachinePanTilt))]
public class SplineCamera : MonoBehaviour
{
    [SerializeField]
    MovableSplineCart splineCart;

    [SerializeField, Tooltip("Zoom script for the camera. Not necessary.")]
    ZoomableCamera zoomScript;

    public SplineCamTargetRotater trackingTarget;    

    public InputAction ClickNDragAction { get; private set; }


    /// <summary>
    /// Recenters the camera using its <see cref="CinemachinePanTilt"/> component to look in the same direction as its <see cref="trackingTarget"/>.
    /// </summary>
    public void RecenterCamera()
    {
        var panTilt = GetComponent<CinemachinePanTilt>();

        // Ensure tracking target is used for recentering
        GetComponent<CinemachineCamera>().Target.TrackingTarget = trackingTarget.transform;
        GetComponent<CinemachineCamera>().Target.CustomLookAtTarget = false;
        panTilt.RecenterTarget = panTilt.RecenterTarget = CinemachinePanTilt.RecenterTargetModes.TrackingTargetForward;

        // Set the correct reference frame
        panTilt.ReferenceFrame = CinemachinePanTilt.ReferenceFrames.World;

        // Ensure recentering parameters are optimal
        panTilt.PanAxis.Recentering.Enabled = true;
        panTilt.PanAxis.Recentering.Wait = 0f;
        panTilt.PanAxis.Recentering.Time = 0f;

        panTilt.TiltAxis.Recentering.Enabled = true;
        panTilt.TiltAxis.Recentering.Wait = 0f;
        panTilt.TiltAxis.Recentering.Time = 0f;

        // Trigger recentering
        panTilt.PanAxis.TriggerRecentering();
        panTilt.TiltAxis.TriggerRecentering();

        panTilt.PanAxis.Recentering.Enabled = false;
        panTilt.TiltAxis.Recentering.Enabled = false;
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
}
