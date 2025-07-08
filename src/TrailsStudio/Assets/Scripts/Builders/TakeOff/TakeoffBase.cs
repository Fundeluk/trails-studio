using Assets.Scripts.Managers;
using System;
using System.Collections;
using System.Diagnostics.Contracts;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders
{
    public abstract class TakeoffBase : ObstacleBase<TakeoffMeshGenerator>
    {       

        public event EventHandler<ParamChangeEventArgs<float>> RadiusChanged;
        protected void OnRadiusChanged(float newRadius)
        {
            RadiusChanged?.Invoke(this, new ParamChangeEventArgs<float>("Radius", newRadius));
        }

        public event EventHandler<ParamChangeEventArgs<float>> EntrySpeedChanged;
        protected void OnEntrySpeedChanged(float newEntrySpeed)
        {
            EntrySpeedChanged?.Invoke(this, new ParamChangeEventArgs<float>("EntrySpeed", newEntrySpeed));
        }

        public event EventHandler<ParamChangeEventArgs<float>> EndAngleChanged;

        protected void OnEndAngleChanged(float newEndAngle)
        {
            EndAngleChanged?.Invoke(this, new ParamChangeEventArgs<float>("EndAngle", newEndAngle));
        }


        [SerializeField]
        protected GameObject trajectoryRendererPrefab;

        protected LineRenderer trajectoryRenderer;

        /// <summary>
        /// A flight trajectory that either matches the landing OR, if no landing is yet built, the trajectory from jumping straight from the takeoff.
        /// </summary>
        public Trajectory MatchingTrajectory { get; protected set; } = null;   
        
        public void SetMatchingTrajectory(Trajectory trajectory)
        {
            MatchingTrajectory = trajectory;
            DrawTrajectory();
        }

        protected void InitTrajectoryRenderer()
        {
            GameObject trajectoryRendererInstance = Instantiate(trajectoryRendererPrefab, transform);
            trajectoryRendererInstance.transform.localPosition = Vector3.zero;

            trajectoryRenderer = trajectoryRendererInstance.GetComponent<LineRenderer>();
        }

        protected void DrawTrajectory()
        {
            if (trajectoryRenderer == null || MatchingTrajectory == null)
            {
                return;
            }
            trajectoryRenderer.positionCount = MatchingTrajectory.Count;

            int i = 0;
            foreach (var point in MatchingTrajectory)
            {
                trajectoryRenderer.SetPosition(i++, point.position);
            }

            trajectoryRenderer.enabled = true;
        }

        /// <summary>
        /// The speed at which a rider enters the transition in meters per second.
        /// </summary>
        public float EntrySpeed { get; protected set; } = 0f;
        
        /// <summary>
        /// Calculates the maximum angle at which the rider can exit the transition to the side with 0 representing no carve.
        /// </summary>
        /// <returns>The angle in radians.</returns>
        public float GetMaxCarveAngle()
        {
            float transitionLength = meshGenerator.CalculateTransitionLength();
            float width = GetWidth();
            float carveAngle = Mathf.Atan2(2 * width * transitionLength, Mathf.Abs(Mathf.Pow(transitionLength,2 ) - Mathf.Pow(width, 2)));
            Mathf.Clamp(carveAngle, -TakeoffConstants.MAX_CARVE_ANGLE_DEG * Mathf.Deg2Rad, TakeoffConstants.MAX_CARVE_ANGLE_DEG * Mathf.Deg2Rad);
            return carveAngle;
        }

        public override Vector3 GetEndPoint() => GetTransform().position + GetRideDirection().normalized * (meshGenerator.Thickness + GetHeight() * GetSideSlope());

        public override Vector3 GetStartPoint() => GetTransform().position - GetRideDirection().normalized * meshGenerator.CalculateTransitionLengthXZ();

        public override float GetLength() => meshGenerator.CalculateTransitionLengthXZ() + meshGenerator.Thickness + GetHeight() * GetSideSlope();

        /// <summary>
        /// Returns the angle of the transition's end in radians.
        /// </summary>
        public float GetEndAngle() => meshGenerator.GetEndAngle();

        public float GetRadius() => meshGenerator.Radius;        

        public float GetExitSpeed() => PhysicsManager.GetExitSpeed(this);

        public Vector3 GetTransitionEnd() => GetTransform().position + GetHeight() * GetTransform().up;


        /// <param name="angle">The normalized angle at which the rider will leave the takeoff. If 0, the default takeoff direction (straight up) is returned.
        /// -/+<see cref="GetMaxCarveAngle"/> vector direction is returned for -1/1.</param>
        ///<returns>The direction at which the rider will leave the takeoff. Basically a ride direction adjusted by the takeoff end angle.</returns>
        public Vector3 GetTakeoffDirection(float angle = 0)
        {            
            // Get the ride direction (which is along the XZ plane)
            Vector3 rideDir = GetRideDirection().normalized;

            // Get the end angle of the takeoff in radians
            float endAngle = GetEndAngle();

            // Create a rotation around the axis perpendicular to ride direction and up vector
            Vector3 rotationAxis = Vector3.Cross(rideDir, Vector3.up).normalized;

            // Rotate the ride direction by the end angle
            // The rotation is around the local X axis of the takeoff (perpendicular to ride direction)
            Quaternion rotation = Quaternion.AngleAxis(endAngle * Mathf.Rad2Deg, rotationAxis);
            Vector3 takeoffDirection = rotation * rideDir;

            if (angle == 0)                
            {
                return takeoffDirection.normalized;
            }

            Mathf.Clamp(angle, -1f, 1f); // Ensure angle is between -1 and 1 for carving            

            // If a specific angle is provided, rotate the takeoff direction by that angle
            Vector3 rideDirNormal = Vector3.Cross(rideDir, Vector3.up).normalized;
            rotationAxis = -Vector3.Cross(takeoffDirection, rideDirNormal).normalized;
            rotation = Quaternion.AngleAxis(angle * GetMaxCarveAngle() * Mathf.Rad2Deg, rotationAxis);
            takeoffDirection = rotation * takeoffDirection;

            return takeoffDirection.normalized;
        }

        private void OnDrawGizmosSelected()
        {
           
            Vector3 rideDirNormal = -Vector3.Cross(GetRideDirection(), Vector3.up).normalized;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(GetTransitionEnd(), 0.3f);
            Gizmos.DrawLine(GetTransitionEnd(), GetTransitionEnd() + GetTakeoffDirection());
            Gizmos.DrawLine(GetStartPoint(), GetStartPoint() + TerrainManager.GetNormalForWorldPosition(GetStartPoint()));

            Gizmos.color = Color.green;
            
            Vector3 leftCorner = GetTransitionEnd() - rideDirNormal * GetWidth() / 2;
            Vector3 rightCorner = GetTransitionEnd() + rideDirNormal * GetWidth() / 2;
            Gizmos.DrawLine(leftCorner, leftCorner + GetTakeoffDirection(-1));
            Gizmos.DrawLine(rightCorner, rightCorner + GetTakeoffDirection(1));
            
        }
    }
}