using UnityEngine;

namespace Obstacles.Landing
{
    /// <summary>
    /// Represents a potential candidate position for landing placement on or near a slope.
    /// </summary>
    public readonly struct LandingCandidate
    {
        /// <summary>
        /// The calculated position of the landing.
        /// </summary>
        public readonly Vector3 Position;

        /// <summary>
        /// The position of the landing's edge.
        /// </summary>
        public readonly Vector3 EdgePosition;

        /// <summary>
        /// Indicates if the landing is placed as a waypoint on the slope.
        /// </summary>
        public readonly bool IsWaypoint;

        /// <summary>
        /// Indicates if the landing is tilted to match the slope's angle.
        /// </summary>
        public readonly bool IsTilted;

        public LandingCandidate(Vector3 position, Vector3 edgePosition, bool isWaypoint, bool isTilted)
        {
            this.Position = position;
            this.EdgePosition = edgePosition;
            this.IsWaypoint = isWaypoint;
            this.IsTilted = isTilted;
        }

        internal void Deconstruct(out Vector3 landingPosition, out Vector3 edgePosition)
        {
            landingPosition = Position;
            edgePosition = this.EdgePosition;
        }

        internal void Deconstruct(out Vector3 landingPosition, out Vector3 edgePosition, out bool isWaypoint, out bool isTilted)
        {
            landingPosition = Position;
            edgePosition = this.EdgePosition;
            isWaypoint = this.IsWaypoint;
            isTilted = this.IsTilted;
        }
    }
}