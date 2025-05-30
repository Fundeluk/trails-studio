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

        public override void Initialize(TakeoffMeshGenerator meshGenerator, GameObject cameraTarget, ILineElement previousLineElement)
        {
            base.Initialize(meshGenerator, cameraTarget, previousLineElement);
        }                

        public override Vector3 GetEndPoint() => GetTransform().position + GetRideDirection().normalized * (meshGenerator.Thickness + GetHeight() * GetSideSlope());

        public override Vector3 GetStartPoint() => GetTransform().position - GetRideDirection().normalized * meshGenerator.CalculateRadiusLength();

        public override float GetLength() => meshGenerator.CalculateRadiusLength() + meshGenerator.Thickness + GetHeight() * GetSideSlope();

        /// <summary>
        /// Returns the angle of the transition's end in radians.
        /// </summary>
        public float GetEndAngle() => meshGenerator.GetEndAngle();

        public float GetRadius() => meshGenerator.Radius;        

        public float GetExitSpeed() => PhysicsManager.GetExitSpeed(this);

        public Vector3 GetTransitionEnd() => GetTransform().position + GetHeight() * GetTransform().up;

        ///<returns>The direction at which the rider will leave the takeoff. Basically a ride direction adjusted by the takeoff end angle.</returns>
        public Vector3 GetTakeoffDirection()
        {
            // TODO adjust for slope angle

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

        public override ObstacleBounds GetBoundsForObstaclePosition(Vector3 position, Vector3 rideDir)
        {
            position.y = 0;
            rideDir = Vector3.ProjectOnPlane(rideDir, Vector3.up).normalized;
            Vector3 rightDir = -Vector3.Cross(rideDir, Vector3.up).normalized;
            Vector3 startPoint = position - (meshGenerator.CalculateRadiusLength()) * rideDir;
            Vector3 endPoint = startPoint + GetLength() * rideDir;
            Vector3 leftStartCorner = startPoint - (GetBottomWidth() / 2) * rightDir;
            Vector3 rightStartCorner = startPoint + (GetBottomWidth() / 2) * rightDir;
            Vector3 leftEndCorner = endPoint - (GetBottomWidth() / 2) * rightDir;
            Vector3 rightEndCorner = endPoint + (GetBottomWidth() / 2) * rightDir;
            return new ObstacleBounds(startPoint, leftStartCorner, rightStartCorner, endPoint, leftEndCorner, rightEndCorner);
        }

        private void OnDrawGizmosSelected()
        {
            if (meshGenerator != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(GetTransitionEnd(), 0.5f);
                Gizmos.DrawLine(GetTransitionEnd(), GetTransitionEnd() + GetTakeoffDirection() * 3);
                Gizmos.DrawLine(GetStartPoint(), GetStartPoint() + TerrainManager.GetNormalForWorldPosition(GetStartPoint()) * 5);
            }
        }
    }
}