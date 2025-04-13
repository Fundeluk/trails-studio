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

namespace Assets.Scripts.Builders
{
    public class SlopeChange : SlopeChangeBase
    {
        public struct SlopeSnapshot
        {
            private readonly SlopeChange slope;
            public bool finished;
            public float heightAtEndpoint;
            public float remainingLength;
            public float width;
            public Vector3 endPoint;
            public Vector3 rideDirection;
            public SlopeSnapshot(SlopeChange slope)
            {
                this.slope = slope;
                finished = slope.finished;
                heightAtEndpoint = slope.heightAtEndpoint;
                remainingLength = slope.remainingLength;
                width = slope.width;
                endPoint = slope.endPoint;
                rideDirection = slope.lastRideDirection;
            }

            public readonly void Revert()
            {
                if (!finished)
                {
                    TerrainManager.Instance.ActiveSlope = slope;
                }

                slope.remainingLength = remainingLength;
                slope.endPoint = endPoint;
                slope.lastRideDirection = rideDirection;
                slope.width = width;
                slope.heightAtEndpoint = heightAtEndpoint;
                slope.finished = finished;

                slope.UpdateHighlight();
            }
        }

        // dictionary -> order of insertion not needed here as deletion of obstacles is performed from the end
        public class WaypointDict : IEnumerable<KeyValuePair<ILineElement, SlopeSnapshot>>
        {            

            private readonly SlopeChange owner;
            public Dictionary<ILineElement, SlopeSnapshot> waypoints = new();

            public void AddWaypoint(ILineElement waypoint)
            {
                SlopeSnapshot snapshot = new(owner);
                waypoints[waypoint] = snapshot;
                waypoint.SetSlope(owner);
            }

            public bool RemoveWaypoint(ILineElement item)
            {
                if (!waypoints.ContainsKey(item))
                {
                    return false;
                }

                SlopeSnapshot snapshot = waypoints[item];
                item.SetSlope(null);
                snapshot.Revert(); // revert the slope to the state before the waypoint was added
                item.GetSlopeHeightmapCoordinates()?.UnmarkAsOccupied(); // unmark the heightmap coordinates of the waypoint
                TerrainManager.Instance.SetHeight(snapshot.heightAtEndpoint); // set the height of the terrain to the height at the waypoint

                return waypoints.Remove(item);
            }            
            public int Count => waypoints.Count;            

            public SlopeSnapshot this[ILineElement key] { get => ((IDictionary<ILineElement, SlopeSnapshot>)waypoints)[key]; set => ((IDictionary<ILineElement, SlopeSnapshot>)waypoints)[key] = value; }


            public IEnumerator<KeyValuePair<ILineElement, SlopeSnapshot>> GetEnumerator()
            {
                return ((IEnumerable<KeyValuePair<ILineElement, SlopeSnapshot>>)waypoints).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)waypoints).GetEnumerator();
            }            

            public bool ContainsKey(ILineElement key)
            {
                return ((IDictionary<ILineElement, SlopeSnapshot>)waypoints).ContainsKey(key);
            }

            public bool Remove(ILineElement key)
            {
                return ((IDictionary<ILineElement, SlopeSnapshot>)waypoints).Remove(key);
            }

            public bool TryGetValue(ILineElement key, out SlopeSnapshot value)
            {
                return ((IDictionary<ILineElement, SlopeSnapshot>)waypoints).TryGetValue(key, out value);
            }            

            public void CopyTo(KeyValuePair<ILineElement, SlopeSnapshot>[] array, int arrayIndex)
            {
                ((ICollection<KeyValuePair<ILineElement, SlopeSnapshot>>)waypoints).CopyTo(array, arrayIndex);
            }            

