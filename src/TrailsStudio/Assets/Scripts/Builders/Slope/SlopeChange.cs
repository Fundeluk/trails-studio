using UnityEngine;
using System.Collections;
using Assets.Scripts.Managers;
using Assets.Scripts.Utilities;
using System.Collections.Generic;
using Unity.Mathematics;
using System.IO.Pipes;
using Unity.VisualScripting;
using UnityEngine.Rendering.Universal;
using System.Linq.Expressions;
using UnityEngine.WSA;
using UnityEngine.UIElements;
using System.Net;
using UnityEditor.VersionControl;
using TMPro;
using System.Linq;
using System;

namespace Assets.Scripts.Builders
{
    public class SlopeChange : SlopeChangeBase
    {
        public record SlopeSnapshot
        {
            private readonly SlopeChange slope;
            public bool finished;
            public float remainingLength;
            public float width;
            public Vector3 endPoint;
            Vector3 lastRideDir;

            public SlopeSnapshot(SlopeChange slope)
            {
                this.slope = slope;
                finished = slope.finished;
                remainingLength = slope.remainingLength;
                width = slope.width;
                endPoint = slope.endPoint;
                lastRideDir = slope.LastRideDirection;
            }

            public void Revert()
            {
                if (!finished)
                {
                    TerrainManager.Instance.ActiveSlope = slope;
                }
                
                if (slope.lastPlacementResult.ChangedHeightmapCoords != null)
                {
                    slope.lastPlacementResult.ChangedHeightmapCoords.SetHeight(endPoint.y);
                }

                slope.remainingLength = remainingLength;
                slope.endPoint = endPoint;
                slope.width = width;
                slope.finished = finished;
                slope.LastRideDirection = lastRideDir;


                slope.UpdateHighlight();
            }
        }

        public record PlacementResult
        {
            public float Remaininglength { get; private set; } = 0f;
            public Vector3 NewEndPoint { get; private set; } = Vector3.zero;
            public bool IsWaypoint { get; private set; } = false;

            public HeightmapCoordinates ChangedHeightmapCoords { get; private set; } = null;

            public PlacementResult(float remainingLength, Vector3 newEndPoint, bool isWaypoint, HeightmapCoordinates changedHeightmapCoords)
            {
                Remaininglength = remainingLength;
                NewEndPoint = newEndPoint;
                IsWaypoint = isWaypoint;
                ChangedHeightmapCoords = changedHeightmapCoords;
            }     
            
            public PlacementResult(SlopeChange slopeChange)
            {
                Remaininglength = slopeChange.remainingLength;
                NewEndPoint = slopeChange.endPoint;                
            }
        }

        public class WaypointList : IEnumerable<(ILineElement, SlopeSnapshot)>
        {            

            private readonly SlopeChange owner;
            public List<(ILineElement element, SlopeSnapshot snapshot)> waypoints = new();

            public void AddWaypoint(ILineElement waypoint)
            {
                // only when the first waypoint is added, mark the flat to start point as occupied to avoid placement issues
                // with the first waypoint (the flat would be marked as occupied and the waypoint couldn't be placed there)
                if (waypoints.Count == 0)
                {
                    owner.flatToStartPoint.MarkAs(CoordinateState.HeightSet);
                }

                SlopeSnapshot snapshot = owner.LastConfirmedSnapshot;
                waypoints.Add((waypoint, snapshot));
                waypoint.SetSlopeChange(owner);
            }

            public (ILineElement element, SlopeSnapshot snapshot) this[int index] => waypoints[index];

            public bool TryFindByElement(ILineElement element, out SlopeSnapshot snapshot)
            {
                foreach (var waypoint in waypoints)
                {
                    if (waypoint.element == element)
                    {
                        snapshot = waypoint.snapshot;
                        return true;
                    }
                }
                snapshot = default;
                return false;
            }

            public bool RemoveWaypoint(ILineElement item)
            {
                if (TryFindByElement(item, out var snapshot))
                {
                    item.SetSlopeChange(null);
                    snapshot.Revert(); // revert the slope to the state before the waypoint was added
                    item.GetUnderlyingSlopeHeightmapCoordinates()?.MarkAs(CoordinateState.Free); // unmark the heightmap coordinates of the waypoint

                    TerrainManager.Instance.SetHeight(snapshot.endPoint.y); // set the height of the terrain to the height at the waypoint

                    owner.LastConfirmedSnapshot = snapshot;

                    return waypoints.Remove((item, snapshot));
                }
                else
                {
                    return false;
                }

            } 
            
