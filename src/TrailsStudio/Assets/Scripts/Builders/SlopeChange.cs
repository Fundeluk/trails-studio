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

namespace Assets.Scripts.Builders
{
    public class SlopeChangeBuilder
    {
        public readonly Vector3 start;
        private readonly GameObject highlight;
        private readonly float startHeight;
        private float endHeight;
        private float length;

        private void UpdateHighlight()
        {
            Vector3 newPos = Vector3.Lerp(start, start + length * Line.Instance.GetLastLineElement().GetRideDirection(), 0.5f);
            newPos.y = Mathf.Max(startHeight, endHeight);
            Highlighter.UpdateHighlight(highlight, length, newPos, Line.Instance.GetLastLineElement().GetRideDirection());
        }

        public SlopeChangeBuilder(Vector3 start, float heightDifference=0, float length=0)
        {
            this.length = length;
            this.start = start;
            this.startHeight = start.y;
            this.endHeight = startHeight + heightDifference;
            highlight = TerrainManager.Instance.GetHighlight();

            UpdateHighlight();

            TerrainManager.Instance.activeSlopeBuilder = this;
        }

        public void SetLength(float length)
        {
            this.length = length;
            UpdateHighlight();
        }

        public void SetHeightDifference(float heightDifference)
        {
            this.endHeight = startHeight + heightDifference;
            UpdateHighlight();
        }

        public SlopeChange Build()
        {
            SlopeChange slopeChange = new (start, endHeight, length, highlight);
            TerrainManager.Instance.AddSlope(slopeChange);
            return slopeChange;
        }


    }
    // TODO maybe introduce a builder pattern for this (beacuse of length and height difference setting before building)
    public class SlopeChange
    {
        /// <summary>
        /// heightmap bounds is basically an axis aligned bounding square in the heightmap. 
        /// that is useful because any data passed to or from the heightmap is in form of a square subset of the heightmap. 
        /// the heightmap coordinates that the slope affects may be a subset of that bounding box, so we need to store that subset as well.
        /// </summary>
        public Dictionary<Terrain, List<int2>> affectedTerrainCoordinates = new(); // <- coordinates in terrain heightmaps that are actually affected by the change

        public float angle; // angle of the slope
        public float startHeight;
        public float endHeight;

        private GameObject highlightProjector;

        /// <summary>
        /// total length of the slope
        /// </summary>
        public float? length = null;

        public float remainingLength;

        /// <summary>
        /// width between last two waypoints
        /// </summary>
        public float width;

        // debug purposes
        List<Vector3> positions = new();

        public List<ILineElement> pathWaypoints = new();
        public Vector3 startPoint;

        /// <summary>
        /// The end point of the slope. If the slope is not finished, this is the point that is the farthest from the start point that the slope affects.
        /// </summary>
        public Vector3 endPoint;

        bool finished = false;

        /// <summary>
        /// Creates a slope with a given angle between two points.
        /// </summary>
        /// <param name="angle">Angle in degrees. Negative value means downwards slope, positive upwards.</param>
        public SlopeChange(float angle, Vector3 start, float length, float width)
        {
            this.length = length;
            remainingLength = length;
            this.startHeight = start.y;
            this.angle = angle;
            if (angle < 0)
            {
                this.endHeight = startHeight - Mathf.Tan(-angle * Mathf.Deg2Rad) * length;
            }
            else if (angle > 0)
            {
                this.endHeight = startHeight + Mathf.Tan(angle * Mathf.Deg2Rad) * length;
            }
            else
            {
                Debug.LogError("Angle must be non-zero.");
            }
            startPoint = start;
            endPoint = start;
            this.width = width;

            affectedTerrainCoordinates[TerrainManager.GetTerrainForPosition(start)] = new List<int2>();
        }

