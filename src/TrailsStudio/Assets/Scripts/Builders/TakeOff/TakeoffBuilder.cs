using Assets.Scripts.Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEditor.Rendering.FilterWindow;

namespace Assets.Scripts.Builders
{
    [RequireComponent(typeof(TakeoffMeshGenerator), typeof(TakeoffPositioner))]
    public class TakeoffBuilder : TakeoffBase, IObstacleBuilder
    {
        // TODO check for trajectory collision with terrain and other obstacles
        private Trajectory trajectory;

        [SerializeField]
        GameObject trajectoryRendererPrefab;

        LineRenderer trajectoryRenderer;

        TakeoffPositioner highlighter;

        public override void Initialize()
        {
            base.Initialize();

            meshGenerator.SetCanBuildMaterial();

            highlighter = GetComponent<TakeoffPositioner>();

            RecalculateCameraTargetPosition();

            GameObject trajectoryRendererInstance = Instantiate(trajectoryRendererPrefab, transform);
            trajectoryRendererInstance.transform.localPosition = Vector3.zero;

            trajectoryRenderer = trajectoryRendererInstance.GetComponent<LineRenderer>();
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
        /// Updates the entry speed of the takeoff builder and the resulting trajectory.
        /// </summary>
        /// <returns>The new entry speed in m/s</returns>
        public float UpdateEntrySpeed()
        {
            EntrySpeed = PhysicsManager.GetSpeedAtPosition(previousLineElement, GetStartPoint());
            //UpdateTrajectory();
            highlighter.UpdateMeasureText();
            return EntrySpeed;
        }

        public void UpdateTrajectory()
        {
            trajectory = PhysicsManager.GetFlightTrajectory(this);
            trajectoryRenderer.positionCount = trajectory.Count;
            for (int i = 0; i < trajectory.Count; i++)
            {
                trajectoryRenderer.SetPosition(i, trajectory[i].position);
            }
        }

        public void SetHeight(float height)
        {
            meshGenerator.Height = height;
            UpdateEntrySpeed();
            highlighter.UpdateLineRenderer();
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
            UpdateEntrySpeed();
            highlighter.UpdateLineRenderer();
        }        

        /// <summary>
        /// Sets the position of the takeoff builder and returns its new entry speed.
        /// </summary>
        /// <returns>Updated entry speed in m/s</returns>
        public float SetPosition(Vector3 position)
        {
            meshGenerator.transform.position = position;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this);
            }

            float speed = UpdateEntrySpeed();
            highlighter.UpdateLineRenderer();

            return speed;
        }

        public void SetRotation(Quaternion rotation)
        {
            meshGenerator.transform.rotation = rotation;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this);
            }

            UpdateEntrySpeed();
            highlighter.UpdateLineRenderer();
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            transform.forward = rideDirection;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this);
            }

            UpdateEntrySpeed();
            highlighter.UpdateLineRenderer();
        }        


        /// <returns>The distance between the Start point and the takeoff edge.</returns>
        public float GetCurrentRadiusLength()
        {
            return (transform.position - GetStartPoint()).magnitude;
        }


        public Takeoff Build()
        {
            trajectoryRenderer.enabled = false;

            enabled = false;

            Takeoff takeoff = GetComponent<Takeoff>();

            takeoff.Initialize(meshGenerator, cameraTarget, previousLineElement);

            takeoff.enabled = true;

            BuildManager.Instance.activeBuilder = null;

            // mark the path from previous line element to this takeoff as occupied
            takeoff.AddSlopeHeightmapCoords(new HeightmapCoordinates(previousLineElement.GetEndPoint(), GetStartPoint(), Mathf.Max(previousLineElement.GetBottomWidth(), GetBottomWidth())));
            takeoff.GetObstacleHeightmapCoordinates().MarkAs(CoordinateState.Occupied);            

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