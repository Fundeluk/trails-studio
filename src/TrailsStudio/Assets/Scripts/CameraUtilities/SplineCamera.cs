﻿using System.Collections;
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

    [SerializeField]
    float inactivityThreshold;

    float inactivityTimer = 0f; // Timer to track inactivity duration

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

        inactivityTimer = 0f; // Reset inactivity timer
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

        inactivityTimer = 0f; // Reset inactivity timer
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


    //// Update is called once per frame
    //void Update()
    //{
    //    inactivityTimer += Time.deltaTime;

    //    if (IsAnyInputPressed())
    //    {
    //        // reset the inactivity timer on user input
    //        inactivityTimer = 0f;
    //    }

    //    //if (inactivityTimer > inactivityThreshold)
    //    //{
    //    //    // switch to rotating camera if the user is inactive for too long
    //    //    CameraManager.Instance.RotateAroundView();
    //    //}
    //}
}
