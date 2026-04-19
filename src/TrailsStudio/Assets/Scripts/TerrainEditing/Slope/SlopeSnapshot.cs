using UnityEngine;

namespace TerrainEditing.Slope
{
    public partial class SlopeChange
    {
        /// <summary>
        /// A snapshot class capturing the state of a slope, permitting reversions and undo operations.
        /// </summary>
        public record SlopeSnapshot
        {
            /// <summary>
            /// The slope instance associated with this recorded snapshot.
            /// </summary>
            public readonly SlopeChange Slope;

            /// <summary>
            /// Denotes if the slope was finished when this snapshot was created.
            /// </summary>
            public readonly bool Finished;
            
            public readonly float RemainingLength;

            /// <summary>
            /// The recorded width value of the slope during this snapshot.
            /// </summary>
            public readonly float Width;

            /// <summary>
            /// The point marking the active edge or end point inside the span in this history record.
            /// </summary>
            public readonly Vector3 EndPoint;
            public readonly Vector3 LastRideDir;

            /// <summary>
            /// Captures the current active state of a slope instance.
            /// </summary>
            public SlopeSnapshot(SlopeChange slope)
            {
                this.Slope = slope;
                Finished = slope.Finished;
                RemainingLength = slope.RemainingLength;
                Width = slope.Width;
                EndPoint = slope.EndPoint;
                LastRideDir = slope.LastRideDirection;
            }

            /// <summary>
            /// Initializes a new snapshot entry with specific recorded states, predominantly used for deserialization.
            /// </summary>
            public SlopeSnapshot(SlopeChange slope, bool finished, float remainingLength, float width, Vector3 endPoint, Vector3 lastRideDir)
            {
                this.Slope = slope;
                this.Finished = finished;
                this.RemainingLength = remainingLength;
                this.Width = width;
                this.EndPoint = endPoint;
                this.LastRideDir = lastRideDir;
            }

            /// <summary>
            /// Restores the slope reference back to the configuration stored in this snapshot frame, discarding subsequent changes.
            /// </summary>
            public void Revert()
            {
                if (!Finished)
                {
                    TerrainManager.Instance.ActiveSlope = Slope;
                }
                
                Slope.RemainingLength = RemainingLength;
                Slope.EndPoint = EndPoint;
                Slope.Width = Width;
                Slope.LastRideDirection = LastRideDir;


                Slope.UpdateHighlight();
            }
        }
    }
}