        /// <summary>
        /// Creates a slope with a given height difference between two points.
        /// </summary>
        /// <param name="heightDifference">Height difference between endpoints in metres. Negative value means downwards slope, positive upwards.</param>
        public SlopeChange(Vector3 start, float endHeight, float length, GameObject highlight)
        {
            this.startHeight = start.y;
            this.endHeight = endHeight;

            startPoint = start;
            endPoint = start;

            this.length = length;
            remainingLength = length;

            affectedTerrainCoordinates[TerrainManager.GetTerrainForPosition(start)] = new List<int2>();
            this.highlightProjector = highlight;            

            UpdateHighlight();
        }

        private void UpdateHighlight()
        {
            Vector3 newPos = endPoint + 0.5f * remainingLength * Line.Instance.GetLastLineElement().GetRideDirection();
            newPos.y = Mathf.Max(startHeight, endHeight);

            Highlighter.UpdateHighlight(highlightProjector, remainingLength, newPos, Line.Instance.GetLastLineElement().GetRideDirection());
        }

        /// <summary>
        /// Returns true if the position is on the slope.
        /// </summary>        
        public bool IsOnSlope(Vector3 position)
        {
            if (remainingLength <= 0)
            {
                return false;
            }           

            // TODO obstacle is placed so that its center is at the position, but here we need to account for its endpoint which is not known yet
            float distance = Vector3.Distance(endPoint, position);

            return distance <= remainingLength;
        }


        /// <summary>
        /// Adds a waypoint to this slope change. If the waypoint is farther from the current end of the slope than the remaining length, the slope change is finished.
        /// </summary>
        /// <param name="waypoint">The obstacle to add.</param>
        /// <returns>True if the waypoint finishes the slope change, false if not.</returns>
        public bool AddWaypoint(ILineElement waypoint)
        {
            if (!this.length.HasValue)
            {
                throw new System.Exception("Length must be set before adding waypoints.");
            }

            Vector3 currentSlopeStart = pathWaypoints.Count == 0 ? startPoint : pathWaypoints[^1].GetEndPoint();
            
            Vector3 waypointEnd = waypoint.GetEndPoint();

            Vector3 currentSlopeDirection = (waypointEnd - currentSlopeStart).normalized;

            float distanceToWaypoint = Vector3.Distance(currentSlopeStart, waypointEnd);

            if (distanceToWaypoint > remainingLength)
            {
                Debug.Log("Waypoint is farther than the remaining length.");
                finished = true;
                endPoint = currentSlopeStart + currentSlopeDirection.normalized * remainingLength;
            }
            else if (distanceToWaypoint == remainingLength)
            {
                Debug.Log("Waypoint is at the remaining length.");
                finished = true;
                endPoint = waypointEnd;
            }
            else
            {
                Debug.Log("Waypoint is closer than the remaining length.");
                finished = false;
                endPoint = waypointEnd;
            }

            float distanceToModify = Vector3.Distance(currentSlopeStart, endPoint);
            Debug.Log("Distance to modify: " + distanceToModify);

            Terrain terrain = TerrainManager.GetTerrainForPosition(currentSlopeStart);

            Vector3 rideDirNormal = Vector3.Cross(currentSlopeDirection, Vector3.up).normalized;

            float currentWidth = Mathf.Max(Line.Instance.line[^2].GetBottomWidth(), waypoint.GetBottomWidth());
            Debug.Log("Current width: " + currentWidth);

            Vector3 leftStartCorner = currentSlopeStart - 0.5f * currentWidth * rideDirNormal;

            // TODO account for a span of multiple terrains

            float currentHeight = startHeight + (endHeight - startHeight) * ((length.Value - remainingLength) / length.Value); // height at the current end of the slope
            Debug.Log("Current height: " + currentHeight);
            float waypointHeight = startHeight + (endHeight - startHeight) * ((length.Value - remainingLength + distanceToModify) / length.Value); // height that the slope should have at the waypoint
            Debug.Log("Waypoint height: " + waypointHeight);

            float[,] heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
            float heightmapSpacing = TerrainManager.GetHeightmapSpacing(terrain);
            int widthSteps = Mathf.CeilToInt(currentWidth / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(distanceToModify / heightmapSpacing);

            for (int i = 0; i <= lengthSteps; i++)
            {
                float heightAtWidth = currentHeight + (waypointHeight - currentHeight) * (i / (float)lengthSteps); // world units

                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 position = leftStartCorner + j * heightmapSpacing * rideDirNormal + i * heightmapSpacing * currentSlopeDirection;
                    //positions.Add(position);

                    int2 heightmapPosition = TerrainManager.WorldToHeightmapCoordinates(position, terrain);

                    affectedTerrainCoordinates[terrain].Add(heightmapPosition);

                    heights[heightmapPosition.x, heightmapPosition.y] = TerrainManager.WorldUnitsToHeightmapUnits(heightAtWidth, terrain);
                }
            }

            terrain.terrainData.SetHeights(0, 0, heights);
            //AddWaypointToAffectedCoordinates(waypoint);
            TerrainManager.Instance.MarkTerrainAsOccupied(terrain, affectedTerrainCoordinates[terrain]);

            TerrainManager.Instance.SetHeight(endHeight);
            
            if (distanceToModify >= remainingLength)
            {
                remainingLength = 0;
            }
            else
            {
                remainingLength -= distanceToModify;
            }

            UpdateHighlight();

            return finished;
        }

