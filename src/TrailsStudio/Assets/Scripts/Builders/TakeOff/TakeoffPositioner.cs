using Assets.Scripts;
using Assets.Scripts.Managers;
using Assets.Scripts.States;
using Assets.Scripts.UI;
using Assets.Scripts.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Splines;
using UnityEngine.UIElements;


namespace Assets.Scripts.Builders
{
    /// <summary>
    /// Moves a highlight object on a line that goes from the last line element position in the direction of riding. <br/>
    /// Positions the highlight based on where the mouse is pointing on the terrain. Draws a line from the last line element to the highlight<br/>
    /// and shows distance from the line endpoint to the highlight.<br/>
    /// </summary>
    /// <remarks>Here, the highlight is the TakeoffBuilder mesh which is <b>attached to the same GameObject</b> as this highlighter.</remarks>
    public class TakeoffPositioner : Positioner
    {
        private TakeoffBuilder builder;

        //private TakeoffBuilder invisibleBuilder;

        /// <summary>
        /// The distance to position of the first obstruction on the way from the last line element to the takeoff.
        /// The default value is <see cref="maxBuildDistance"/>.<br/>
        /// Used to prevent placing a takeoff after an obstruction - the ride path should be free of obstacles.
        /// </summary>
        private float distanceToFirstObstruction;

        private Button buildButton;


        public override void OnEnable()
        {
            builder = gameObject.GetComponent<TakeoffBuilder>();
            baseBuilder = builder;
            //invisibleBuilder = builder.InvisibleClone;

            base.OnEnable();            

            builder.PositionChanged += OnParamChanged;
            builder.RotationChanged += OnParamChanged;
            builder.RadiusChanged += OnParamChanged;
            builder.HeightChanged += OnParamChanged;
            builder.EntrySpeedChanged += OnParamChanged;            

            builder.SetRideDirection(lastLineElement.GetRideDirection());            

            // position the highlight at minimal build distance from the last line element
            builder.SetPosition(lastLineElement.GetEndPoint() + (TakeoffSettings.MIN_BUILD_DISTANCE + builder.GetTransitionLengthXZ()) * lastLineElement.GetRideDirection());    
            
            buildButton = StudioUIManager.Instance.takeOffBuildUI.GetComponent<TakeOffBuildUI>().BuildButton;


            if (builder.GetExitSpeed() == 0)
            {
                string message;
                if (TerrainManager.Instance.ActiveSlope == null)
                {
                    message = "Insufficient speed to exit the takeoff on this position. Try adjusting its height and radius. If that does not work, you probably have to add a slope change.";
                }
                else
                {
                    message = "Insufficient speed to exit the takeoff on this position. Try adjusting its height and radius or move it along the slope change.";
                }
                
                StudioUIManager.Instance.ShowMessage(message, 5f);
                StudioUIManager.ToggleButton(buildButton, false);
            }

            UpdateLineRenderer();

            GetComponent<MeshRenderer>().enabled = true;

            distanceToFirstObstruction = TerrainManager.Instance.GetRideableDistance(lastLineElement.GetEndPoint(), lastLineElement.GetRideDirection(), clearanceWidth, lastLineElement.GetEndPoint().y, TakeoffSettings.MAX_BUILD_DISTANCE);

        }

        protected override void OnDisable()
        {
            builder.PositionChanged -= OnParamChanged;
            builder.RotationChanged -= OnParamChanged;
            builder.RadiusChanged -= OnParamChanged;
            builder.HeightChanged -= OnParamChanged;
            builder.EntrySpeedChanged -= OnParamChanged;

            base.OnDisable();            
        }
        

        private void OnParamChanged<T>(object sender, ParamChangeEventArgs<T> e)
        {
            UpdateLineRenderer();
            UpdateMeasureText();
        }

        public void UpdateLineRenderer()
        {
            // draw a line between the current line end point and the point where the mouse is pointing
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, lastLineElement.GetEndPoint());
            lineRenderer.SetPosition(1, builder.GetStartPoint());
        }

