using Assets.Scripts.Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEditor.Rendering.FilterWindow;

namespace Assets.Scripts.Builders
{
    [RequireComponent(typeof(TakeoffMeshGenerator), typeof(TakeoffPositionHighlighter))]
    public class TakeoffBuilder : TakeoffBase, IObstacleBuilder
    {
        private List<(Vector3 position, Vector3 velocity)> trajectory;

        [SerializeField]
        GameObject trajectoryRendererPrefab;

        LineRenderer trajectoryRenderer;

        TakeoffPositionHighlighter highlighter;

        public override void Initialize()
        {
            base.Initialize();
            GetComponent<MeshRenderer>().material = material;

            highlighter = GetComponent<TakeoffPositionHighlighter>();

            RecalculateCameraTargetPosition();

            GameObject trajectoryRendererInstance = Instantiate(trajectoryRendererPrefab, transform);

            trajectoryRenderer = trajectoryRendererInstance.GetComponent<LineRenderer>();
        }        

        /// <summary>
        /// Updates the entry speed of the takeoff builder and the resulting trajectory.
        /// </summary>
        /// <returns>The new entry speed in m/s</returns>
        public float UpdateEntrySpeed()
        {
            EntrySpeed = PhysicsManager.GetSpeedAtPosition(previousLineElement, GetStartPoint());
            UpdateTrajectory();
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


        /// <returns>The distance from previous <see cref="ILineElement"/>s endpoint to this takeoffs startpoint in meters.</returns>
        public float GetDistanceFromPreviousLineElement()
        {
            return Vector3.Distance(previousLineElement.GetEndPoint(), GetStartPoint());
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
        /// <returns>Entry speed in m/s</returns>
        public float SetPosition(Vector3 position)
        {
            meshGenerator.transform.position = position;
            TerrainManager.SitFlushOnTerrain(this, GetStartPoint);
            RecalculateCameraTargetPosition();  
            float speed = UpdateEntrySpeed();
            highlighter.UpdateLineRenderer();

            return speed;
        }

        public void SetRotation(Quaternion rotation)
        {
            meshGenerator.transform.rotation = rotation;
            TerrainManager.SitFlushOnTerrain(this, GetStartPoint);
            RecalculateCameraTargetPosition();
            UpdateEntrySpeed();
            highlighter.UpdateLineRenderer();
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            transform.forward = rideDirection;
            TerrainManager.SitFlushOnTerrain(this, GetStartPoint);
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

        public void ResetAfterRevert()
        {
            GetComponent<MeshRenderer>().material = material;
            trajectoryRenderer.enabled = true;
        }

        public void Cancel()
        {
            BuildManager.Instance.activeBuilder = null;
            DestroyUnderlyingGameObject();
        }        
    }
}