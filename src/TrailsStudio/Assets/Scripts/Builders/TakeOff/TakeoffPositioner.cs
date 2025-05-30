using Assets.Scripts;
using Assets.Scripts.Managers;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;


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
        // the minimum and maximum distances between the last line element and new obstacle
        [Header("Build newBounds")]
        [Tooltip("The minimum distance between the last line element and the new obstacle.")]
        public float minBuildDistance = 1;
        [Tooltip("The maximum distance between the last line element and the new obstacle.")]
        public float maxBuildDistance = 30;

        private TakeoffBuilder builder;

        /// <summary>
        /// The distance to position of the first obstruction on the way from the last line element to the takeoff.
        /// The default value is <see cref="maxBuildDistance"/>.<br/>
        /// Used to prevent placing a takeoff after an obstruction so that the ride path to it is free of obstacles.
        /// </summary>
        private float distanceToFirstObstruction;


        public override void OnEnable()
        {
            base.OnEnable();

            builder = gameObject.GetComponent<TakeoffBuilder>();

            builder.SetRideDirection(lastLineElement.GetRideDirection());

            // position the highlight at minimal build distance from the last line element
            builder.SetPosition(lastLineElement.GetEndPoint() + (minBuildDistance + builder.GetCurrentRadiusLength()) * lastLineElement.GetRideDirection());

            baseBuilder = builder;

            UpdateLineRenderer();

            GetComponent<MeshRenderer>().enabled = true;

            distanceToFirstObstruction = TerrainManager.Instance.GetRideableDistance(lastLineElement.GetEndPoint(), lastLineElement.GetRideDirection(), 1.5f, lastLineElement.GetEndPoint().y, maxBuildDistance);
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
            textMesh.GetComponent<TextMeshPro>().text = $"Distance: {builder.GetDistanceFromPreviousLineElement():F2}m";
            textMesh.GetComponent<TextMeshPro>().text += $"\nEntry speed: {PhysicsManager.MsToKmh(builder.EntrySpeed):F2}km/h";
        }

        /// <summary>
        /// Moves the highlight, its distance text and the line renderer to the point where the mouse is pointing on the terrain.
        /// </summary>
        /// <param name="hit">The hitpoint on the terrain where the mouse points</param>
        /// <returns>True if the move destination is valid</returns>
        public override bool TrySetPosition(Vector3 hit)
        {
            Vector3 lastElemEndPoint = lastLineElement.GetEndPoint();
            Vector3 rideDirection = Vector3.ProjectOnPlane(lastLineElement.GetRideDirection(), Vector3.up);

            // project the hit point on a line that goes from the last line element position in the direction of riding
            Vector3 projectedHitPoint = lastElemEndPoint + Vector3.Project(hit - lastElemEndPoint, rideDirection) ;

            ObstacleBounds newBounds = builder.GetBoundsForObstaclePosition(projectedHitPoint, rideDirection);            

            float distanceToStartPoint = Vector3.Distance(newBounds.startPoint, lastElemEndPoint);

            // if the projected point is too close to the last line element or too far from it, return
            if (distanceToStartPoint < minBuildDistance)
            {
                UIManager.Instance.ShowMessage($"The new obstacle position is too close to the last line element. The minimal distance is {minBuildDistance}m", 2f);
                return false;
            }
            else if (distanceToStartPoint > distanceToFirstObstruction)
            {
                UIManager.Instance.ShowMessage($"The new obstacle position is colliding with a terrain change or another obstacle.", 2f);
                return false;
            }
            else if (distanceToStartPoint > maxBuildDistance)
            {
                UIManager.Instance.ShowMessage($"The new obstacle position is too far from the last line element. The maximum distance is {maxBuildDistance}m.", 2f);
                return false;
            }

            bool newPositionDoesNotCollide = TerrainManager.Instance.IsAreaFree(newBounds.startPoint, newBounds.endPoint, builder.GetBottomWidth());
            if (!newPositionDoesNotCollide)
            {
                UIManager.Instance.ShowMessage("The new obstacle position collides with another obstacle or terrain change.", 2f);
                return false;
            }

            UIManager.Instance.HideMessage();

            builder.SetPosition(projectedHitPoint);

            // make the text go along the line and lay flat on the terrain
            float camDistance = CameraManager.Instance.GetTDCamDistance();

            textMesh.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 4, camDistance)), 
                Quaternion.LookRotation(-Vector3.up, Vector3.Cross(lastLineElement.GetRideDirection(), Vector3.up)));

            UpdateLineRenderer();

            return true;            
        }        
    }
}
