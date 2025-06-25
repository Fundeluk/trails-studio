using Assets.Scripts.Managers;
using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using System.Collections.Generic;
using System.Linq;


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

            meshGenerator.SetCanBuildMaterial();

            PairedTakeoff = Line.Instance.GetLastLineElement() as Takeoff;      

            SetHeight(PairedTakeoff.GetHeight());

            RecalculateCameraTargetPosition();
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

        public void UpdateExitSpeed()
        {
            if (MatchingTrajectory == null)
            {
                return;
            }

            ExitSpeed = PhysicsManager.GetExitSpeed(this, MatchingTrajectory.Last());

            OnExitSpeedChanged(ExitSpeed);
        }
        
        public void SetMatchingTrajectory(Trajectory trajectory)
        {
            MatchingTrajectory = trajectory;
            UpdateExitSpeed();
        }

        public void SetHeight(float height)
        {
            meshGenerator.Height = height;
            RecalculateCameraTargetPosition();
            UpdateExitSpeed();
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

        /// <param name="slope">Slope in radians.</param>
        public void SetSlope(float slope)
        {
            meshGenerator.Slope = slope;
            UpdateExitSpeed();
            OnSlopeChanged(GetSlopeAngle());
        }

        /// <summary>
        /// Sets new position of the landing and returns the new exit speed.
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            meshGenerator.transform.position = position;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this);
            }

            RecalculateCameraTargetPosition();

            UpdateExitSpeed();
            OnPositionChanged(transform.position);
        }
        

        /// <summary>
        /// Rotates the landing around the y-axis to angle. Negative values rotate to riders left, positive to riders right.
        /// </summary>
        /// <param name="angle">The Angle in degrees.</param>
        public void SetRotation(float angle)
        {
            float angleDiff = angle - GetRotation();
            meshGenerator.transform.Rotate(Vector3.up, angleDiff);
            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this);
            }
            RecalculateCameraTargetPosition();

            UpdateExitSpeed();

            OnRotationChanged(transform.rotation);
        }

        public void SetRotation(Quaternion rotation)
        {
            meshGenerator.transform.rotation = rotation;
            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this);
            }
            RecalculateCameraTargetPosition(); 

            UpdateExitSpeed();

            OnRotationChanged(transform.rotation);
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            transform.forward = rideDirection;
            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this);
            }
            RecalculateCameraTargetPosition();

            UpdateExitSpeed();

            OnRotationChanged(transform.rotation);
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

            landing.Initialize(meshGenerator, cameraTarget, previousLineElement, ExitSpeed);

            landing.enabled = true;

            PairedTakeoff.SetLanding(landing);

            BuildManager.Instance.activeBuilder = null;

            landing.GetObstacleHeightmapCoordinates().MarkAs(new OccupiedCoordinateState(landing));

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