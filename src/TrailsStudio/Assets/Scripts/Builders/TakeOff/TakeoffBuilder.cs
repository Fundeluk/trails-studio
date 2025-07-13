using Assets.Scripts.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEditor.Rendering.FilterWindow;

namespace Assets.Scripts.Builders
{
    [RequireComponent(typeof(TakeoffMeshGenerator))]
    public class TakeoffBuilder : TakeoffBase, IObstacleBuilder
    {        
        public override void Initialize()
        {
            base.Initialize();

            meshGenerator.SetCanBuildMaterial();

            RecalculateCameraTargetPosition();

            UpdateEntrySpeed();

            UpdateTrajectory();

            InitTrajectoryRenderer();
        }

        public override void Initialize(TakeoffMeshGenerator meshGenerator, GameObject cameraTarget, ILineElement previousLineElement)
        {
            base.Initialize(meshGenerator, cameraTarget, previousLineElement);
        }

        public void CanBuild(bool canBuild)
        {
            if (canBuild)
            {
                meshGenerator.SetCanBuildMaterial();
            }
            else
            {
                meshGenerator.SetCannotBuildMaterial();
            }
        }
        
        /// <summary>
        /// Returns the XZ distance between the takeoff edge and the straight jump trajectory landing point in meters.
        /// </summary>
        public float GetFlightDistanceXZ()
        {
            if (MatchingTrajectory.Count == 0)
            {
                return 0;
            }

            Vector3 edgePoint = GetTransitionEnd();
            edgePoint.y = 0f; // ignore height for distance calculation

            Vector3 landingPoint = MatchingTrajectory.Last().position;
            landingPoint.y = 0f; // ignore height for distance calculation

            return Vector3.Distance(edgePoint, landingPoint);
        }

        /// <summary>
        /// Updates the entry speed of the takeoff builder and the resulting trajectory.
        /// </summary>
        /// <returns>The new entry speed in m/s</returns>
        public float UpdateEntrySpeed()
        {
            // should not happen, validated in TakeoffPositioner
            if (!PhysicsManager.TryGetSpeedAtPosition(previousLineElement, GetStartPoint(), out float entrySpeed))
            {
                throw new InsufficientSpeedException("Cannot update entry speed: Cannot reach the start point due to insufficient speed.");
            }

            EntrySpeed = entrySpeed;

            UpdateTrajectory();
            OnEntrySpeedChanged(EntrySpeed);
            return EntrySpeed;
        }        

        public void UpdateTrajectory()
        {
            if (GetExitSpeed() == 0)
            {
                // if the exit speed is 0, we cannot calculate a trajectory
                MatchingTrajectory = null;

                if (trajectoryRenderer != null)
                {                    
                    trajectoryRenderer.enabled = false;
                }

                return;
            }            

            MatchingTrajectory = PhysicsManager.GetFlightTrajectory(this);

            DrawTrajectory();
        }

        public void SetHeight(float height)
        {
            meshGenerator.Height = height;
            UpdateEntrySpeed();
            RecalculateCameraTargetPosition();

            OnEndAngleChanged(GetEndAngle());
            OnHeightChanged(GetHeight());
        }

        public void SetWidth(float width)
        {
            meshGenerator.Width = width;

            OnWidthChanged(GetWidth());
        }

        public void SetThickness(float thickness)
        {
            meshGenerator.Thickness = thickness;            
            RecalculateCameraTargetPosition(); 

            OnThicknessChanged(GetThickness());
        }        

        public void SetRadius(float radius)
        {
            meshGenerator.Radius = radius;
            UpdateEntrySpeed();

            OnEndAngleChanged(GetEndAngle());
            OnRadiusChanged(GetRadius());
        }        

        /// <summary>
        /// Sets the position of the takeoff builder and returns its new entry speed.
        /// </summary>
        /// <returns>Updated entry speed in m/s</returns>
        public void SetPosition(Vector3 position)
        {
            meshGenerator.transform.position = position;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceTakeoff(this);
            }

            UpdateEntrySpeed();

            OnPositionChanged(transform.position);
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            transform.forward = rideDirection;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceTakeoff(this);
            }

            UpdateEntrySpeed();

            OnRotationChanged(transform.rotation);
        }        


        /// <returns>The distance between the Start point and the takeoff edge.</returns>
        public float GetTransitionLengthXZ()
        {
            Vector3 pos = transform.position;
            pos.y = 0;

            Vector3 start = GetStartPoint();
            start.y = 0;

            return Vector3.Distance(pos, start);
        }
        
        public Takeoff Build()
        {
            trajectoryRenderer.enabled = false;

            enabled = false;

            Takeoff takeoff = GetComponent<Takeoff>();

            takeoff.Initialize(meshGenerator, cameraTarget, previousLineElement, EntrySpeed);

            takeoff.enabled = true;

            BuildManager.Instance.activeBuilder = null;

            // mark the path from previous line element to this takeoff as occupied
            takeoff.AddSlopeHeightmapCoords(new HeightmapCoordinates(previousLineElement.GetEndPoint(), GetStartPoint(), Mathf.Max(previousLineElement.GetBottomWidth(), GetBottomWidth())));
            takeoff.GetObstacleHeightmapCoordinates().MarkAs(new OccupiedCoordinateState(takeoff));            

            if (TerrainManager.Instance.ActiveSlope != null)
            {                   
                TerrainManager.Instance.ActiveSlope.ConfirmChanges(takeoff);
            }            
            
            return takeoff;
        }

        public void ResetAfterRevert()
        {
            meshGenerator.SetCanBuildMaterial();
            trajectoryRenderer.enabled = true;
        }

        public void Cancel()
        {
            BuildManager.Instance.activeBuilder = null;
            DestroyUnderlyingGameObject();
        }        
    }
}