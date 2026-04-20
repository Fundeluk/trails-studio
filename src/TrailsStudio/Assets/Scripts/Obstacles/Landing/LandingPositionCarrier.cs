using PhysicsManager;
using UnityEngine;

namespace Obstacles.Landing
{
    public partial class LandingPositioner
    {
        /// <summary>
        /// Represents a carrier for landing position data, including the position, trajectory, and other related
        /// parameters used to configure and validate a landing.
        /// </summary>
        /// <remarks>This class encapsulates the data and operations required to manage a landing
        /// position, including matching builders to the landing configuration and validating the landing parameters. It
        /// is used in conjunction with a <see cref="LandingPositioner"/> to ensure the landing meets specific criteria
        /// such as slope angle and trajectory alignment.</remarks>
        private class LandingPositionCarrier
        {
            public readonly Vector3 landingPosition;
            public readonly Trajectory trajectory;
            public readonly Vector3 edgePosition;
            public readonly Vector3 landingVelocityDirection;

            public readonly LandingPositioner positioner;

            public LandingPositionCarrier(Vector3 position, Trajectory trajectory, Vector3 edgePosition, Vector3 landingVelocityDirection, LandingPositioner positioner)
            {
                landingPosition = position;
                this.trajectory = trajectory;
                this.landingVelocityDirection = landingVelocityDirection.normalized;
                this.edgePosition = edgePosition;
                this.positioner = positioner;
            }

            public virtual void MatchBuilder()
            {
                LandingBuilder landing = positioner.builder;
                landing.SetPosition(landingPosition);
                float slopeAngle = GetSlopeFromLandingVelocity(landingVelocityDirection, landing.GetTransform().up);
                landing.SetSlope(slopeAngle);

                landing.SetMatchingTrajectory(trajectory);
            }

            public virtual void MatchInvisibleBuilder()
            {
                LandingBuilder invisibleBuilder = positioner.invisibleBuilder;
                invisibleBuilder.SetPosition(landingPosition);
                float slopeAngle = GetSlopeFromLandingVelocity(landingVelocityDirection, invisibleBuilder.GetTransform().up);
                invisibleBuilder.SetSlope(slopeAngle);
            }            

            public bool IsValid()
            {
                float potentialSlopeDeg = GetSlopeFromLandingVelocity(landingVelocityDirection, positioner.invisibleBuilder.GetTransform().up) * Mathf.Rad2Deg;
                bool isSlopeAngleValid = potentialSlopeDeg >= LandingSettings.MinSlopeDeg && potentialSlopeDeg <= LandingSettings.MaxSlopeDeg;

                if (!isSlopeAngleValid)
                {
                    InternalDebug.Log("Rejected landing position: invalid slope angle: " + potentialSlopeDeg);
                    return false;
                }

                float angleBetweenRideDirAndVelocity = GetAngleBetweenRideDirAndVelocity(positioner.builder.GetRideDirection(), landingVelocityDirection);

                if (angleBetweenRideDirAndVelocity > LandingSettings.MaxAngleBetweenTrajectoryAndLandingDeg)
                {
                    InternalDebug.Log("Rejected landing position: angle between ride direction and landing velocity is too high: " + angleBetweenRideDirAndVelocity);
                    return false;
                }


                MatchInvisibleBuilder();                

                if (!positioner.ValidatePosition(positioner.invisibleBuilder))
                {
                    InternalDebug.Log("Rejected landing position: failed invisibleBuilder validation");
                    return false;
                }

                return true;
            }
        }
    }
}