using UnityEngine;

namespace TerrainEditing.Slope
{
    public partial class SlopeChange
    {
        /// <summary>
        /// Represents the result of placing a line element (takeoff or landing) on or near a slope.
        /// This record captures the state changes that occur when an obstacle is positioned, including
        /// the updated slope geometry and affected terrain heightmap coordinates.
        /// </summary>
        /// <remarks>
        /// <para>
        /// PlacementResult is used to track both tentative and confirmed placements of obstacles on slopes.
        /// It serves as an intermediate state holder that can be used to preview changes before they are
        /// committed to the slope's state.
        /// </para>
        /// <para>
        /// The placement result is stored in <see cref="SlopeChange.lastPlacementResult"/> and is later
        /// confirmed via <see cref="SlopeChange.ConfirmChanges{T}"/> which applies the
        /// changes permanently to the slope.
        /// </para>
        /// </remarks>
        public record PlacementResult
        {
            /// <summary>
            /// The remaining length of the slope after placement.
            /// </summary>
            public float RemainingLength { get; private set; } = 0f;

            /// <summary>
            /// The new calculated endpoint of the active slope segment.
            /// </summary>
            public Vector3 NewEndPoint { get; private set; } = Vector3.zero;

            /// <summary>
            /// Indicates whether the newly placed element is considered a waypoint inside the slope bounds.
            /// </summary>
            public bool IsWaypoint { get; private set; } = false;

            /// <summary>
            /// The specific heightmap coordinates that were modified by this placement.
            /// </summary>
            public TerrainManager.HeightmapCoordinates ChangedHeightmapCoords { get; private set; } = null;

            /// <summary>
            /// Initializes a new instance of the <see cref="PlacementResult"/> record with individual parameters.
            /// </summary>
            public PlacementResult(float remainingLength, Vector3 newEndPoint, bool isWaypoint, TerrainManager.HeightmapCoordinates changedHeightmapCoords)
            {
                RemainingLength = remainingLength;
                NewEndPoint = newEndPoint;
                IsWaypoint = isWaypoint;
                ChangedHeightmapCoords = changedHeightmapCoords;
            }     
            
            /// <summary>
            /// Initializes a new instance of the <see cref="PlacementResult"/> record directly from the current state of a slope.
            /// </summary>
            public PlacementResult(SlopeChange slopeChange)
            {
                RemainingLength = slopeChange.RemainingLength;
                NewEndPoint = slopeChange.EndPoint;                
            }

            public void Discard(float restoreHeight)
            {
                ChangedHeightmapCoords?.SetHeight(restoreHeight);
            }
        }
    }
}