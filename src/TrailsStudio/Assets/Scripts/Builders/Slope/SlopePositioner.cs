using Assets.Scripts.Managers;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using System.Collections;
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
        // the minimum and maximum distances between the last line element and new obstacle
        [Header("Build bounds")]
        [Tooltip("The minimum distance between the last line element and the new obstacle.")]
        public float minBuildDistance = 0;
        [Tooltip("The maximum distance between the last line element and the new obstacle.")]
        public float maxBuildDistance = 30;       

        private SlopeChangeBuilder builder;
        
        public override void OnEnable()
        {
            base.OnEnable();

            builder = GetComponent<SlopeChangeBuilder>();
            builder.enabled = true;
            
            // move highlight in front of the last line element and make it 
            Vector3 position = lastLineElement.GetEndPoint() + lastLineElement.GetRideDirection().normalized;

            builder.SetPosition(position);

            baseBuilder = builder;
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
                UIManager.Instance.ShowMessage("Cannot place the slope change behind the previous line element.", 2f);
                return false;
            }

            // if the projected point is too close to the last line element or too far from it, return
            if (toHit.magnitude < minBuildDistance)
            {
                UIManager.Instance.ShowMessage($"Slope must be at least {minBuildDistance:F2}m away from the last line element.", 2f);
                return false;
            }
            else if (toHit.magnitude > maxBuildDistance)
            {
                UIManager.Instance.ShowMessage($"Slope must be at most {maxBuildDistance:F2}m away from the last line element.", 2f);
                return false;
            }
            else if (!builder.IsBuildable(projectedHitPoint, builder.Length, rideDirection))
            {
                UIManager.Instance.ShowMessage("Slope cannot be built here. The area is occupied.", 2f);
                return false;
            }

            // check if the slope can be reached and whether it can be traveled at the current speed
            if (PhysicsManager.TryCalculateExitSpeed(Line.Instance.GetLastLineElement().GetExitSpeed(), 
                Vector3.Distance(Line.Instance.GetLastLineElement().GetEndPoint(), position), out float entrySpeed))
            {
                // check if the whole slope can be traveled
                float slopeLength = builder.Length;
                float slopeAngle = builder.Angle;

                if (PhysicsManager.TryCalculateExitSpeed(entrySpeed, slopeLength, out float exitSpeed, slopeAngle))
                {
                    if (exitSpeed < Line.MIN_EXIT_SPEED_MS)
                    {
                        UIManager.Instance.ShowMessage($"The speed at the slope end is smaller than the limit: {PhysicsManager.MsToKmh(Line.MIN_EXIT_SPEED_MS)}km/h.");
                        return false;
                    }                                                            
                }
                else
                {
                    UIManager.Instance.ShowMessage("The slope end cannot be reached: Insufficient speed.", 2f);
                }
            }
            else
            {
                UIManager.Instance.ShowMessage("The slope cannot be reached: Insufficient speed.", 2f);
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

            Vector3 toHit = projectedHitPoint - endPoint;

            UIManager.Instance.HideMessage();

            builder.SetPosition(projectedHitPoint);

            float distance = Vector3.Distance(projectedHitPoint, endPoint);

            // position the text in the middle of the screen

            // make the text go along the line and lay flat on the terrain
            float camDistance = CameraManager.Instance.GetTDCamDistance();
            textMesh.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, camDistance)), Quaternion.LookRotation(-Vector3.up, Vector3.Cross(toHit, Vector3.up)));
            textMesh.GetComponent<TextMeshPro>().text = $"Distance: {distance:F2}m";

            // draw a line between the current line end point and the point where the mouse is pointing
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, endPoint + 0.1f * Vector3.up);
            lineRenderer.SetPosition(1, builder.GetStartPoint());

            return true;            
        }                 
    }
}