﻿using Assets.Scripts.Builders.Slope;
using Assets.Scripts.Managers;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using System.Collections;
using System.IO.Pipes;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Assets.Scripts.Builders
{
    /// <summary>
    /// Moves a highlight object based on user input on a line that goes from the last line element position in the direction of riding.<br/>
    /// Measures the distance from the last line element to the highlight and shows it to the user.
    /// </summary>
    /// <remarks>The highlight here is a Unity Decal Projector component</remarks>
    public class SlopePositioner : Positioner
    {
        private SlopeChangeBuilder builder;
        
        public override void OnEnable()
        {
            base.OnEnable();

            builder = GetComponent<SlopeChangeBuilder>();
            builder.enabled = true;
            
            // move highlight in front of the last line element and make it 
            Vector3 position = lastLineElement.GetEndPoint() + lastLineElement.GetRideDirection().normalized;

            builder.LengthChanged += OnParamChanged;
            builder.HeightDiffChanged += OnParamChanged;
            builder.PositionChanged += OnParamChanged;

            builder.SetPosition(position);

            baseBuilder = builder;


        }

        protected override void OnDisable()
        {
            base.OnDisable();
            builder.LengthChanged -= OnParamChanged;
            builder.HeightDiffChanged -= OnParamChanged;
            builder.PositionChanged -= OnParamChanged;
        }

        void OnParamChanged<T>(object sender, ParamChangeEventArgs<T> args)
        {
            UpdateLineRenderer();
            UpdateTextMesure();
        }

        void UpdateLineRenderer()
        {

            // draw a line between the current line end point and the point where the mouse is pointing
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, lastLineElement.GetEndPoint());
            lineRenderer.SetPosition(1, builder.GetStartPoint());
        }

        void UpdateTextMesure()
        {
            Vector3 endPoint = lastLineElement.GetEndPoint();
            // project the hit point on a line that goes from the last line element position in the direction of riding
            Vector3 projectedHitPoint = Vector3.Project(builder.Start - endPoint, Line.Instance.GetCurrentRideDirection()) + endPoint;

            Vector3 toHit = projectedHitPoint - endPoint;

            float distance = Vector3.Distance(projectedHitPoint, endPoint);

            // make the text go along the line and lay flat on the terrain
            float camDistance = CameraManager.Instance.GetTDCamDistance();
            textMesh.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, camDistance)), Quaternion.LookRotation(-Vector3.up, Vector3.Cross(toHit, Vector3.up)));

            string text = $"Distance: {distance:F2}m";

            float exitSpeed = builder.GetExitSpeed();
            if (exitSpeed != 0)
            {
                text += $"\nExit Speed: {PhysicsManager.MsToKmh(exitSpeed):F2}km/h";
            }            
            
            textMesh.GetComponent<TextMeshPro>().text = text;

        }

        /// <summary>
        /// For a given direction, creates a rotation that positions a Unity DecalProjector flush with the ground and facing the direction.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static Quaternion GetRotationForDirection(Vector3 direction)
        {
            Vector3 newRideDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            Vector3 rideDirNormal = Vector3.Cross(newRideDirection, Vector3.up).normalized;
            return Quaternion.LookRotation(-Vector3.up, rideDirNormal);
        }

        public bool ValidatePosition(Vector3 position)
        {
            Vector3 endPoint = lastLineElement.GetEndPoint();
            Vector3 rideDirection = lastLineElement.GetRideDirection();


            // project the hit point on a line that goes from the last line element position in the direction of riding
            Vector3 projectedHitPoint = Vector3.Project(position - endPoint, rideDirection) + endPoint;

            Vector3 toHit = projectedHitPoint - endPoint;

            float projection = Vector3.Dot(toHit, rideDirection);
            if (projection < 0)
            {
                StudioUIManager.Instance.ShowMessage("Cannot place the slope change behind the previous line element.", 2f);
                return false;
            }

            // if the projected point is too close to the last line element or too far from it, return
            if (toHit.magnitude < SlopeSettings.MIN_BUILD_DISTANCE)
            {
                StudioUIManager.Instance.ShowMessage($"Slope must be at least {SlopeSettings.MIN_BUILD_DISTANCE:F2} away from the last line element.", 2f);
                return false;
            }
            else if (toHit.magnitude > SlopeSettings.MAX_BUILD_DISTANCE)
            {
                StudioUIManager.Instance.ShowMessage($"Slope must be at most {SlopeSettings.MAX_BUILD_DISTANCE:F2} away from the last line element.", 2f);
                return false;
            }
            else if (!builder.IsBuildable(projectedHitPoint, builder.Length, rideDirection))
            {
                StudioUIManager.Instance.ShowMessage("Slope cannot be built here. The area is occupied.", 2f);
                return false;
            }

            // check if the slope can be reached and whether it can be traveled at the current speed
            if (!SlopeChangeBuilder.HasEnoughExitSpeed(position, builder.Length, builder.HeightDifference))
            {
                StudioUIManager.Instance.ShowMessage($"Cannot place the slope change here. The exit speed at the end of the slope would be lower than {PhysicsManager.MsToKmh(LineSettings.MIN_EXIT_SPEED_MS)}", 2f);
                return false;
            }

            return true;
        }

        public override bool TrySetPosition(Vector3 hit)
        {    
            if (!ValidatePosition(hit))
            {
                return false;
            }

            Vector3 endPoint = lastLineElement.GetEndPoint();
            Vector3 rideDirection = lastLineElement.GetRideDirection();
            // project the hit point on a line that goes from the last line element position in the direction of riding
            Vector3 projectedHitPoint = Vector3.Project(hit - endPoint, rideDirection) + endPoint;

            StudioUIManager.Instance.HideMessage();

            builder.SetPosition(projectedHitPoint);
            
            return true;            
        }                 
    }
}