        private void AddWaypointToAffectedCoordinates(ILineElement waypoint)
        {
            HeightmapBounds bounds = waypoint.GetHeightmapBounds();
            for (int i = bounds.startX; i < bounds.startX + bounds.width; i++)
            {
                for (int j = bounds.startZ; j < bounds.startZ + bounds.height; j++)
                {
                    affectedTerrainCoordinates[bounds.terrain].Add(new int2(j, i));
                }
            }
        }

        public void Undo()
        {
            foreach (var terrain in affectedTerrainCoordinates.Keys)
            {
                float[,] heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
                foreach (var coord in affectedTerrainCoordinates[terrain])
                {
                    heights[coord.x, coord.y] = TerrainManager.WorldUnitsToHeightmapUnits(startHeight, terrain);
                }
                terrain.terrainData.SetHeights(0, 0, heights);
            }

            GameObject.Destroy(highlightProjector);

            //TerrainManager.Instance.MarkTerrainAsFree(affectedTerrainCoordinates);
        }

        public void OnDrawGizmos()
        {
            Gizmos.DrawCube(startPoint, Vector3.one);
            foreach (var waypoint in pathWaypoints)
            {
                Gizmos.DrawCube(waypoint.GetEndPoint(), Vector3.one);
            }

            Vector3 currentSlopeStart = pathWaypoints.Count == 0 ? startPoint : pathWaypoints[^1].GetEndPoint();

            Vector3 rideDir = Line.Instance.GetLastLineElement().GetRideDirection();

            Vector3 rideDirNormal = Vector3.Cross(rideDir, Vector3.up);

            float currentWidth = Line.Instance.GetLastLineElement().GetBottomWidth();
            Debug.Log("Current width: " + currentWidth);

            Vector3 leftStartCorner = endPoint - 0.5f * currentWidth * rideDirNormal;

            float heightmapSpacing = TerrainManager.GetHeightmapSpacing(TerrainManager.GetTerrainForPosition(currentSlopeStart));

            float distanceToModify = Vector3.Distance(currentSlopeStart, endPoint);

            int widthSteps = Mathf.CeilToInt(currentWidth / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(distanceToModify / heightmapSpacing);

            Vector3 rightStartCorner = leftStartCorner + widthSteps * rideDirNormal;
            Vector3 leftEndCorner = leftStartCorner + lengthSteps * rideDir;
            Vector3 rightEndCorner = leftStartCorner + lengthSteps * rideDir + widthSteps * rideDirNormal;

            Gizmos.DrawCube(leftStartCorner, Vector3.one);
            Gizmos.DrawCube(rightStartCorner, Vector3.one);
            Gizmos.DrawCube(leftEndCorner, Vector3.one);
            Gizmos.DrawCube(rightEndCorner, Vector3.one);
        }
    }
}