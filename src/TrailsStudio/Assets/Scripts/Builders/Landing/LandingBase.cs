using Assets.Scripts.Managers;
using System;
using System.Collections;
using System.IO.Pipes;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts.Builders
{
    public abstract class LandingBase : ObstacleBase<LandingMeshGenerator>
    {
        public event EventHandler<ParamChangeEventArgs<float>> SlopeChanged;
        protected void OnSlopeChanged(float newSlope)
        {
            SlopeChanged?.Invoke(this, new ParamChangeEventArgs<float>("Slope", newSlope));
        }

        public event EventHandler<ParamChangeEventArgs<float>> ExitSpeedChanged;
        protected void OnExitSpeedChanged(float newExitSpeed)
        {
            ExitSpeedChanged?.Invoke(this, new ParamChangeEventArgs<float>("ExitSpeed", newExitSpeed));
        }

        public Takeoff PairedTakeoff { get; protected set; }

        /// <summary>
        /// A flight trajectory that matches the one from <see cref="PairedTakeoff"/>."/>
        /// </summary>
        public Trajectory MatchingTrajectory { get; protected set; } = null;

        /// <summary>
        /// The speed at which a rider exits the landing in meters per second.
        /// </summary>
        public float ExitSpeed { get; protected set; } = 0f;

        public override Vector3 GetEndPoint() => meshGenerator.transform.position + (meshGenerator.CalculateRadiusLengthXZ() + meshGenerator.CalculateSlopeLengthXZ()) * meshGenerator.transform.forward;

        public override Vector3 GetStartPoint() => GetTransform().position - ((GetThickness() + GetHeight() * GetSideSlope()) * GetRideDirection());

        /// <returns>Distance from start point to end point in meters.</returns>
        public override float GetLength() => meshGenerator.CalculateLength();

        public float GetSlopeLength() => meshGenerator.CalculateSlopeLength();

        public float GetRadius() => meshGenerator.CalculateRadius();

        public float GetLandingAreaLengthXZ() => meshGenerator.CalculateLandingAreaLengthXZ();

        public float GetLandingAreaLength() => meshGenerator.CalculateLandingAreaLength();


        /// <returns>The position on the landing's edge at which the rider will land.</returns>
        public Vector3 GetLandingPoint() => GetTransform().position + GetHeight() * GetTransform().up;
  
        /// <returns>The direction at which the rider will land on the landing adjusted for its slope angle.</returns>
        public Vector3 GetLandingDirection()
        {
            // Get the ride direction (which is along the XZ plane)
            Vector3 rideDir = GetRideDirection().normalized;

            // Get the end angle of the takeoff in radians
            float endAngle = GetSlopeAngle();

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
            Vector3 takeoffForwardOnFlat = Vector3.ProjectOnPlane(PairedTakeoff.GetRideDirection().normalized, Vector3.up);
            Vector3 landingForwardOnFlat = Vector3.ProjectOnPlane(meshGenerator.transform.forward, Vector3.up);
            return Vector3.SignedAngle(takeoffForwardOnFlat, landingForwardOnFlat, GetTransform().up);
        }        
        
        /// <summary>
        /// Measure distance from takeoff's edge to this landing's edge.
        /// </summary>
        public override float GetDistanceFromPreviousLineElement()
        {
            return Vector3.Distance(PairedTakeoff.GetTransitionEnd(), GetLandingPoint());
        }

        /// <returns>Current slope angle of the landing in radians.</returns>
        public float GetSlopeAngle() => meshGenerator.Slope;

        public float GetExitSpeed() => ExitSpeed;

        private void OnDrawGizmosSelected()
        {
            if (meshGenerator != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(GetLandingPoint(), 0.2f);
                Gizmos.DrawLine(GetLandingPoint(), GetLandingPoint() + GetLandingDirection() * 3);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(GetEndPoint(), 0.2f);
                Gizmos.DrawSphere(GetStartPoint(), 0.2f);


            }
        }
    }
}