            public WaypointDict(SlopeChange owner)
            {
                this.owner = owner;
            }
        }

        public float angle; // angle of the slope       

        HeightmapCoordinates affectedCoordinates;

        public Terrain terrain;

        public float remainingLength;

        /// <summary>
        /// Width between last two waypoints
        /// </summary>
        public float width;

        public WaypointDict waypoints;

        private float heightAtEndpoint;

        /// <summary>
        /// The end point of the slope. If the slope is not finished, this is the point that is the farthest from the start point that the slope affects.
        /// </summary>
        public Vector3 endPoint;

        public Vector3 lastRideDirection;

        protected override void UpdateHighlight()
        {
            if (finished || length == 0)
            {
                highlight.enabled = false;
                return;
            }
            else
            {
                highlight.enabled = true;
            }

            Vector3 rideDirNormal = Vector3.Cross(lastRideDirection, Vector3.up).normalized;

            Vector3 position = Vector3.Lerp(endPoint, endPoint + remainingLength * lastRideDirection, 0.5f);
            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, rideDirNormal);

            highlight.transform.SetPositionAndRotation(position, rotation);

            DecalProjector decalProjector = highlight.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(remainingLength, width, 20);
        }

        /// <summary>
        /// Initializes a slope with a given start point, length and end height.
        /// </summary>
        public void Initialize(Vector3 start, float endHeight, float length)
        {
            waypoints = new WaypointDict(this);
            this.terrain = TerrainManager.GetTerrainForPosition(start);
            this.startHeight = start.y;
            this.endHeight = endHeight;

            this.start = start;
            endPoint = start; 
            
            heightAtEndpoint = endPoint.y;

            this.length = length;
            remainingLength = length;

            this.angle = 90 - Mathf.Atan(length / (endHeight - startHeight)) * Mathf.Rad2Deg;

            width = Line.Instance.GetLastLineElement().GetBottomWidth();
            this.lastRideDirection = Line.Instance.GetCurrentRideDirection();

            affectedCoordinates = new HeightmapCoordinates(Line.Instance.GetLastLineElement().GetEndPoint(), start, width);
            affectedCoordinates.MarkAsOccupied();

            this.highlight = GetComponent<DecalProjector>();

            TerrainManager.Instance.AddSlope(this);

            UpdateHighlight();
        }

        public void InitializeForTesting(float distanceFromLastElem, float endHeight, float length, ILineElement prevElem)
        {

            start = prevElem.GetEndPoint() + distanceFromLastElem * prevElem.GetRideDirection();
            endPoint = start;

            this.startHeight = start.y;
            this.endHeight = endHeight;

            heightAtEndpoint = endPoint.y;

            this.length = length;
            remainingLength = length;

            width = prevElem.GetBottomWidth();

            //AddPathToAffectedCoordinates(prevElem.GetEndPoint(), start);

            this.highlight = GetComponent<DecalProjector>();

            this.angle = 90 - Mathf.Atan(length / (endHeight - startHeight)) * Mathf.Rad2Deg;

            this.lastRideDirection = prevElem.GetRideDirection();

            UpdateHighlight();
        }

        
        /// <returns>Returns whether a position is before this slope's start point with respect to its <see cref="lastRideDirection"/>.</returns>        
        private bool IsPositionBeforeSlopeStart(Vector3 position)
        {
            Vector3 slopeEndToPosition = position - endPoint;
            float projection = Vector3.Dot(slopeEndToPosition, lastRideDirection);
            if (projection < 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the position is on the slope.
        /// </summary>        
        public bool IsOnSlope(Vector3 position)
        {
            if (finished)
            {
                return false;
            }

            if (IsPositionBeforeSlopeStart(position))
            {
                return false;
            }

            float distance = Vector3.Distance(endPoint, position);

            return distance <= remainingLength;
        }

        /// <summary>
        /// Calculates the height difference for a given distance using the slope's angle.
        /// </summary>
        /// <remarks>Is not bounded by the slope's <see cref="remainingLength"/></remarks>
        private float GetHeightDifferenceForDistance(float distance)
        {
            float heightDif = distance * Mathf.Tan(angle * Mathf.Deg2Rad);
            Debug.Log("HeightDif for distance: " + distance + "m is: " + heightDif);
            return heightDif;
        }
       
        private HeightmapCoordinates DrawRamp(Vector3 start, Vector3 end, float startHeight)
        {
            Terrain terrain = TerrainManager.GetTerrainForPosition(start);           

            float distanceToModify = Vector3.Distance(start, end);

            Vector3 rideDirNormal = Vector3.Cross(lastRideDirection, Vector3.up).normalized;

            Vector3 leftStartCorner = start - 0.5f * width * rideDirNormal;
            
            float heightmapSpacing = TerrainManager.GetHeightmapSpacing(terrain);
            int widthSteps = Mathf.CeilToInt(width / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(distanceToModify / heightmapSpacing);

            int2 leftSCorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner, terrain);
            int2 rightSCorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + widthSteps * rideDirNormal, terrain);
            int2 leftECorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + lengthSteps * lastRideDirection, terrain);
            int2 rightECorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + lengthSteps * lastRideDirection + widthSteps * rideDirNormal, terrain);

            int minX = Mathf.Min(leftSCorner.x, rightSCorner.x, leftECorner.x, rightECorner.x);
            int maxX = Mathf.Max(leftSCorner.x, rightSCorner.x, leftECorner.x, rightECorner.x);
            int minY = Mathf.Min(leftSCorner.y, rightSCorner.y, leftECorner.y, rightECorner.y);
            int maxY = Mathf.Max(leftSCorner.y, rightSCorner.y, leftECorner.y, rightECorner.y);
            int hMapWidth = maxX - minX + 1;
            int hMapHeight = maxY - minY + 1;

            float endHeight = startHeight + GetHeightDifferenceForDistance(distanceToModify); // world units            

            HashSet<int2> coordinates = new();

            float[,] heights = terrain.terrainData.GetHeights(minX, minY, hMapWidth, hMapHeight);

            for (int i = 0; i <= lengthSteps; i++)
            {
                float heightAtLength = startHeight + (endHeight - startHeight) * (i / (float)lengthSteps); // world units
                heightAtEndpoint = heightAtLength;
                heightAtLength = TerrainManager.WorldUnitsToHeightmapUnits(heightAtLength, terrain); // heightmap units

                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 position = leftStartCorner + j * heightmapSpacing * rideDirNormal + i * heightmapSpacing * lastRideDirection;

                    int2 heightmapPosition = TerrainManager.WorldToHeightmapCoordinates(position, terrain);                 

                    coordinates.Add(heightmapPosition);

                    int x = heightmapPosition.x - minX;
                    int y = heightmapPosition.y - minY;

                    heights[y, x] = heightAtLength;
                }
            }

            terrain.terrainData.SetHeights(minX, minY, heights);

            var result = new HeightmapCoordinates(terrain, minX, minY, maxX - minX + 1, maxY - minY + 1, coordinates);
            result.MarkAsOccupied();
            return result;
        }        

        /// <summary>
        /// Adds an landing to this slope change. If it is farther from the current end of the slope than the remaining length, the slope change is finished.
        /// </summary>
        /// <returns>True if the waypoint finishes the slope change, false if not.</returns>
        public bool AddWaypoint(Takeoff takeoff)
        {
            Vector3 waypointStart = takeoff.GetStartPoint();
            Vector3 waypointEnd = takeoff.GetEndPoint();

            // check if entire takeoff is before the slope
            if (waypoints.Count == 0 && IsPositionBeforeSlopeStart(waypointEnd))
            {
                return false;
            }

            waypoints.AddWaypoint(takeoff);

            float distanceToWaypointStart = Vector3.Distance(endPoint, waypointStart);
            float distanceToWaypointEnd = Vector3.Distance(endPoint, waypointEnd);

            lastRideDirection = takeoff.GetRideDirection();

            width = Mathf.Max(width, takeoff.GetBottomWidth());

            float distanceToModify = Mathf.Min(distanceToWaypointEnd, remainingLength);
            Debug.Log("distance to modify: " + distanceToModify);
            Vector3 newEndPoint = endPoint + lastRideDirection.normalized * distanceToModify;

            HeightmapCoordinates coords;

            // takeoff is on border of the slope start            
            if (waypoints.Count == 1 && IsPositionBeforeSlopeStart(waypointStart))
            {
                // mark the part from the slope start to takeoff end so that a ramp wont be drawn under the takeoff and
                // create an overhang
                coords = new HeightmapCoordinates(endPoint, waypointEnd, width);
            }
            // takeoff's start point is after the slope start point
            else
            {
                Vector3 rampEndPoint = newEndPoint;

                // if takeoff is on the border of the slope end
                if (remainingLength > distanceToWaypointStart && remainingLength <= distanceToWaypointEnd)
                {
                    // draw the ramp up to takeoffs end point
                    rampEndPoint = waypointEnd;
                }

                coords = DrawRamp(endPoint, rampEndPoint, heightAtEndpoint);
                TerrainManager.Instance.SetHeight(endHeight);
            }

            takeoff.AddSlopeHeightmapCoords(coords);

            endPoint = newEndPoint;            
            
            remainingLength -= distanceToModify;

            if (remainingLength <= 0)
            {
                remainingLength = 0;
                finished = true;
                TerrainManager.Instance.ActiveSlope = null;
            }
            
            UpdateHighlight();

            return finished;
        }

        /// <summary>
        /// Adds an landing to this slope change. If it is farther from the current end of the slope than the remaining length, the slope change is finished.
        /// </summary>
        /// <returns>True if the waypoint finishes the slope change, false if not.</returns>
        public bool AddWaypoint(Landing landing)
        {
            Vector3 waypointStart = landing.GetStartPoint();
            Vector3 waypointEnd = landing.GetEndPoint();

            // check if entire landing is before the slope
            // check if entire takeoff is before the slope
            if (waypoints.Count == 0 && IsPositionBeforeSlopeStart(waypointEnd))
            {
                return false;
            }

            waypoints.AddWaypoint(landing);

            float distanceToWaypointStart = Vector3.Distance(endPoint, waypointStart);
            float distanceToWaypointEnd = Vector3.Distance(endPoint, waypointEnd);             


            width = Mathf.Max(width, landing.GetBottomWidth() + 1f);

            float distanceToModify = Mathf.Min(distanceToWaypointEnd, remainingLength);

            Vector3 newEndPoint;

            if (remainingLength < distanceToWaypointStart)
            {
                lastRideDirection = waypointStart - endPoint;
                newEndPoint = endPoint + lastRideDirection * remainingLength;
            }
            else
            {
                lastRideDirection = landing.GetRideDirection();
                newEndPoint = endPoint + (waypointStart - endPoint).normalized * distanceToWaypointStart + lastRideDirection * (distanceToModify - distanceToWaypointStart);
            }

            Debug.Log("distance to modify: " + distanceToModify);

            float startHeight = heightAtEndpoint;
            HeightmapCoordinates coords;

            // landing is on the border of the slope start
            if (waypoints.Count == 1 && IsPositionBeforeSlopeStart(waypointStart))
            {
                // ramp is drawn from before the slope's start point so start height is bigger
                startHeight = heightAtEndpoint - GetHeightDifferenceForDistance(Vector3.Distance(endPoint, waypointStart));                
            }

            // landing ends after the slope end, dont draw ramp, just lower all terrain
            if (remainingLength <= distanceToWaypointEnd)
            {
                TerrainManager.Instance.SetHeight(endHeight);
            }
            // landing's endpoint is on the slope, draw ramp
            else
            {
                coords = DrawRamp(waypointStart, newEndPoint, startHeight);
                landing.AddSlopeHeightmapCoords(coords);
                TerrainManager.Instance.SetHeight(heightAtEndpoint);          
            }

            endPoint = newEndPoint;

            remainingLength -= distanceToModify;

            // landing is on border of the slope end or after it
            if (remainingLength <= 0)
            {
                finished = true;
                remainingLength = 0;
                TerrainManager.Instance.ActiveSlope = null;               
            }                   

            UpdateHighlight();
            return finished;
        }        

        public void RemoveWaypoint(ILineElement element)
        {
            waypoints.RemoveWaypoint(element);
        }
    }
}