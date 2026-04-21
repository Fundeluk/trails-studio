using System.Linq;
using Managers;
using PhysicsManager;
using TerrainEditing;
using TerrainEditing.Slope;
using UnityEngine;

namespace Obstacles.TakeOff
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
        private void UpdateEntrySpeed()
        {
            // should not happen, validated in TakeoffPositioner
            if (!PhysicsManager.PhysicsManager.TryGetSpeedAtPosition(PreviousLineElement, GetStartPoint(), out float entrySpeed))
            {
                throw new InsufficientSpeedException("Cannot update entry speed: Cannot reach the start point due to insufficient speed.");
            }

            EntrySpeed = entrySpeed;

            UpdateTrajectory();
            OnEntrySpeedChanged(EntrySpeed);
        }

        private void UpdateTrajectory()
        {
            if (GetExitSpeed() == 0)
            {
                // if the exit speed is 0, we cannot calculate a trajectory
                MatchingTrajectory = null;

                if (TrajectoryRenderer != null)
                {                    
                    TrajectoryRenderer.enabled = false;
                }

                return;
            }            

            MatchingTrajectory = PhysicsManager.PhysicsManager.GetFlightTrajectory(this);

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
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this, position);
            }

            UpdateEntrySpeed();

            OnPositionChanged(transform.position);
        }
        
        public SlopeChange.PlacementResult PlaceOnSlope(SlopeChange slope, Vector3 rawPosition, bool isTilted = false)
        {
            slope.DiscardTentativePlacement();

            Vector3 waypointStartXZ = GetStartPoint();
            waypointStartXZ.y = 0; // ignore the takeoff's height for the XZ calculations
            Vector3 waypointEndXZ = GetEndPoint();
            waypointEndXZ.y = 0; // ignore the takeoff's height for the XZ calculations

            Vector3 slopeDir = slope.LastRideDirection;
            
            // check if entire takeoff is before the slope
            if (slope.IsBeforeStart(waypointStartXZ, slopeDir) && slope.IsBeforeStart(waypointEndXZ, slopeDir))
            {
                TerrainManager.Instance.FitObstacleOnFlat(this);
                return new SlopeChange.PlacementResult(slope.RemainingLength, slope.EndPoint, slopeDir, slope.Width,
                    false, new TerrainManager.HeightmapCoordinates());
            }

            float newRemainingLength = slope.RemainingLength;
            Vector3 newEndPoint;
            TerrainManager.HeightmapCoordinates coords = new();

            float newWidth = Mathf.Max(slope.Width, GetBottomWidth());

            float startHeight = slope.EndPoint.y;


            // obstacle is on the border of slope start
            if (slope.IsBeforeStart(waypointStartXZ, slopeDir) && slope.IsOnActivePartOfSlope(waypointEndXZ, slopeDir))
            {
                float heightDiff = slope.GetHeightDifferenceForXZDistance(Vector3.Distance(waypointStartXZ, waypointEndXZ));
                var rampCoords = TerrainManager.Instance.DrawRamp(waypointStartXZ, waypointEndXZ,
                    heightDiff, newWidth, startHeight);
                coords.Add(rampCoords);

                var flatCoords = TerrainManager.Instance.DrawFlat(waypointStartXZ, waypointEndXZ,
                    startHeight, newWidth);
                coords.Add(flatCoords);

                TerrainManager.Instance.FitObstacleOnFlat(this);
                float distanceTaken = Vector3.Distance(slope.EndPoint, GetEndPoint());
                newRemainingLength -= distanceTaken;

                newEndPoint = GetEndPoint();
                float newEndPointHeight = slope.EndPoint.y + slope.GetHeightDifferenceForXZDistance(distanceTaken);
                newEndPoint.y = newEndPointHeight;

            }
            // whole obstacle is on slope
            else if (slope.IsOnActivePartOfSlope(waypointStartXZ, slopeDir) && slope.IsOnActivePartOfSlope(waypointEndXZ, slopeDir))
            {
                float heightDiff = slope.GetHeightDifferenceForXZDistance(Vector3.Distance(slope.EndPoint, waypointEndXZ));

                var rampCoords = TerrainManager.Instance.DrawRamp(slope.EndPoint, waypointEndXZ, heightDiff,
                    newWidth, startHeight);
                coords.Add(rampCoords);
                slope.FitObstacleOnSlope(this);
                newRemainingLength -= slope.GetXZDistanceFromSlopeLength(Vector3.Distance(slope.EndPoint, GetEndPoint()));
                newEndPoint = GetEndPoint();
            }
            // obstacle is on border of slope end
            else if (slope.IsOnActivePartOfSlope(waypointStartXZ, slopeDir) && slope.IsAfterSlope(waypointEndXZ, slopeDir))
            {
                float heightDiff = slope.GetHeightDifferenceForXZDistance(Vector3.Distance(slope.EndPoint, waypointEndXZ));
                var rampCoords = TerrainManager.Instance.DrawRamp(slope.EndPoint, waypointEndXZ, heightDiff,
                    newWidth, startHeight);
                coords.Add(rampCoords);

                slope.FitObstacleOnSlope(this);
                newEndPoint = slope.GetFinishedEndPoint(slopeDir);

                newRemainingLength = 0;
            }
            // whole obstacle is after the slope
            else if (slope.IsAfterSlope(waypointStartXZ, slopeDir) && slope.IsAfterSlope(waypointEndXZ, slopeDir))
            {
                newEndPoint = slope.GetFinishedEndPoint(slopeDir);
                float heightDiff = slope.GetHeightDifferenceForXZDistance(Vector3.Distance(slope.EndPoint, newEndPoint));
                var rampCoords = TerrainManager.Instance.DrawRamp(slope.EndPoint, newEndPoint, heightDiff,
                    newWidth, startHeight);
                coords.Add(rampCoords);

                var flatCoords = TerrainManager.Instance.DrawFlat(newEndPoint, waypointEndXZ,
                    slope.EndHeight, newWidth);
                coords.Add(flatCoords);

                TerrainManager.Instance.FitObstacleOnFlat(this);
                newRemainingLength = 0;
            }
            // slope is so short that the obstacle starts before it but ends after it.
            else
            {
                newEndPoint = slope.GetFinishedEndPoint(slopeDir);
                float heightDiff = slope.GetHeightDifferenceForXZDistance(Vector3.Distance(waypointStartXZ, newEndPoint));
                var rampCoords = TerrainManager.Instance.DrawRamp(waypointStartXZ, newEndPoint, heightDiff,
                    newWidth, startHeight);
                coords.Add(rampCoords);

                slope.FitObstacleOnSlope(this);
                newRemainingLength = 0;
            }
        
            return new SlopeChange.PlacementResult(newRemainingLength, newEndPoint, slopeDir, newWidth, true, coords);
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            transform.forward = rideDirection;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.PlaceObstacle(this, GetTransform().position);
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
            TrajectoryRenderer.enabled = false;

            enabled = false;

            Takeoff takeoff = GetComponent<Takeoff>();

            takeoff.Initialize(meshGenerator, CameraTarget, PreviousLineElement, EntrySpeed);

            takeoff.enabled = true;

            BuildManager.Instance.ActiveBuilder = null;

            if (TerrainManager.Instance.ActiveSlope != null)
            {                   
                TerrainManager.Instance.ActiveSlope.ConfirmChanges(takeoff);
            }
            
            // mark the path from previous line element to this takeoff as occupied
            takeoff.GetRidePathHeightmapCoordinates().MarkAs(new HeightSetCoordinateState());
            takeoff.GetObstacleHeightmapCoordinates().MarkAs(new OccupiedCoordinateState(takeoff));
            
            return takeoff;
        }

        public void ResetAfterRevert()
        {
            meshGenerator.SetCanBuildMaterial();
            TrajectoryRenderer.enabled = true;
        }

        public void Cancel()
        {
            BuildManager.Instance.ActiveBuilder = null;
            DestroyUnderlyingGameObject();
        }        
    }
}