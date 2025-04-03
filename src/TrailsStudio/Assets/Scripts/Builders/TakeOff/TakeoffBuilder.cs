using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEditor.Rendering.FilterWindow;

namespace Assets.Scripts.Builders
{
    [RequireComponent(typeof(TakeoffMeshGenerator))]
    public class TakeoffBuilder : TakeoffBase, IObstacleBuilder
    {
        // TODO sort out naming, this is more like an initialization method, whereas in base, its more like a copy constructor
        public override void Initialize()
        {
            base.Initialize();
            previousLineElement = Line.Instance.GetLastLineElement();
            GetComponent<MeshRenderer>().material = material;

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
            RecalculateHeightmapBounds();
        }

        public void SetThickness(float thickness)
        {
            meshGenerator.Thickness = thickness;            
            RecalculateCameraTargetPosition();
            RecalculateHeightmapBounds();
        }

        public void SetRadius(float radius)
        {
            meshGenerator.Radius = radius;            
            RecalculateHeightmapBounds();
        }

        public void SetPosition(Vector3 position)
        {
            meshGenerator.transform.position = position;
            RecalculateCameraTargetPosition();
            RecalculateHeightmapBounds();
        }

        public void SetRotation(Quaternion rotation)
        {
            meshGenerator.transform.rotation = rotation;
            RecalculateCameraTargetPosition();
            RecalculateHeightmapBounds();
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            transform.forward = rideDirection;
            RecalculateHeightmapBounds();
        }


        /// <returns>The distance between the start point and the takeoff edge.</returns>
        public float GetCurrentRadiusLength()
        {
            return (transform.position - GetStartPoint()).magnitude;
        }


        public Takeoff Build()
        {
            enabled = false;

            Takeoff takeoff = GetComponent<Takeoff>();

            takeoff.Initialize(meshGenerator, terrain, cameraTarget, previousLineElement, bounds);

            takeoff.enabled = true;

            BuildManager.Instance.activeBuilder = null;

            takeoff.SetPath(TerrainManager.Instance.MarkPathAsOccupied(previousLineElement, takeoff));

            if (BuildManager.Instance.activeSlopeChange != null)
            {                
                BuildManager.Instance.activeSlopeChange.AddWaypoint(takeoff);

                TerrainManager.SitOnSlope(this, terrain);        
            }
            else
            {
                TerrainManager.Instance.MarkTerrainAsOccupied(takeoff.GetHeightmapBounds());
            }

            return takeoff;
        }

        public void Cancel()
        {
            BuildManager.Instance.activeBuilder = null;
            DestroyUnderlyingGameObject();
        }        
    }
}