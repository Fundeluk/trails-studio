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
        public void CloneSetRadius(object sender, ParamChangeEventArgs<float> args) => InvisibleClone.SetRadius(args.NewValue);

        public void CloneSetHeight(object sender, ParamChangeEventArgs<float> args) => InvisibleClone.SetHeight(args.NewValue);

        public void CloneSetThickness(object sender, ParamChangeEventArgs<float> args) => InvisibleClone.SetThickness(args.NewValue);

        public void CloneSetWidth(object sender, ParamChangeEventArgs<float> args) => InvisibleClone.SetHeight(args.NewValue);

        public void CloneSetEntrySpeed(object sender, ParamChangeEventArgs<float> args) => InvisibleClone.UpdateEntrySpeed();

        public TakeoffBuilder InvisibleClone { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            meshGenerator.SetCanBuildMaterial();

            RecalculateCameraTargetPosition();

            UpdateEntrySpeed();

            UpdateTrajectory();

            InitTrajectoryRenderer();

            CreateInvisibleClone();
        }

        public override void Initialize(TakeoffMeshGenerator meshGenerator, GameObject cameraTarget, ILineElement previousLineElement)
        {
            base.Initialize(meshGenerator, cameraTarget, previousLineElement);

            CreateInvisibleClone();
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

        private void CreateInvisibleClone()
        {
            // Create a new empty GameObject instead of using the prefab
            GameObject clone = new("TakeoffBuilder_PositioningClone");

            // Add only the essential components
            TakeoffMeshGenerator meshGen = clone.AddComponent<TakeoffMeshGenerator>();
            TakeoffBuilder cloneBuilder = clone.AddComponent<TakeoffBuilder>();

            // Set position and rotation
            clone.transform.SetPositionAndRotation(transform.position, transform.rotation);

            cloneBuilder.InitCameraTarget();

            // Copy mesh generator properties from original
            meshGen.Height = meshGenerator.Height;
            meshGen.Width = meshGenerator.Width;
            meshGen.Thickness = meshGenerator.Thickness;
            meshGen.Radius = meshGenerator.Radius;

            // Set up the clone builder properties
            cloneBuilder.meshGenerator = meshGen;
            cloneBuilder.MatchingTrajectory = MatchingTrajectory;
            cloneBuilder.EntrySpeed = EntrySpeed;
            cloneBuilder.previousLineElement = previousLineElement;

            InvisibleClone = cloneBuilder;
            EntrySpeedChanged += CloneSetEntrySpeed;
            RadiusChanged += CloneSetRadius;
            HeightChanged += CloneSetHeight;
            ThicknessChanged += CloneSetThickness;
            WidthChanged += CloneSetWidth;
        }

        /// <summary>
        /// Returns the XZ distance between the takeoff edge and the straight jump trajectory landing point in meters.
        /// </summary>
        public float GetFlightDistanceXZ()
        {
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
            MatchingTrajectory = PhysicsManager.GetFlightTrajectory(this);

            HighestReachablePoint = MatchingTrajectory.Apex.Value;

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
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this);
            }

            UpdateEntrySpeed();

            OnPositionChanged(transform.position);
        }

        public void SetRotation(Quaternion rotation)
        {
            meshGenerator.transform.rotation = rotation;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this);
            }

            UpdateEntrySpeed();

            OnRotationChanged(transform.rotation);
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            transform.forward = rideDirection;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this);
            }

            UpdateEntrySpeed();

            OnRotationChanged(transform.rotation);
        }        


        /// <returns>The distance between the Start point and the takeoff edge.</returns>
        public float GetCurrentRadiusLength()
        {
            return (transform.position - GetStartPoint()).magnitude;
        }

        private void OnDisable()
        {
            EntrySpeedChanged -= CloneSetEntrySpeed;
            RadiusChanged -= CloneSetRadius;
            HeightChanged -= CloneSetHeight;
            ThicknessChanged -= CloneSetThickness;
            WidthChanged -= CloneSetWidth;

            if (InvisibleClone != null)
            {
                Destroy(InvisibleClone.gameObject);
            }            
        }

        public Takeoff Build()
        {
            trajectoryRenderer.enabled = false;

            enabled = false;

            Takeoff takeoff = GetComponent<Takeoff>();

            takeoff.Initialize(meshGenerator, cameraTarget, previousLineElement, EntrySpeed, HighestReachablePoint);

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