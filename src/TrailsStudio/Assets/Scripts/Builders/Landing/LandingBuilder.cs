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

            BuildManager.Instance.activeBuilder = this;
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

        /// <param name="slope">Slope in degrees.</param>
        public void SetSlope(float slope)
        {
            meshGenerator.Slope = slope * Mathf.Deg2Rad;            
        }

        /// <summary>
        /// Sets new position of the landing and returns the new exit speed.
        /// </summary>
        /// <returns>Exit speed in m/s</returns>
        public float SetPosition(Vector3 position)
        {
            meshGenerator.transform.position = position;
            TerrainManager.SitFlushOnTerrain(this, GetEndPoint);
            RecalculateCameraTargetPosition();

            // TODO calculate the new exit speed
            return 0;

        }

        /// <summary>
        /// Rotates the landing around the y-axis. Negative values rotate to  riders left, positive to riders right.
        /// </summary>
        /// <param name="angle">The Angle in degrees.</param>
        public void SetRotation(int angle)
        {
            float angleDiff = angle - GetRotation();
            meshGenerator.transform.Rotate(Vector3.up, angleDiff);
            TerrainManager.SitFlushOnTerrain(this, GetEndPoint);

        }
        public void SetRotation(Quaternion rotation)
        {
            meshGenerator.transform.rotation = rotation;
            TerrainManager.SitFlushOnTerrain(this, GetEndPoint);
            RecalculateCameraTargetPosition(); 
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            transform.forward = rideDirection;
            TerrainManager.SitFlushOnTerrain(this, GetEndPoint);
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

            landing.Initialize(meshGenerator, terrain, cameraTarget, previousLineElement);

            landing.enabled = true;

            takeoff.SetLanding(landing);

            BuildManager.Instance.activeBuilder = null;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.AddWaypoint(landing);
            }

            landing.GetHeightmapCoordinates().MarkAsOccupied();

            TerrainManager.SitFlushOnTerrain(this, GetEndPoint);                

            return landing;
        }

        public void Cancel()
        {
            BuildManager.Instance.activeBuilder = null;
            DestroyUnderlyingGameObject();
        }

    }
}