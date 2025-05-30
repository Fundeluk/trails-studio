using Assets.Scripts.Managers;
using System.Collections;
using System.IO.Pipes;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts.Builders
{
    public abstract class LandingBase : ObstacleBase<LandingMeshGenerator>
    {
        protected Takeoff takeoff;

        public override Vector3 GetEndPoint() => meshGenerator.transform.position + (meshGenerator.CalculateRadiusLength() + meshGenerator.CalculateSlopeLength()) * meshGenerator.transform.forward;

        public override Vector3 GetStartPoint() => GetTransform().position - ((GetThickness() + GetHeight() * GetSideSlope()) * GetRideDirection());

        /// <returns>Distance from start point to end point in meters.</returns>
        public override float GetLength() => meshGenerator.CalculateLength();


        /// <returns>The position on the landing's edge at which the rider will land.</returns>
        public Vector3 GetLandingPoint() => GetTransform().position + GetHeight() * GetTransform().up;
  
        /// <returns>The direction at which the rider will land on the landing adjusted for its slope angle.</returns>
        public Vector3 GetLandingDirection()
        {
            // Get the ride direction (which is along the XZ plane)
            Vector3 rideDir = GetRideDirection().normalized;

            // Get the end angle of the takeoff in radians
            float endAngle = GetSlope();

            // Create a rotation around the axis perpendicular to ride direction and up vector
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, rideDir).normalized;

            // Rotate the ride direction by the end angle
            // The rotation is around the local X axis of the takeoff (perpendicular to ride direction)
            Quaternion rotation = Quaternion.AngleAxis(endAngle * Mathf.Rad2Deg, rotationAxis);

            return (rotation * rideDir).normalized;
        }

        /// <summary>
        /// Retrieves current Angle between the ride direction of the landing and its takeoff.
        /// </summary>
        /// <returns>Angle in degrees. Negative values mean rotation to the left of the ride direction, positive to the right.</returns>
        public float GetRotation()
        {
            Vector3 takeoffForwardOnFlat = Vector3.ProjectOnPlane(takeoff.GetRideDirection().normalized, Vector3.up);
            Vector3 landingForwardOnFlat = Vector3.ProjectOnPlane(meshGenerator.transform.forward, Vector3.up);
            return Vector3.SignedAngle(takeoffForwardOnFlat, landingForwardOnFlat, GetTransform().up);
        }  
        
        public override ObstacleBounds GetBoundsForObstaclePosition(Vector3 position, Vector3 rideDir)
        {
            position.y = 0;
            rideDir = Vector3.ProjectOnPlane(rideDir, Vector3.up).normalized;
            Vector3 rightDir = -Vector3.Cross(rideDir, Vector3.up).normalized;
            Vector3 startPoint = position - (GetThickness() + GetHeight() * GetSideSlope()) * rideDir;
            Vector3 endPoint = startPoint + GetLength() * rideDir;
            Vector3 leftStartCorner = startPoint - (GetBottomWidth() / 2) * rightDir;
            Vector3 rightStartCorner = startPoint + (GetBottomWidth() / 2) * rightDir;
            Vector3 leftEndCorner = endPoint - (GetBottomWidth() / 2) * rightDir;
            Vector3 rightEndCorner = endPoint + (GetBottomWidth() / 2) * rightDir;
            return new ObstacleBounds(startPoint, leftStartCorner, rightStartCorner, endPoint, leftEndCorner, rightEndCorner);
        }

        /// <returns>Current slope of the landing in radians.</returns>
        public float GetSlope() => meshGenerator.Slope;

        private void OnDrawGizmosSelected()
        {
            if (meshGenerator != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(GetLandingPoint(), 0.5f);
                Gizmos.DrawLine(GetLandingPoint(), GetLandingPoint() + GetLandingDirection() * 3);
                Gizmos.DrawLine(GetEndPoint(), GetEndPoint() + TerrainManager.GetNormalForWorldPosition(GetEndPoint()) * 3);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(GetEndPoint(), GetEndPoint() + GetTransform().up * 10);
            }
        }
    }
}