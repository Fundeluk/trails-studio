using Assets.Scripts;
using Assets.Scripts.Managers;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.WSA;


namespace Assets.Scripts.Builders
{
    /// <summary>
    /// Moves a highlight object anywhere after the last line element based on user input. <br/>
    /// Positions the highlight based on where the mouse is pointing on the terrain. Draws a line from the last line element to the highlight<br/>
    /// and shows distance from the line endpoint to the highlight + the Angle between the last line elements ride direction and the line.<br/>
    /// </summary>
    /// <remarks>Here, the highlight is the LandingBuilder mesh which is <b>attached to the same GameObject</b> as this highlighter.</remarks>
    public class LandingPositioner : Positioner
    {
        // the minimum and maximum distances between the last line element and new obstacle
        [Header("Build bounds")]
        [Tooltip("The minimum distance between the last line element and the new obstacle.")]
        public float minBuildDistance = 0.5f;
        [Tooltip("The maximum distance between the last line element and the new obstacle.")]
        public float maxBuildDistance = 15;

        [Tooltip("The minimum distance after the landing where the rideout area must be free of obstacles.")]
        public float landingClearanceDistance = 5f;


        private LandingBuilder builder;


        public override void OnEnable()
        {
            base.OnEnable();
            builder = gameObject.GetComponent<LandingBuilder>();
            builder.SetRideDirection(lastLineElement.GetRideDirection());

            float startToEdge = (builder.transform.position - builder.GetStartPoint()).magnitude;

            builder.SetPosition(lastLineElement.GetEndPoint() + (minBuildDistance + startToEdge) * lastLineElement.GetRideDirection());

            UpdateLineRenderer();

            GetComponent<MeshRenderer>().enabled = true;
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
            Vector3 toLanding = Vector3.ProjectOnPlane(builder.GetTransform().position - lastLineElement.GetTransform().position, Vector3.up);
            Vector3 rideDirProjected = Vector3.ProjectOnPlane(lastLineElement.GetRideDirection(), Vector3.up);
            textMesh.GetComponent<TextMeshPro>().text = $"Distance: {builder.GetDistanceFromPreviousLineElement():F2}m" +
                $"\nAngle: {(int)Vector3.SignedAngle(rideDirProjected, toLanding, Vector3.up):F2}°";
        }

        public bool TrySetRotation(float angle)
        {
            // the distances are calculated on the XZ plane to avoid the influence of the height of the terrain
            Vector3 lastElementEnd = lastLineElement.GetEndPoint();
            lastElementEnd.y = 0;

            float angleDiff = angle - builder.GetRotation();
            Vector3 rideDir = Quaternion.AngleAxis(angleDiff, Vector3.up) * transform.forward;

            ObstacleBounds newBounds = builder.GetBoundsForObstaclePosition(transform.position, rideDir);

            float distanceToStartPoint = Vector3.Distance(newBounds.startPoint, lastElementEnd);

            if (distanceToStartPoint > maxBuildDistance)
            {
                UIManager.Instance.ShowMessage($"The new obstacle position is too far from the last line element. The maximum distance is {maxBuildDistance}m", 2f);
                return false;
            }
            else if (distanceToStartPoint < minBuildDistance)
            {
                UIManager.Instance.ShowMessage($"The new obstacle position is too close to the last line element. The minimal distance is {minBuildDistance}m", 2f);
                return false;
            }

            bool newPositionCollides = !TerrainManager.Instance.IsAreaFree(newBounds.startPoint, newBounds.endPoint, builder.GetBottomWidth());
            if (newPositionCollides)
            {
                UIManager.Instance.ShowMessage("The new obstacle position is colliding with a terrain change or another obstacle.", 2f);
                return false;
            }

            // TODO make the width here parametrized by global setting
            bool newRideoutAreaDoesNotCollide = TerrainManager.Instance.IsAreaFree(newBounds.endPoint
                , newBounds.endPoint + newBounds.RideDirection * landingClearanceDistance, 1.5f);

            if (!newRideoutAreaDoesNotCollide)
            {
                UIManager.Instance.ShowMessage($"The rideout area after the landing is occupied by another obstacle or a terrain change.", 2f);
                return false;
            }

            UIManager.Instance.HideMessage();

            // place the highlight at the hit point
            builder.SetRotation(angle);

            // make the text go along the line and lay flat on the terrain
            float camDistance = CameraManager.Instance.GetTDCamDistance();


            textMesh.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 3, camDistance)),
                Quaternion.LookRotation(-Vector3.up, Vector3.Cross(lastLineElement.GetRideDirection(), Vector3.up)));

            UpdateMeasureText();

            UpdateLineRenderer();

            return true;

        }

        public override bool TrySetPosition(Vector3 hit)
        {
            // the distances are calculated on the XZ plane to avoid the influence of the height of the terrain
            Vector3 lastElementEnd = lastLineElement.GetEndPoint();
            lastElementEnd.y = 0;

            Vector3 lastElemRideDir = Vector3.ProjectOnPlane(lastLineElement.GetRideDirection(), Vector3.up);

            Vector3 toHit = Vector3.ProjectOnPlane(hit - lastElementEnd, Vector3.up);

            ObstacleBounds newBounds = builder.GetBoundsForObstaclePosition(hit, builder.GetRideDirection());

            float distanceToStartPoint = Vector3.Distance(newBounds.startPoint, lastElementEnd);

            // prevent the landing from being placed behind the takeoff
            float projection = Vector3.Dot(toHit, lastElemRideDir);
            if (projection < 0)                
            {
                UIManager.Instance.ShowMessage("Cannot place the landing at an angle larger than 90 degrees with respect to its takeoff.", 2f);
                return false;
            }
            else if (distanceToStartPoint > maxBuildDistance)
            {
                UIManager.Instance.ShowMessage($"The new obstacle position is too far from the last line element. The maximum distance is {maxBuildDistance}m", 2f);
                return false;
            }            
            else if (distanceToStartPoint < minBuildDistance)
            {
                UIManager.Instance.ShowMessage($"The new obstacle position is too close to the last line element. The minimal distance is {minBuildDistance}m", 2f);
                return false;
            }            

            bool newPositionCollides = !TerrainManager.Instance.IsAreaFree(newBounds.startPoint, newBounds.endPoint, builder.GetBottomWidth());
            if (newPositionCollides)
            {
                UIManager.Instance.ShowMessage("The new obstacle position is colliding with a terrain change or another obstacle.", 2f);
                return false;
            }

            // TODO make the width here parametrized by global setting
            bool newRideoutAreaDoesNotCollide = TerrainManager.Instance.IsAreaFree(newBounds.endPoint
                , newBounds.endPoint + newBounds.RideDirection * landingClearanceDistance, 1.5f);

            if (!newRideoutAreaDoesNotCollide)
            {
                UIManager.Instance.ShowMessage($"The rideout area after the landing is occupied by another obstacle or a terrain change.", 2f);
                return false;
            }

            UIManager.Instance.HideMessage();

            // place the highlight at the hit point
            builder.SetPosition(hit);

            // make the text go along the line and lay flat on the terrain
            float camDistance = CameraManager.Instance.GetTDCamDistance();


            textMesh.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 3, camDistance)), 
                Quaternion.LookRotation(-Vector3.up, Vector3.Cross(lastLineElement.GetRideDirection(), Vector3.up)));

            UpdateMeasureText();

            UpdateLineRenderer();

            return true;            
        }
    }
}
