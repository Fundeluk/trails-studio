using Assets.Scripts.Managers;
using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Unity.VisualScripting;


namespace Assets.Scripts.Builders
{
    public class LandingBuilder : LandingBase, IObstacleBuilder
    {
        public override void Initialize()
        {
            base.Initialize();

            if (Line.Instance.GetLastLineElement() is not Takeoff)
            {
                throw new Exception("Landing must be built after a takeoff.");
            }

            GetComponent<MeshRenderer>().material = material;

            this.takeoff = Line.Instance.GetLastLineElement() as Takeoff;      
            
            RecalculateCameraTargetPosition();
        }

        public bool IsValidForTakeoffTrajectory()
        {
            // TODO check if the landing is in the right position and has correct parameters for the takeoff trajectory
            return true;
        }

        public void SetHeight(float height)
        {
            meshGenerator.Height = height;
            RecalculateCameraTargetPosition();
        }

        public void SetWidth(float width)
        {
            meshGenerator.Width = width;            
        }

        public void SetThickness(float thickness)
        {
            meshGenerator.Thickness = thickness;
            RecalculateCameraTargetPosition();            
        }

        /// <param name="slope">Slope in radians.</param>
        public void SetSlope(float slope) => meshGenerator.Slope = slope;

        /// <summary>
        /// Sets new position of the landing and returns the new exit speed.
        /// </summary>
        /// <returns>Exit speed in m/s</returns>
        public float SetPosition(Vector3 position)
        {
            // TODO make the landing as high as possible for its takeoff trajectory
            meshGenerator.transform.position = position;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this);
            }

            RecalculateCameraTargetPosition();

            // TODO calculate the new exit speed
            return 0;

        }

        /// <summary>
        /// Rotates the landing around the y-axis to angle. Negative values rotate to riders left, positive to riders right.
        /// </summary>
        /// <param name="angle">The Angle in degrees.</param>
        public void SetRotation(float angle)
        {
            float angleDiff = angle - GetRotation();
            Debug.Log($"landings setrotation: rotation before: {GetRotation()}, target angle: {angle}, angleDiff: {angleDiff}");
            meshGenerator.transform.Rotate(Vector3.up, angleDiff);
            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this);
            }
            RecalculateCameraTargetPosition();
        }

        public void SetRotation(Quaternion rotation)
        {
            meshGenerator.transform.rotation = rotation;
            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this);
            }
            RecalculateCameraTargetPosition(); 
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            transform.forward = rideDirection;
            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this);
            }
            RecalculateCameraTargetPosition();
        }

        /// <returns>The distance between the Start point and the takeoff edge.</returns>
        public float GetCurrentSlopeLength()
        {
            return Vector3.Distance(GetEndPoint(), transform.position);
        }


        public Landing Build()
        {
            enabled = false;

            Landing landing = GetComponent<Landing>();

            landing.Initialize(meshGenerator, cameraTarget, previousLineElement);

            landing.enabled = true;

            takeoff.SetLanding(landing);

            BuildManager.Instance.activeBuilder = null;

            landing.GetObstacleHeightmapCoordinates().MarkAs(CoordinateState.Occupied);

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.ConfirmChanges(landing);
            }

            return landing;
        }

        public void Cancel()
        {
            BuildManager.Instance.activeBuilder = null;
            DestroyUnderlyingGameObject();
        }

    }
}