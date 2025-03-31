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

        public override Vector3 GetStartPoint() => GetTransform().position - ((GetThickness() + GetHeight() * LandingMeshGenerator.sideSlope) * GetRideDirection());

        public override float GetLength() => meshGenerator.CalculateLength();

        /// <summary>
        /// Retrieves current angle between the ride direction of the landing and its takeoff.
        /// </summary>
        /// <returns>Angle in degrees. Negative values mean rotation to the left of the ride direction, positive to the right.</returns>
        public int GetRotation() => (int)Vector3.SignedAngle(takeoff.GetRideDirection().normalized, meshGenerator.transform.forward, Vector3.up);

        /// <returns>Current slope of the landing in degrees.</returns>
        public float GetSlope() => meshGenerator.Slope * Mathf.Rad2Deg;
    }
}