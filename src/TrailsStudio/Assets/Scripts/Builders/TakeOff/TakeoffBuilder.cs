﻿using Assets.Scripts.Managers;
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
         
        }

        public void SetThickness(float thickness)
        {
            meshGenerator.Thickness = thickness;            
            RecalculateCameraTargetPosition();         
        }

        public void SetRadius(float radius)
        {
            meshGenerator.Radius = radius;
         
        }

        public void SetPosition(Vector3 position)
        {
            meshGenerator.transform.position = position;
            TerrainManager.SitFlushOnTerrain(this, GetStartPoint);
            RecalculateCameraTargetPosition();         
        }

        public void SetRotation(Quaternion rotation)
        {
            meshGenerator.transform.rotation = rotation;
            TerrainManager.SitFlushOnTerrain(this, GetStartPoint);
            RecalculateCameraTargetPosition();         
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            transform.forward = rideDirection;
            TerrainManager.SitFlushOnTerrain(this, GetStartPoint);
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

            takeoff.Initialize(meshGenerator, terrain, cameraTarget, previousLineElement);

            takeoff.enabled = true;

            BuildManager.Instance.activeBuilder = null;

            if (TerrainManager.Instance.ActiveSlope != null)
            {                
                TerrainManager.Instance.ActiveSlope.AddWaypoint(takeoff);
            }
            else
            {
                // mark the path from previous line element to this takeoff as occupied
                takeoff.AddSlopeHeightmapCoords(new HeightmapCoordinates(previousLineElement.GetEndPoint(), GetStartPoint(), Mathf.Max(previousLineElement.GetBottomWidth(), GetBottomWidth())));
            }

            takeoff.GetHeightmapCoordinates().MarkAsOccupied();            
            
            TerrainManager.SitFlushOnTerrain(this, GetStartPoint);

            return takeoff;
        }

        public void Cancel()
        {
            BuildManager.Instance.activeBuilder = null;
            DestroyUnderlyingGameObject();
        }        
    }
}