            public void Clear()
            {
                foreach ((ILineElement element, SlopeSnapshot snapshot) in waypoints)
                {
                    element.SetSlopeChange(null);
                    element.GetUnderlyingSlopeHeightmapCoordinates()?.MarkAs(CoordinateState.Free);
                }
                waypoints.Clear();
            }

            public int Count => waypoints.Count;          

            
            public IEnumerator<(ILineElement, SlopeSnapshot)> GetEnumerator()
            {
                return waypoints.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)waypoints).GetEnumerator();
            }

            public WaypointList(SlopeChange owner)
            {
                this.owner = owner;
            }
        }

        [SerializeField]
        GameObject endPointHighlightPrefab;

        readonly List<GameObject> endPointHighlights = new();

        GameObject infoText;               

        /// <summary>
        /// The portion of the terrain that goes from the last line element before the slope's start to the slope's start.
        /// </summary>
        HeightmapCoordinates flatToStartPoint;

        private float remainingLength;       

        private WaypointList waypoints;

        /// <summary>
        /// The end point of the slope. If the slope is not finished, this is an end point of the realized portion of the slope.
        /// </summary>
        Vector3 endPoint;

        public Vector3 LastRideDirection { get; private set; }

        public SlopeSnapshot LastConfirmedSnapshot { get; private set; }

        protected override void UpdateHighlight()
        {
            if (finished || Length == 0 || remainingLength == 0)
            {
                highlight.enabled = false;
                return;
            }

            highlight.enabled = true;

            Vector3 rideDirNormal = Vector3.Cross(LastRideDirection, Vector3.up).normalized;

            Vector3 position = Vector3.Lerp(endPoint, endPoint + remainingLength * LastRideDirection, 0.5f);
            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, rideDirNormal);

            transform.SetPositionAndRotation(position, rotation);

            DecalProjector decalProjector = highlight.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(remainingLength, width, 20);
        }

        /// <summary>
        /// Initializes a slope with a given Start point, Length and end height.
        /// </summary>
        public void Initialize(Vector3 start, float endHeight, float length)
        {
            waypoints = new WaypointList(this);
            startHeight = start.y;
            this.endHeight = endHeight;

            Start = start;
            endPoint = start; 
            
            Length = length;
            remainingLength = length;

            UpdateAngle();

            previousLineElement = Line.Instance.GetLastLineElement();
            LastRideDirection = previousLineElement.GetRideDirection();
            width = previousLineElement.GetBottomWidth();

            flatToStartPoint = new HeightmapCoordinates(previousLineElement.GetEndPoint(), start, width);

            highlight = GetComponent<DecalProjector>();
            highlight.material.color = Color.green;

            TerrainManager.Instance.AddSlope(this);

            UpdateHighlight();

            UIManager.Instance.GetDeleteUI().DeleteSlopeButtonEnabled = true;

            lastPlacementResult = new(this);
            LastConfirmedSnapshot = GetSlopeSnapshot();
        }

        List<(string name, string value)> GetInfoText()
        {
            List<(string name, string value)> info = new()
            {
                ("Length", $"{Length:0.##}m"),
                ("Angle", $"{Angle * Mathf.Rad2Deg:0}°"),
                ("Height difference", $"{endHeight - startHeight:0.##}m")
            };
            return info;
        }

        public void ShowInfo()
        {
            // offset the info text to the side of the slope
            Vector3 infoTextPos = Start + Vector3.Cross(Camera.main.transform.forward, Vector3.up).normalized * 5f + Vector3.up * 4f;
            infoText = UIManager.Instance.ShowSlopeInfo(GetInfoText(), infoTextPos, transform, Start);

            endPointHighlights.Add(Instantiate(endPointHighlightPrefab, Start, Quaternion.identity));
            endPointHighlights[0].transform.parent = transform;
            endPointHighlights.Add(Instantiate(endPointHighlightPrefab, endPoint, Quaternion.identity));
            endPointHighlights[1].transform.parent = transform;            
        }

        public void HideInfo()
        {
            foreach (var highlight in  endPointHighlights)
            {
                Destroy(highlight);
            }
            endPointHighlights.Clear();

            if (infoText != null)
            {
                Destroy(infoText);
            }
        } 
        
        /// <summary>
        /// Returns whether a position is before the Start of this slope. 
        /// </summary>        
        /// <returns>True if the slope has no waypoints and the position is before the slope's start, false if otherwise.</returns>
        public bool IsBeforeStart(Vector3 position)
        {     
            if (waypoints.Count > 0)
            {
                // if waypoints are added, the slope can go in any direction, even before its start,
                // so we can't check if the position is before the slope's start
                return false;
            }

            Vector3 slopeStartToPosition = Vector3.ProjectOnPlane(position - Start, Vector3.up);
            float projection = Vector3.Dot(slopeStartToPosition, LastRideDirection);
            
            return projection < 0; // if the position is before the slope start, the projection is negative
        }        
        
               
        /// <returns>True if the slope is not yet finished 
        /// and the position is on the part from current endpoint to the potential finished endpoint of the slope.</returns>
        public bool IsOnActivePartOfSlope(Vector3 position)
        {
            if (finished)
            {
                return false;
            }

            Vector3 endPointToPosition = Vector3.ProjectOnPlane(position - endPoint, Vector3.up).normalized;
            float projection = Vector3.Dot(endPointToPosition, LastRideDirection);

            Vector3 endPointXZ = new(endPoint.x, 0, endPoint.z);
            Vector3 positionXZ = new(position.x, 0, position.z);            

            return Vector3.Distance(endPointXZ, positionXZ) <= remainingLength && projection > 0;
        }        

        public bool IsAfterSlope(Vector3 position)
        {
            Vector3 endPointToPosition = Vector3.ProjectOnPlane(position - endPoint, Vector3.up).normalized;
            float projection = Vector3.Dot(endPointToPosition, LastRideDirection);

            Vector3 endPointXZ = new(endPoint.x, 0, endPoint.z);
            Vector3 positionXZ = new(position.x, 0, position.z);

            return projection > 0 && Vector3.Distance(endPointXZ, positionXZ) > remainingLength;
        }

        /// <summary>
        /// Returns the end point of the slope if finished, otherwise where the slope will end in the current <see cref="LastRideDirection"/>
        /// </summary>
        public Vector3 GetFinishedEndPoint()
        {
            if (finished)
            {
                return endPoint;
            }
            else
            {
                Quaternion tiltToAngle = Quaternion.AngleAxis(Angle * Mathf.Rad2Deg, -Vector3.Cross(Vector3.up, LastRideDirection).normalized);
                Vector3 angledRideDir = (tiltToAngle * LastRideDirection).normalized;
                return endPoint + angledRideDir * remainingLength;
            }
        }

        /// <summary>
        /// Calculates the height difference for a given distance using the slope's Angle.
        /// </summary>
        /// <remarks>Is not bounded by the slope's <see cref="remainingLength"/></remarks>
        private float GetHeightDifferenceForDistance(float distance)
        {
            float heightDif = distance * Mathf.Tan(Angle);
            return heightDif;
        }


        /// <param name="XZDistance">The distance on the XZ plane.</param>
        /// <rerns>The slope Length that corresponds to the given XZ distance.</returns>
        public float GetSlopeLengthFromXZDistance(float XZDistance)
        {           
            return XZDistance / Mathf.Cos(Mathf.Abs(Angle));
        }
        
        /// <param name="slopeLength">The distance on the slope</param>
        /// <returns>The XZ plane distance that corresponds to the given slope distance</returns>
        public float GetXZDistanceFromSlopeLength(float slopeLength)
        {
            return slopeLength * Mathf.Cos(Mathf.Abs(Angle));
        }

        public Vector3 GetNormal(Vector3 rideDir)
        {
            Quaternion tiltToAngle = Quaternion.AngleAxis(Angle * Mathf.Rad2Deg, -Vector3.Cross(Vector3.up, rideDir).normalized);
            Vector3 normal = tiltToAngle * Vector3.up;
            return normal.normalized;
        }

        public Vector3 TiltVectorBySlopeAngle(Vector3 vector, Vector3 axisUpNormal)
        {
            Quaternion tiltToAngle = Quaternion.AngleAxis(Angle * Mathf.Rad2Deg, -Vector3.Cross(Vector3.up, axisUpNormal).normalized);
            return tiltToAngle * vector;
        }

        /// <summary>
        /// Rotates the obstacle so that its forward direction is aligned with the slope's angled ride direction
        /// and places it on the terrain at the correct height.
        /// </summary>
        private void FitObstacleOnSlope(IObstacleBuilder builder)
        {
            Vector3 rideDir = Vector3.ProjectOnPlane(builder.GetRideDirection(), Vector3.up).normalized;
            Vector3 angledRideDir = TiltVectorBySlopeAngle(rideDir, rideDir);         

            builder.GetTransform().forward = angledRideDir;
            float newHeight = TerrainManager.GetHeightAt(builder.GetTransform().position);
            builder.GetTransform().position = new(builder.GetTransform().position.x, newHeight, builder.GetTransform().position.z);
        }

        public LandingPositionTrajectoryInfo GetLandingTrajectoryInfo(LandingBase landing, Vector3 positionXZ)
        {
            // default values for when the position is not affected by the slope
            Vector3 position = new(positionXZ.x, TerrainManager.GetHeightAt(positionXZ), positionXZ.z);
            Vector3 edgePosition = position + landing.GetHeight() * Vector3.up;
            Vector3 landingDir = landing.GetLandingDirection();

            Vector3 rideDirXZ = Vector3.ProjectOnPlane(landing.GetRideDirection(), Vector3.up).normalized;
            Vector3 normal = GetNormal(rideDirXZ);

            float toStartMagnitude = landing.GetThickness() + landing.GetHeight() * landing.GetSideSlope();
            Vector3 landingStartPointXZ = positionXZ - rideDirXZ * toStartMagnitude;
            Vector3 landingEndPointXZ = positionXZ + rideDirXZ * landing.GetTransitionLengthXZ();
           
            // obstacle is on the border of slope start
            if (IsBeforeStart(landingStartPointXZ) && IsOnActivePartOfSlope(landingEndPointXZ))
            {

                if (IsOnActivePartOfSlope(positionXZ))
                {
                    position.y += GetHeightDifferenceForDistance(Vector3.Distance(endPoint, positionXZ));
                }
                else
                {
                    position.y -= GetHeightDifferenceForDistance(Vector3.Distance(endPoint, positionXZ));
                }

                edgePosition = position + landing.GetHeight() * normal;
                landingDir = TiltVectorBySlopeAngle(landingDir, rideDirXZ);
            }
            // whole obstacle is on slope
            else if (IsOnActivePartOfSlope(landingStartPointXZ) && IsOnActivePartOfSlope(landingEndPointXZ))
            {
                position.y -= GetHeightDifferenceForDistance(Vector3.Distance(endPoint, positionXZ));
                edgePosition = position + landing.GetHeight() * normal;
                landingDir = TiltVectorBySlopeAngle(landingDir, rideDirXZ);
            }
            // obstacle is on border of slope end  OR whole obstacle is after the slope         
            else
            {
                position.y = endHeight;
                edgePosition = position + landing.GetHeight() * Vector3.up;
            }           
            

            return new(position, new(edgePosition, landingDir.normalized));
        }


        private HeightmapCoordinates DrawFlat(Vector3 start, Vector3 end, float height)
        {
            start.y = 0;
            end.y = 0;

            float distanceToModify = Vector3.Distance(start, end);

            if (distanceToModify == 0)
            {
                return new HeightmapCoordinates();
            }

            Vector3 rideDir = Vector3.ProjectOnPlane(end - start, Vector3.up).normalized;

            Vector3 rideDirNormal = Vector3.Cross(rideDir, Vector3.up).normalized;

            Vector3 leftStartCorner = start - 0.5f * width * rideDirNormal;

            float heightmapSpacing = TerrainManager.GetHeightmapSpacing();
            int widthSteps = Mathf.CeilToInt(width / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(distanceToModify / heightmapSpacing);

            int2 leftSCorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner);
            int2 rightSCorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + widthSteps * rideDirNormal);
            int2 leftECorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + lengthSteps * rideDir);
            int2 rightECorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + lengthSteps * rideDir + widthSteps * rideDirNormal);

            int minX = Mathf.Min(leftSCorner.x, rightSCorner.x, leftECorner.x, rightECorner.x);
            int maxX = Mathf.Max(leftSCorner.x, rightSCorner.x, leftECorner.x, rightECorner.x);
            int minY = Mathf.Min(leftSCorner.y, rightSCorner.y, leftECorner.y, rightECorner.y);
            int maxY = Mathf.Max(leftSCorner.y, rightSCorner.y, leftECorner.y, rightECorner.y);
            int hMapWidth = maxX - minX + 1;
            int hMapHeight = maxY - minY + 1;

            HashSet<int2> coordinates = new();

            float[,] heights = TerrainManager.Floor.terrainData.GetHeights(minX, minY, hMapWidth, hMapHeight);

            height = TerrainManager.WorldUnitsToHeightmapUnits(height); // heightmap units

            for (int i = 0; i <= lengthSteps; i++)
            {
                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 position = leftStartCorner + j * heightmapSpacing * rideDirNormal + i * heightmapSpacing * rideDir;

                    int2 heightmapPosition = TerrainManager.WorldToHeightmapCoordinates(position);

                    coordinates.Add(heightmapPosition);

                    int x = heightmapPosition.x - minX;
                    int y = heightmapPosition.y - minY;

                    heights[y, x] = height;
                }
            }

            TerrainManager.Floor.terrainData.SetHeightsDelayLOD(minX, minY, heights);

            var result = new HeightmapCoordinates(coordinates);
            return result;
        }
       
        private HeightmapCoordinates DrawRamp(Vector3 start, Vector3 end, float startHeight)
        {
            start.y = 0;
            end.y = 0;

            float distanceToModify = Vector3.Distance(start, end);

            if (distanceToModify == 0)
            {
                return new HeightmapCoordinates();
            }

            Vector3 rideDir = Vector3.ProjectOnPlane(end - start, Vector3.up).normalized;

            Vector3 rideDirNormal = Vector3.Cross(rideDir, Vector3.up).normalized;

            Vector3 leftStartCorner = start - 0.5f * width * rideDirNormal;
            
            float heightmapSpacing = TerrainManager.GetHeightmapSpacing();
            int widthSteps = Mathf.CeilToInt(width / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(distanceToModify / heightmapSpacing);

            int2 leftSCorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner);
            int2 rightSCorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + widthSteps * rideDirNormal);
            int2 leftECorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + lengthSteps * rideDir);
            int2 rightECorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + lengthSteps * rideDir + widthSteps * rideDirNormal);

            int minX = Mathf.Min(leftSCorner.x, rightSCorner.x, leftECorner.x, rightECorner.x);
            int maxX = Mathf.Max(leftSCorner.x, rightSCorner.x, leftECorner.x, rightECorner.x);
            int minY = Mathf.Min(leftSCorner.y, rightSCorner.y, leftECorner.y, rightECorner.y);
            int maxY = Mathf.Max(leftSCorner.y, rightSCorner.y, leftECorner.y, rightECorner.y);
            int hMapWidth = maxX - minX + 1;
            int hMapHeight = maxY - minY + 1;

            HashSet<int2> coordinates = new();

            float[,] heights = TerrainManager.Floor.terrainData.GetHeights(minX, minY, hMapWidth, hMapHeight);

            float endHeight = startHeight + GetHeightDifferenceForDistance(distanceToModify); // world units

            for (int i = 0; i <= lengthSteps; i++)
            {
                float heightAtLength = startHeight + (endHeight - startHeight) * (i / (float)lengthSteps); // world units                
                heightAtLength = TerrainManager.WorldUnitsToHeightmapUnits(heightAtLength); // heightmap units

                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 position = leftStartCorner + j * heightmapSpacing * rideDirNormal + i * heightmapSpacing * rideDir;

                    int2 heightmapPosition = TerrainManager.WorldToHeightmapCoordinates(position);                 

                    coordinates.Add(heightmapPosition);

                    int x = heightmapPosition.x - minX;
                    int y = heightmapPosition.y - minY;

                    heights[y, x] = heightAtLength;
                }
            }

            TerrainManager.Floor.terrainData.SetHeightsDelayLOD(minX, minY, heights);

            var result = new HeightmapCoordinates(coordinates);            
            return result;
        }
        
        public SlopeSnapshot GetSlopeSnapshot() => new(this);

        PlacementResult lastPlacementResult;

        public void PlaceObstacle(TakeoffBuilder takeoff)
        {             
            LastConfirmedSnapshot.Revert();

            Vector3 waypointStartXZ = takeoff.GetStartPoint();
            waypointStartXZ.y = 0; // ignore the takeoff's height for the XZ calculations
            Vector3 waypointEndXZ = takeoff.GetEndPoint();
            waypointEndXZ.y = 0; // ignore the takeoff's height for the XZ calculations

            LastRideDirection = Vector3.ProjectOnPlane(takeoff.GetRideDirection(), Vector3.up);

            // check if entire takeoff is before the slope
            if (IsBeforeStart(waypointStartXZ) && IsBeforeStart(waypointEndXZ))
            {
                TerrainManager.FitObstacleOnFlat(takeoff);
                return;
            }

            bool isWaypoint;
            float newRemainingLength = remainingLength;
            Vector3 newEndPoint;
            HeightmapCoordinates coords = new();
           
            width = Mathf.Max(width, takeoff.GetBottomWidth());

            float startHeight = endPoint.y;


            // obstacle is on the border of slope start
            if (IsBeforeStart(waypointStartXZ) && IsOnActivePartOfSlope(waypointEndXZ))
            {
                coords.Add(DrawFlat(waypointStartXZ, waypointEndXZ, startHeight));
                TerrainManager.FitObstacleOnFlat(takeoff);
                newRemainingLength -= Vector3.Distance(endPoint, takeoff.GetEndPoint());
                newEndPoint = takeoff.GetEndPoint();
                isWaypoint = true;
            }
            // whole obstacle is on slope
            else if (IsOnActivePartOfSlope(waypointStartXZ) && IsOnActivePartOfSlope(waypointEndXZ))
            {                
                coords.Add(DrawRamp(endPoint, waypointEndXZ, startHeight));
                FitObstacleOnSlope(takeoff);
                newRemainingLength -= GetXZDistanceFromSlopeLength(Vector3.Distance(endPoint, takeoff.GetEndPoint()));
                newEndPoint = takeoff.GetEndPoint();
                isWaypoint = true;
            }
            // obstacle is on border of slope end
            else if (IsOnActivePartOfSlope(waypointStartXZ) && IsAfterSlope(waypointEndXZ))
            {
                coords.Add(DrawRamp(endPoint, waypointEndXZ, startHeight));
                FitObstacleOnSlope(takeoff);
                newEndPoint = GetFinishedEndPoint();
                newRemainingLength = 0;
                isWaypoint = true;
            }
            // whole obstacle is after the slope
            else if (IsAfterSlope(waypointStartXZ) && IsAfterSlope(waypointEndXZ))
            {
                newEndPoint = GetFinishedEndPoint();
                coords.Add(DrawRamp(endPoint, newEndPoint, startHeight));
                coords.Add(DrawFlat(newEndPoint, waypointEndXZ, endHeight));
                TerrainManager.FitObstacleOnFlat(takeoff);
                newRemainingLength = 0;
                isWaypoint = true;
            }
            // slope is so short that the obstacle starts before it but ends after it.
            else
            {
                newEndPoint = GetFinishedEndPoint();
                coords.Add(DrawRamp(waypointStartXZ, newEndPoint, startHeight));                
                FitObstacleOnSlope(takeoff); // place the landing on the flat terrain
                newRemainingLength = 0;
                isWaypoint = true;
            }                   

            UpdateHighlight();

            lastPlacementResult = new PlacementResult(newRemainingLength, newEndPoint, isWaypoint, coords);

            return;
        }     
        
       
        public void PlaceObstacle(LandingBuilder landing)
        {
            LastConfirmedSnapshot.Revert();

            Vector3 waypointStartXZ = landing.GetStartPoint();
            Vector3 waypointEndXZ = landing.GetEndPoint();

            // ignore the landing's y-axis position for the XZ calculations
            waypointStartXZ.y = 0; 
            waypointEndXZ.y = 0;

            // the ride direction here is from last line element to the landing, ignoring the landings rotation
            LastRideDirection = Vector3.ProjectOnPlane(waypointStartXZ - Line.Instance.GetLastLineElement().GetEndPoint()
                , Vector3.up).normalized;

            // check if entire landing is before the slope
            if (IsBeforeStart(waypointStartXZ) && IsBeforeStart(waypointEndXZ))
            {
                TerrainManager.FitObstacleOnFlat(landing);
                return;
            }

            bool isWaypoint;
            float newRemainingLength = remainingLength;
            Vector3 newEndPoint;
            HeightmapCoordinates coords = new();

            float distanceToWaypointStartXZ = Vector3.Distance(endPoint, waypointStartXZ);

            width = Mathf.Max(width, landing.GetBottomWidth() + 1f);

            float startHeight = endPoint.y;

            // obstacle is on the border of slope start
            if (IsBeforeStart(waypointStartXZ) && IsOnActivePartOfSlope(waypointEndXZ))
            {
                // ramp is drawn from before the slope's Start point so Start height is bigger
                startHeight -= GetHeightDifferenceForDistance(Vector3.Distance(Start, waypointStartXZ));
                coords.Add(DrawRamp(waypointStartXZ, waypointEndXZ, startHeight));
                FitObstacleOnSlope(landing);
                newRemainingLength -= GetXZDistanceFromSlopeLength(Vector3.Distance(endPoint, landing.GetEndPoint()));
                newEndPoint = landing.GetEndPoint();
                isWaypoint = true;
            }
            // whole obstacle is on slope
            else if (IsOnActivePartOfSlope(waypointStartXZ) && IsOnActivePartOfSlope(waypointEndXZ))
            {
                // the ramp starts lower depending on how far the landings start point is from the slope's start point
                startHeight += GetHeightDifferenceForDistance(Vector3.Distance(endPoint, waypointStartXZ));
                coords.Add(DrawRamp(waypointStartXZ, waypointEndXZ, startHeight));
                FitObstacleOnSlope(landing);
                newRemainingLength -= GetXZDistanceFromSlopeLength(Vector3.Distance(endPoint, landing.GetEndPoint()));
                newEndPoint = landing.GetEndPoint();
                isWaypoint = true;
            }
            // obstacle is on border of slope end  OR whole obstacle is after the slope         
            else if ((IsOnActivePartOfSlope(waypointStartXZ) && IsAfterSlope(waypointEndXZ)) 
                || (IsAfterSlope(waypointStartXZ) && IsAfterSlope(waypointEndXZ)))
            {     
                coords.Add(DrawFlat(waypointStartXZ, waypointEndXZ, endHeight));
                TerrainManager.FitObstacleOnFlat(landing); // place the landing on the flat terrain
                Vector3 landingDirectionXZ = Vector3.ProjectOnPlane(landing.GetRideDirection(), Vector3.up).normalized;
                newEndPoint = endPoint + LastRideDirection.normalized * distanceToWaypointStartXZ + landingDirectionXZ * (newRemainingLength - distanceToWaypointStartXZ);
                newEndPoint.y = endHeight;
                newRemainingLength = 0;
                isWaypoint = true;
            }
            // slope is so short that the obstacle starts before it but ends after it.
            else
            {
                coords.Add(DrawFlat(waypointStartXZ, waypointEndXZ, endHeight));
                TerrainManager.FitObstacleOnFlat(landing); // place the landing on the flat terrain
                newEndPoint = GetFinishedEndPoint();
                newRemainingLength = 0;
                isWaypoint = true;
            }            

            UpdateHighlight();

            LastRideDirection = Vector3.ProjectOnPlane(landing.GetRideDirection(), Vector3.up).normalized;

            lastPlacementResult = new PlacementResult(newRemainingLength, newEndPoint, isWaypoint, coords);

            return;
        }


        public void ConfirmChanges<T>(ObstacleBase<T> element) where T : MeshGeneratorBase
        {

            if (lastPlacementResult.IsWaypoint && element.TryGetComponent<ILineElement>(out var lineElement))
            {
                waypoints.AddWaypoint(lineElement);
            }

            remainingLength = lastPlacementResult.Remaininglength;
            endPoint = lastPlacementResult.NewEndPoint;

            if (remainingLength <= 0)
            {
                finished = true;
                TerrainManager.Instance.ActiveSlope = null;
            }

            element.AddSlopeHeightmapCoords(lastPlacementResult.ChangedHeightmapCoords);
            TerrainManager.Instance.SetHeight(endPoint.y);            
            lastPlacementResult = new(this); // reset last change

            TerrainManager.ConfirmChanges();

            LastConfirmedSnapshot = GetSlopeSnapshot();

            UpdateHighlight();
        }

        public void RemoveWaypoint(ILineElement element)
        {
            waypoints.RemoveWaypoint(element);
            if (waypoints.Count == 0)
            {
                UIManager.Instance.GetDeleteUI().DeleteSlopeButtonEnabled = true;
            }
        }

        public void Delete()
        {
            if (waypoints.Count > 0)
            {
                Debug.LogError("Deleting slope with waypoints. This should not happen.");
                waypoints.Clear();
            }            

            TerrainManager.Instance.ActiveSlope = null;
            
            flatToStartPoint.MarkAs(CoordinateState.Free);

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Start, 0.5f);
            Gizmos.DrawSphere(endPoint, 0.5f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(GetFinishedEndPoint(), 0.5f);
        }
    }
}