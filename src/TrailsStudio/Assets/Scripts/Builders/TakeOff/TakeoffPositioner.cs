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

        private TakeoffBuilder invisibleBuilder;

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
            invisibleBuilder = builder.InvisibleClone;

            base.OnEnable();            

            builder.PositionChanged += OnParamChanged;
            builder.RotationChanged += OnParamChanged;
            builder.RadiusChanged += OnParamChanged;
            builder.HeightChanged += OnParamChanged;
            builder.EntrySpeedChanged += OnParamChanged;            

            builder.SetRideDirection(lastLineElement.GetRideDirection());
            invisibleBuilder.SetRideDirection(lastLineElement.GetRideDirection());
            invisibleBuilder.SetPosition(builder.GetTransform().position);

            // position the highlight at minimal build distance from the last line element
            builder.SetPosition(lastLineElement.GetEndPoint() + (TakeoffConstants.MIN_BUILD_DISTANCE + builder.GetCurrentRadiusLength()) * lastLineElement.GetRideDirection());    
            
            buildButton = UIManager.Instance.takeOffBuildUI.GetComponent<TakeOffBuildUI>().BuildButton;


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
                
                UIManager.Instance.ShowMessage(message, 5f);
                UIManager.ToggleButton(buildButton, false);
            }

            UpdateLineRenderer();

            GetComponent<MeshRenderer>().enabled = true;

            distanceToFirstObstruction = TerrainManager.Instance.GetRideableDistance(lastLineElement.GetEndPoint(), lastLineElement.GetRideDirection(), clearanceWidth, lastLineElement.GetEndPoint().y, TakeoffConstants.MAX_BUILD_DISTANCE);

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

        public bool TryChangeParamsForNonZeroExitSpeed()
        {
            while (invisibleBuilder.GetExitSpeed() == 0)
            {
                bool paramsChanged = false;
                if (invisibleBuilder.GetRadius() <= invisibleBuilder.GetHeight() && invisibleBuilder.GetRadius() <= TakeoffConstants.MAX_RADIUS)
                {
                    invisibleBuilder.SetRadius(invisibleBuilder.GetRadius() + 0.1f);
                    paramsChanged = true;
                }

                if (invisibleBuilder.GetHeight() >= invisibleBuilder.GetRadius() && invisibleBuilder.GetHeight() >= TakeoffConstants.MIN_HEIGHT)
                {
                    invisibleBuilder.SetHeight(invisibleBuilder.GetHeight() - 0.1f);
                    paramsChanged = true;
                }

                if (!paramsChanged)
                {
                    return false;
                }                
            }

            builder.SetRadius(invisibleBuilder.GetRadius());
            builder.SetHeight(invisibleBuilder.GetHeight());

            return true;
        }

        // tries the proposed position with the invisible builder and confirm its validity with it.
        public bool ValidatePosition(Vector3 newPosition)
        {
            
            invisibleBuilder.SetPosition(newPosition);


            if (invisibleBuilder.GetExitSpeed() == 0)
            {
                invisibleBuilder.SetPosition(transform.position);
                UIManager.Instance.ShowMessage("Not enough speed to even exit the takeoff. Please move it closer to the last built line element or adjust its parameters.", 2f);
                return false;
            }
            

            Vector3 lastElemEndPoint = lastLineElement.GetEndPoint();
            Vector3 rideDirection = Vector3.ProjectOnPlane(lastLineElement.GetRideDirection(), Vector3.up);

            // check for placing behind the last line element
            Vector3 toHit = Vector3.ProjectOnPlane(invisibleBuilder.GetTransform().position - lastElemEndPoint, Vector3.up);
            float projection = Vector3.Dot(toHit, rideDirection);
            if (projection < 0)
            {
                UIManager.Instance.ShowMessage("Cannot place the takeoff behind the previous line element.", 2f);
                invisibleBuilder.SetPosition(transform.position);
                return false;
            }                       

            float distanceToStartPoint = Vector3.Distance(invisibleBuilder.GetStartPoint(), lastElemEndPoint);

            // if the projected point is too close to the last line element or too far from it, return
            if (distanceToStartPoint < TakeoffConstants.MIN_BUILD_DISTANCE)
            {
                UIManager.Instance.ShowMessage($"The new obstacle position is too close to the last line element. The minimal distance is {TakeoffConstants.MIN_BUILD_DISTANCE}m", 2f);
                invisibleBuilder.SetPosition(transform.position);
                return false;
            }
            else if (distanceToStartPoint > distanceToFirstObstruction)
            {
                UIManager.Instance.ShowMessage($"The new obstacle position is colliding with a terrain change or another obstacle.", 2f);
                invisibleBuilder.SetPosition(transform.position);
                return false;
            }
            else if (distanceToStartPoint > TakeoffConstants.MAX_BUILD_DISTANCE)
            {
                UIManager.Instance.ShowMessage($"The new obstacle position is too far from the last line element. The maximum distance is {TakeoffConstants.MAX_BUILD_DISTANCE}m.", 2f);
                invisibleBuilder.SetPosition(transform.position);
                return false;
            }            

            // check whether it is even possible to land far enough from the takeoff
            if (invisibleBuilder.GetFlightDistanceXZ() < TakeoffConstants.MIN_BUILD_DISTANCE)
            {
                UIManager.Instance.ShowMessage($"There is not enough entry speed for the takeoff to fly further than {TakeoffConstants.MIN_BUILD_DISTANCE}m away from the takeoff.", 2f);
                invisibleBuilder.SetPosition(transform.position);
                return false;
            }

            bool newPositionDoesNotCollide = TerrainManager.Instance.IsAreaFree(invisibleBuilder.GetStartPoint(), invisibleBuilder.GetEndPoint(), builder.GetBottomWidth());
            if (!newPositionDoesNotCollide)
            {
                invisibleBuilder.SetPosition(transform.position);
                UIManager.Instance.ShowMessage("The new obstacle position collides with another obstacle or terrain change.", 2f);
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

            if (!ValidatePosition(projectedHitPoint))
            {
                // revert position of the invisible builder to the actual builder position
                invisibleBuilder.SetPosition(transform.position);                

                UIManager.ToggleButton(buildButton, false);

                return false;
            }       
            
            UIManager.ToggleButton(buildButton, true);

            UIManager.Instance.HideMessage();

            builder.SetPosition(projectedHitPoint);            

            return true;            
        }        
    }
}
