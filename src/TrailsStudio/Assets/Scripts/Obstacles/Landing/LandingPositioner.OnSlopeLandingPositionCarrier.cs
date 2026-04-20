using PhysicsManager;
using TerrainEditing.Slope;
using UnityEngine;

namespace Obstacles.Landing
{
    public partial class LandingPositioner
    {
        /// <summary>
        /// Represents a landing position carrier for scenarios where the landing is positioned on top of a slope change.
        /// </summary>
        /// <remarks>This class extends <see cref="LandingPositionCarrier"/> to provide additional
        /// functionality for handling slope-specific landing scenarios. It includes properties and methods to manage
        /// slope changes, tilted landings, and waypoint-based positioning. The slope angle and trajectory are adjusted
        /// based on the landing velocity and slope characteristics.</remarks>
        private class OnSlopeLandingPositionCarrier : LandingPositionCarrier
        {
            public readonly SlopeChange slope;
            public readonly bool isTilted;
            public readonly bool isWaypoint;

            public OnSlopeLandingPositionCarrier(Vector3 position, Trajectory trajectory, Vector3 edgePosition, Vector3 landingVelocityDirection, LandingPositioner positioner, SlopeChange slope, bool isWaypoint, bool isTilted)
                : base(position, trajectory, edgePosition, landingVelocityDirection, positioner)
            {
                this.slope = slope;
                this.isWaypoint = isWaypoint;

                this.isTilted = isTilted;
            }

            public override void MatchBuilder()
            {
                LandingBuilder landing = positioner.builder;

                if (isWaypoint)
                {
                    slope.PlaceObstacle(landing, landingPosition, isTilted);
                }
                else
                {
                    landing.SetPosition(landingPosition);
                }

                float slopeAngle = GetSlopeFromLandingVelocity(landingVelocityDirection, landing.GetTransform().up);
                landing.SetSlope(slopeAngle);

                landing.SetMatchingTrajectory(trajectory);
            }

            public override void MatchInvisibleBuilder()
            {
                LandingBuilder invisibleBuilder = positioner.invisibleBuilder;

                if (isWaypoint)
                {
                    slope.PlaceObstacle(invisibleBuilder, landingPosition, isTilted);
                }
                else
                {
                    invisibleBuilder.SetPosition(landingPosition);
                }

                float slopeAngle = GetSlopeFromLandingVelocity(landingVelocityDirection, invisibleBuilder.GetTransform().up);
                invisibleBuilder.SetSlope(slopeAngle);
            }
        }
    }
}