using System.Collections;
using UnityEngine;
using Assets.Scripts.Managers;
using Unity.VisualScripting;

namespace Assets.Scripts.Builders
{
    public abstract class LandingBase : ObstacleBase<LandingMeshGenerator>
    {
        protected Takeoff takeoff;

        public override Vector3 GetEndPoint() => meshGenerator.transform.position + (meshGenerator.CalculateRadiusLength() + meshGenerator.CalculateSlopeLength()) * meshGenerator.transform.forward;

        public override Vector3 GetStartPoint() => GetTransform().position - ((GetThickness() + GetHeight() * GetSideSlope()) * GetRideDirection());

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
        public int GetRotation() => (int)Vector3.SignedAngle(takeoff.GetRideDirection().normalized, meshGenerator.transform.forward, Vector3.up);

        /// <returns>Current slope of the landing in degrees.</returns>
        public float GetSlope() => meshGenerator.Slope;

        private void OnDrawGizmosSelected()
        {
            if (meshGenerator != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(GetLandingPoint(), 0.5f);
                Gizmos.DrawLine(GetLandingPoint(), GetLandingPoint() + GetLandingDirection() * 3);
            }
        }
    }
}