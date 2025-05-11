using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders
{
    public abstract class TakeoffBase : ObstacleBase<TakeoffMeshGenerator>
    {
        /// <summary>
        /// The speed at which a rider enters the transition in meters per second.
        /// </summary>
        public float EntrySpeed { get; protected set; } = 0f;

        public override void Initialize(TakeoffMeshGenerator meshGenerator, Terrain terrain, GameObject cameraTarget, ILineElement previousLineElement)
        {
            base.Initialize(meshGenerator, terrain, cameraTarget, previousLineElement);
            UpdateEntrySpeed();
        }                

        public override Vector3 GetEndPoint() => GetTransform().position + GetRideDirection().normalized * (meshGenerator.Thickness + GetHeight() * GetSideSlope());

        public override Vector3 GetStartPoint() => GetTransform().position - GetRideDirection().normalized * meshGenerator.CalculateRadiusLength();

        public override float GetLength() => meshGenerator.CalculateRadiusLength() + meshGenerator.Thickness + GetHeight() * GetSideSlope();

        /// <summary>
        /// Returns the angle of the transition's end in radians.
        /// </summary>
        public float GetEndAngle() => meshGenerator.GetEndAngle();

        public float GetRadius() => meshGenerator.Radius;

        public float UpdateEntrySpeed()
        {
            EntrySpeed = PhysicsManager.GetSpeedAtPosition(previousLineElement, GetStartPoint());
            Debug.Log($"Updated entry speed: {EntrySpeed}");
            return EntrySpeed;
        }

        public float GetExitSpeed() => PhysicsManager.GetExitSpeed(this);

        public Vector3 GetTransitionEnd()
        {
            Vector3 pos = transform.position;
            pos.y += GetHeight();
            return pos;
        }

        /// <summary>
        /// Returns the direction at which the rider will leave the takeoff. Basically a ride direction adjusted by the takeoff end angle.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetTakeoffDirection()
        {
            // Get the ride direction (which is along the XZ plane)
            Vector3 rideDir = GetRideDirection().normalized;

            // Get the end angle of the takeoff in radians
            float endAngle = GetEndAngle();

            // Create a rotation around the axis perpendicular to ride direction and up vector
            Vector3 rotationAxis = -Vector3.Cross(Vector3.up, rideDir).normalized;

            // Rotate the ride direction by the end angle
            // The rotation is around the local X axis of the takeoff (perpendicular to ride direction)
            Quaternion rotation = Quaternion.AngleAxis(endAngle * Mathf.Rad2Deg, rotationAxis);
            Vector3 takeoffDirection = rotation * rideDir;

            return takeoffDirection.normalized;
        }

        private void OnDrawGizmosSelected()
        {
            if (meshGenerator != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(GetTransitionEnd(), 0.5f);
                Gizmos.DrawLine(GetTransitionEnd(), GetTransitionEnd() + GetTakeoffDirection() * 3);
            }
        }
    }
}