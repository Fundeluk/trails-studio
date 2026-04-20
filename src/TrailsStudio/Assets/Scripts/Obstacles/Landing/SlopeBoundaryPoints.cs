using UnityEngine;

namespace Obstacles.Landing
{
    /// <summary>
    /// Contains positional limits and edge points around a slope's boundaries, used for calculations like landing placement.
    /// </summary>
    public readonly struct SlopeBoundaryPoints
    {
        /// <summary>
        /// The last possible placement edge candidate resting before the start of the slope.
        /// </summary>
        public readonly Vector3? LastBeforeSlopeEdge;

        /// <summary>
        /// The first possible placement edge candidate lying on the slope geometry.
        /// </summary>
        public readonly Vector3? FirstOnSlopeEdge;

        /// <summary>
        /// The final possible placement edge candidate lying right on the slope, before the slope ends.
        /// </summary>
        public readonly Vector3? LastOnSlopeEdge;

        /// <summary>
        /// The first viable placement edge candidate situated on the flat terrain right after the slope end.
        /// </summary>
        public readonly Vector3 FirstAfterSlopeEdge;
        public SlopeBoundaryPoints(Vector3? lastBeforeSlopeEdge, Vector3? firstOnSlopeEdge, Vector3? lastOnSlopeEdge, Vector3 firstAfterSlopeEdge)
        {
            this.LastBeforeSlopeEdge = lastBeforeSlopeEdge;
            this.FirstOnSlopeEdge = firstOnSlopeEdge;
            this.LastOnSlopeEdge = lastOnSlopeEdge;
            this.FirstAfterSlopeEdge = firstAfterSlopeEdge;
        }

        internal void Deconstruct(out Vector3? lastBeforeSlopeEdge, out Vector3? firstOnSlopeEdge, out Vector3? lastOnSlopeEdge, out Vector3 firstAfterSlopeEdge)
        {
            lastBeforeSlopeEdge = this.LastBeforeSlopeEdge;
            firstOnSlopeEdge = this.FirstOnSlopeEdge;
            lastOnSlopeEdge = this.LastOnSlopeEdge;
            firstAfterSlopeEdge = this.FirstAfterSlopeEdge;
        }
    }
}