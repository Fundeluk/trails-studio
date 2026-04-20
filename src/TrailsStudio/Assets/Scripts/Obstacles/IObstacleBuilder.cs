using TerrainEditing;
using TerrainEditing.Slope;
using UnityEngine;

namespace Obstacles
{
    public interface IBuilder
    {
        public void SetPosition(Vector3 position);

        public void SetRideDirection(Vector3 rideDirection);

        public Vector3 GetStartPoint();

        public Vector3 GetEndPoint();

        public Transform GetTransform();

        public Vector3 GetRideDirection();

        public void CanBuild(bool canBuild);

        public void DestroyUnderlyingGameObject();
    }

    public interface IObstacleBuilder : IBuilder
    {
        public void SetHeight(float height);

        public void SetWidth(float width);

        public void SetThickness(float thickness);        

        public void RecalculateCameraTargetPosition();
        
        /// <summary>
        /// Calculates and applies the placement of this obstacle on the given slope.
        /// </summary>
        /// <param name="slope">The slope this obstacle is being placed on.</param>
        /// <param name="rawPosition">The raw cursor/hit position.</param>
        /// <param name="isTilted">Whether the obstacle should tilt to match the slope.</param>
        /// <returns>The placement result containing new slope bounds and modified terrain coordinates.</returns>
        SlopeChange.PlacementResult PlaceOnSlope(SlopeChange slope, Vector3 rawPosition, bool isTilted = false);
     

        public TerrainManager.HeightmapCoordinates GetObstacleHeightmapCoordinates();
    }
}