        public void UpdateMeasureText()
        {
            // make the text go along the line and lay flat on the terrain
            float camDistance = CameraManager.Instance.GetTDCamDistance();

            textMesh.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 4, camDistance)),
                Quaternion.LookRotation(-Vector3.up, Vector3.Cross(lastLineElement.GetRideDirection(), Vector3.up)));

            textMesh.GetComponent<TextMeshPro>().text = $"Distance: {builder.GetDistanceFromPreviousLineElement():F2}m";
            textMesh.GetComponent<TextMeshPro>().text += $"\nEntry speed: {PhysicsManager.MsToKmh(builder.EntrySpeed):F2}km/h";
        }

        public bool SetAndValidatePosition(Vector3 newPosition)
        {
            Vector3 lastElemEndPoint = lastLineElement.GetEndPoint();
            Vector3 rideDirection = Vector3.ProjectOnPlane(lastLineElement.GetRideDirection(), Vector3.up);

            // check for placing behind the last line element
            Vector3 potentialStartPoint = newPosition - rideDirection * builder.GetTransitionLengthXZ();
            Vector3 toStart = Vector3.ProjectOnPlane(potentialStartPoint - lastElemEndPoint, Vector3.up);
            float projection = Vector3.Dot(toStart, rideDirection);
            if (projection < 0)
            {
                StudioUIManager.Instance.ShowMessage("Cannot place the takeoff behind the previous line element.", 2f);
                return false;
            }

            float distanceToStartPoint = Vector3.Distance(potentialStartPoint, lastElemEndPoint);

            // if the projected point is too close to the last line element or too far from it, return
            if (distanceToStartPoint < TakeoffSettings.MIN_BUILD_DISTANCE)
            {
                StudioUIManager.Instance.ShowMessage($"The new obstacle position is too close to the last line element. The minimal distance is {TakeoffSettings.MIN_BUILD_DISTANCE}", 2f);
                return false;
            }
            else if (distanceToStartPoint >= distanceToFirstObstruction)
            {
                StudioUIManager.Instance.ShowMessage($"The new obstacle position is colliding with a terrain change or another obstacle.", 2f);
                return false;
            }
            else if (distanceToStartPoint > TakeoffSettings.MAX_BUILD_DISTANCE)
            {
                StudioUIManager.Instance.ShowMessage($"The new obstacle position is too far from the last line element. The maximum distance is {TakeoffSettings.MAX_BUILD_DISTANCE}.", 2f);
                return false;
            }

            Vector3 potentialEndPoint = potentialStartPoint + rideDirection * builder.GetLength();
            bool newPositionDoesNotCollide = TerrainManager.Instance.IsAreaFree(potentialStartPoint, potentialEndPoint, builder.GetBottomWidth());
            if (!newPositionDoesNotCollide)
            {
                StudioUIManager.Instance.ShowMessage("The new obstacle position collides with another obstacle or terrain change.", 2f);
                return false;
            }


            builder.SetPosition(newPosition);


            if (builder.GetExitSpeed() == 0)
            {
                StudioUIManager.Instance.ShowMessage("Not enough speed to even exit the takeoff. Please move it closer to the last built line element or adjust its parameters.", 2f);
                return false;
            }                        

            // check whether it is even possible to land far enough from the takeoff
            if (builder.GetFlightDistanceXZ() < TakeoffSettings.MIN_BUILD_DISTANCE)
            {
                StudioUIManager.Instance.ShowMessage($"There is not enough entry speed for the takeoff to fly further than {TakeoffSettings.MIN_BUILD_DISTANCE} away from the takeoff.", 2f);
                return false;
            }            

            return true;
        }

        /// <summary>
        /// Moves the highlight, its distance text and the line renderer to the point where the mouse is pointing on the terrain.
        /// </summary>
        /// <param name="newPosition">The hitpoint on the terrain where the mouse points</param>
        /// <returns>True if the move destination is valid</returns>
        public override bool TrySetPosition(Vector3 newPosition)
        {
            Vector3 lastElemEndPoint = lastLineElement.GetEndPoint();
            Vector3 rideDirection = Vector3.ProjectOnPlane(lastLineElement.GetRideDirection(), Vector3.up);

            // project the hit point on a line that goes from the last line element position in the direction of riding
            Vector3 projectedHitPoint = lastElemEndPoint + Vector3.Project(newPosition - lastElemEndPoint, rideDirection);

            if (!SetAndValidatePosition(projectedHitPoint))
            {                
                StudioUIManager.ToggleButton(buildButton, false);

                return false;
            }       
            
            StudioUIManager.ToggleButton(buildButton, true);

            StudioUIManager.Instance.HideMessage();

            return true;            
        }        
    }
}
