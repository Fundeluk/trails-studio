﻿using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using Assets.Scripts.Utilities;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.Builders;
using UnityEngine.Rendering.Universal;
using System;

namespace Assets.Scripts.Managers
{
    public enum CoordinateState
    {
        Free = 0, // Free to build on
        HeightSet, // Height has been set, but nothing is built on it yet
        Occupied // Occupied by an object
    }

    public abstract class CoordinateStateHolder
    {        
        public abstract CoordinateState GetState();
    }

    public class FreeCoordinateState : CoordinateStateHolder
    {
        public override CoordinateState GetState()
        {
            return CoordinateState.Free;
        }
    }

    public class HeightSetCoordinateState : CoordinateStateHolder
    {
        public override CoordinateState GetState()
        {
            return CoordinateState.HeightSet;
        }

    }
    public class OccupiedCoordinateState : CoordinateStateHolder
    {
        public ILineElement OccupyingElement { get; private set; }
        public OccupiedCoordinateState(ILineElement occupyingElement)
        {
            OccupyingElement = occupyingElement;
        }
        public override CoordinateState GetState()
        {
            return CoordinateState.Occupied;
        }
    }

    /// <summary>
    /// Class that contains the coordinates of a heightmap in a terrain. Optimized for writing to heightmap.
    /// </summary>
    public class HeightmapCoordinates : IEnumerable<int2>
    {
        public int startX;
        public int startY;
        /// <summary>
        /// X axis
        /// </summary>
        public int arrayWidth;
        /// <summary>
        /// Z axis
        /// </summary>
        public int arrayHeight;

        /// <summary>
        /// Coordinates in unbounded (zero-based) heightmap space.
        /// </summary>
        public HashSet<int2> coordinates;

        public IEnumerator<int2> GetEnumerator()
        {
            return ((IEnumerable<int2>)coordinates).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)coordinates).GetEnumerator();
        }

        public int2 TranslateToUnboundedCoordinates(int2 coord)
        {
            return new int2(coord.x + startX, coord.y + startY);
        }

        public HeightmapCoordinates()
        {
            startX = int.MaxValue;
            startY = int.MaxValue;
            arrayWidth = 0;
            arrayHeight = 0;
            coordinates = new();
        }

        public HeightmapCoordinates(Vector3 start, Vector3 end, float width)
        {
            coordinates = new();

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            float heightmapSpacing = TerrainManager.GetHeightmapSpacing();
            int widthSteps = Mathf.CeilToInt(width / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(Vector3.Distance(start, end) / heightmapSpacing);

            Vector3 direction = (end - start).normalized;
            Vector3 directionNormal = Vector3.Cross(direction, Vector3.up).normalized;

            Vector3 leftStartCorner = start - 0.5f * width * directionNormal;

            for (int i = 0; i <= lengthSteps; i++)
            {
                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 position = leftStartCorner + j * heightmapSpacing * directionNormal + i * heightmapSpacing * direction;
                    int2 heightmapPosition = TerrainManager.WorldToHeightmapCoordinates(position);

                    // Update min and max coordinates
                    minX = Mathf.Min(minX, heightmapPosition.x);
                    minY = Mathf.Min(minY, heightmapPosition.y);
                    maxX = Mathf.Max(maxX, heightmapPosition.x);
                    maxY = Mathf.Max(maxY, heightmapPosition.y);

                    coordinates.Add(heightmapPosition);
                }
            }

            startX = minX;
            startY = minY;
            this.arrayWidth = maxX - minX + 1;
            this.arrayHeight = maxY - minY + 1;
        }

        public HeightmapCoordinates(IEnumerable<int2> coords)
        {
            coordinates = new();

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (var coord in coords)
            {
                minX = Mathf.Min(minX, coord.x);
                minY = Mathf.Min(minY, coord.y);
                maxX = Mathf.Max(maxX, coord.x);
                maxY = Mathf.Max(maxY, coord.y);
                coordinates.Add(coord);
            }

            startX = minX;
            startY = minY;
            arrayWidth = maxX - minX + 1;
            arrayHeight = maxY - minY + 1;
        }

        public HeightmapCoordinates(int startX, int startY, int width, int height, IEnumerable<int2> coords)
        {
            coordinates = new HashSet<int2>();

            foreach (int2 coord in coords)
            {
                coordinates.Add(coord);
            }

            this.startX = startX;
            this.startY = startY;
            this.arrayWidth = width;
            this.arrayHeight = height;            
        }        

        public HeightmapCoordinates(HeightmapCoordinates toClone)
        {
            coordinates = new(toClone.coordinates);
            startX = toClone.startX;
            startY = toClone.startY;
            arrayWidth = toClone.arrayWidth;
            arrayHeight = toClone.arrayHeight;
        }

        public void Add(HeightmapCoordinates other)
        {
            if (other == null || other.coordinates.Count == 0 || other.arrayHeight == 0 || other.arrayWidth == 0)
            {
                return;
            }           

            if (coordinates.Count == 0 || arrayHeight == 0 || arrayWidth == 0)
            {
                startX = other.startX;
                startY = other.startY;
                arrayWidth = other.arrayWidth;
                arrayHeight = other.arrayHeight;
                coordinates = new HashSet<int2>(other.coordinates);
                return;
            }

            // Add all coordinates from the other instance
            coordinates.UnionWith(other.coordinates);

            // Calculate the new startX and startY (smallest of the mins)
            int newStartX = Mathf.Min(startX, other.startX);
            int newStartY = Mathf.Min(startY, other.startY);

            // Calculate the new maxX and maxY (largest of the maxes)
            int thisMaxX = startX + arrayWidth - 1;
            int thisMaxY = startY + arrayHeight - 1;
            int otherMaxX = other.startX + other.arrayWidth - 1;
            int otherMaxY = other.startY + other.arrayHeight - 1;

            int newMaxX = Mathf.Max(thisMaxX, otherMaxX);
            int newMaxY = Mathf.Max(thisMaxY, otherMaxY);

            // Update the fields
            startX = newStartX;
            startY = newStartY;
            arrayWidth = newMaxX - newStartX + 1;
            arrayHeight = newMaxY - newStartY + 1;
        }

        /// <summary>
        /// Adds a coordinate to the heightmap coordinates and updates the startX, startY, width, and height accordingly.
        /// </summary>
        /// <param name="coordinate">Coordinate in unbounded heightmap space.</param>
        public void Add(int2 coordinate)
        {
            coordinates.Add(coordinate);
            startX = Mathf.Min(startX, coordinate.x);
            startY = Mathf.Min(startY, coordinate.y);
            int maxX = Mathf.Max(startX + arrayWidth - 1, coordinate.x);
            int maxY = Mathf.Max(startY + arrayHeight - 1, coordinate.y);
            arrayWidth = maxX - startX + 1;
            arrayHeight = maxY - startY + 1;
        }

        public void MarkAs(CoordinateStateHolder state) => TerrainManager.Instance.MarkTerrainAs(state, coordinates);

        /// <summary>
        /// Sets the height of the coordinates to height in world space.
        /// </summary>
        /// <param name="height">Height to set in world space.</param>
        public void SetHeight(float height)
        {
            if (coordinates.Count == 0 || this.arrayHeight == 0 || arrayWidth == 0)
            {
                return;
            }

            if (height < -TerrainManager.maxHeight || height > TerrainManager.maxHeight)
            {
                StudioUIManager.Instance.ShowMessage($"Trying to set height that is out of bounds: {height}m. It must be between {-TerrainManager.maxHeight}m and {TerrainManager.maxHeight}m. Clamping it to the closest allowed value..",
                    5f, MessagePriority.High);

                height = Mathf.Clamp(height, -TerrainManager.maxHeight, TerrainManager.maxHeight);
            }

            float heightmapValue = TerrainManager.WorldUnitsToHeightmapUnits(height);
            float[,] heights = TerrainManager.Floor.terrainData.GetHeights(startX, startY, arrayWidth, this.arrayHeight);

            foreach (var coord in coordinates)
            {
                int x = coord.x - startX;
                int y = coord.y - startY;
                heights[y, x] = heightmapValue;                
            }

            TerrainManager.Floor.terrainData.SetHeightsDelayLOD(startX, startY, heights);
        }
        

        /// <summary>
        /// For debugging purposes. Raises the corners of the terrain by 0.5f to show the area occupied by the coordinates.
        /// </summary>
        public void RaiseCorners()
        {
            float[,] heights = TerrainManager.Floor.terrainData.GetHeights(startX, startY, arrayWidth, this.arrayHeight);

            // Raise the corners
            heights[0, 0] += 0.5f;
            heights[0, arrayWidth - 1] += 0.5f;
            heights[arrayHeight - 1, 0] += 0.5f;
            heights[arrayHeight - 1, arrayWidth - 1] += 0.5f;

            TerrainManager.Floor.terrainData.SetHeights(startX, startY, heights);
        }
    }    

    public class TerrainManager : Singleton<TerrainManager>, ISaveable<TerrainManagerData>
    {
        public GameObject slopeBuilderPrefab;

        public static float maxHeight;

        public float GlobalHeightLevel { get; private set; } = 0f;

        public static Terrain Floor { get; private set; }

        /// <summary>
        /// For each terrain, maps each position on the heightmap to a boolean value that tells if it has something built over it or not
        /// </summary>
        public CoordinateStateHolder[,] UntouchedTerrainMap { get; private set; }

        /// <summary>
        /// Contains finished <see cref="SlopeChange"/> instances.
        /// </summary>
        public List<SlopeChange> slopeChanges = new();

        private SlopeChange _activeSlope = null;
        public SlopeChange ActiveSlope
        {
            get => _activeSlope;
            set
            {
                _activeSlope = value;
                if (_activeSlope == null)
                {
                    StudioUIManager.Instance.GetSidebar().SlopeButtonEnabled = true;
                    StudioUIManager.Instance.GetSidebar().DeleteSlopeButtonEnabled = false;

                    if (Line.Instance.Count > 1)
                    {
                        StudioUIManager.Instance.GetSidebar().DeleteButtonEnabled = true;
                    }
                }
                else
                {
                    StudioUIManager.Instance.GetSidebar().SlopeButtonEnabled = false;
                }
            }
        }

        private void InitUntouchedTerrainMap()
        {
            UntouchedTerrainMap = new CoordinateStateHolder[Floor.terrainData.heightmapResolution, Floor.terrainData.heightmapResolution];
            for (int i = 0; i < Floor.terrainData.heightmapResolution; i++)
            {
                for (int j = 0; j < Floor.terrainData.heightmapResolution; j++)
                {
                    UntouchedTerrainMap[i, j] = new FreeCoordinateState();
                }
            }
        }

        private void Awake()
        {
            Floor = Terrain.activeTerrain;            
            maxHeight = Floor.terrainData.size.y/2; // the terrain default height is set to half of its size, so max height is half of the size

            InitUntouchedTerrainMap();
        }

        //void Start()
        //{
        //    RollIn rollIn = Line.Instance.GetRollIn();
        //    if (rollIn != null)
        //    {
        //        rollIn.GetObstacleHeightmapCoordinates().MarkAs(new OccupiedCoordinateState(rollIn));
        //    }
        //}

        public void ShowSlopeInfo()
        {
            foreach (SlopeChange slope in slopeChanges)
            {
                slope.ShowInfo();
            }
        }

        public void HideSlopeInfo()
        {
            foreach (SlopeChange slope in slopeChanges)
            {
                slope.HideInfo();
            }
        }


        /// <summary>
        /// For all active terrains, sets the terrain (apart from occupied positions) to a given Height.
        /// </summary>
        /// <remarks><b>Expensive</b>, do not call frequently!</remarks>
        /// <param name="height">The terrain Height to set</param>
        public void SetHeight(float height)
        {            
            if (height < -maxHeight || height > maxHeight)
            {
                StudioUIManager.Instance.ShowMessage($"Trying to set height that is out of bounds: {height}m. It must be between {-maxHeight}m and {maxHeight}m. Clamping it to the closest allowed value..",
                    5f, MessagePriority.High);

                height = Mathf.Clamp(height, -maxHeight, maxHeight);
            }

            float heightMapValue = WorldUnitsToHeightmapUnits(height);

            float[,] heights = Floor.terrainData.GetHeights(0, 0, Floor.terrainData.heightmapResolution, Floor.terrainData.heightmapResolution);
            for (int i = 0; i < Floor.terrainData.heightmapResolution; i++)
            {
                for (int j = 0; j < Floor.terrainData.heightmapResolution; j++)
                {                        
                    if (UntouchedTerrainMap[i, j].GetState() == CoordinateState.Free)
                    {
                        heights[i, j] = heightMapValue;
                    }                        
                }
            }
            Floor.terrainData.SetHeightsDelayLOD(0, 0, heights);   
            
            GlobalHeightLevel = height;
        }

        public SlopePositioner StartSlopeBuild()
        {
            ILineElement lastLineElement = Line.Instance.GetLastLineElement();

            GameObject builder = Instantiate(slopeBuilderPrefab, lastLineElement.GetEndPoint() + lastLineElement.GetRideDirection().normalized, SlopePositioner.GetRotationForDirection(lastLineElement.GetRideDirection()));
            builder.transform.SetParent(transform);

            ActiveSlope = builder.GetComponent<SlopeChange>();

            AddSlope(ActiveSlope);

            StudioUIManager.Instance.GetSidebar().DeleteSlopeButtonEnabled = true;
            StudioUIManager.Instance.GetSidebar().DeleteButtonEnabled = false;

            return builder.GetComponent<SlopePositioner>();
        }

        public void AddSlope(SlopeChange slope)
        {
            slopeChanges.Add(slope);
        }

        public void RemoveSlope(SlopeChange slope)
        {
            if (slopeChanges.Contains(slope))
            {
                slopeChanges.Remove(slope);
            }
            else
            {
                Debug.LogWarning("Trying to remove a slope that is not in the list of slopes.");
            }

            if (ActiveSlope == slope)
            {
                ActiveSlope = null;
            }
            else
            {
                Debug.LogWarning("Trying to remove an inactive slope.");
            }            
        }

        /// <summary>
        /// Evaluates whether a given coordinate is occupied by an object or terrain change built on the terrain.
        /// </summary>
        /// <param name="coord">Coordinate in unbounded heightmap space.</param>
        public CoordinateStateHolder GetStateHolder(int2 coord)
        {
            // Ensure coordinates are within bounds
            if (coord.x >= 0 && coord.x < Floor.terrainData.heightmapResolution &&
                coord.y >= 0 && coord.y < Floor.terrainData.heightmapResolution)
            {
                return UntouchedTerrainMap[coord.y, coord.x];
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(coord), "Coordinate is out of bounds of the terrain heightmap resolution.");
            }
        }

        /// <summary>
        /// Checks if an area from start to end of some width is unoccupied.
        /// </summary>        
        /// <returns></returns>
        public bool IsAreaFree(Vector3 start, Vector3 end, float width, ILineElement allowedElement = null)
        {
            Vector3 rightDir = Vector3.Cross(Vector3.ProjectOnPlane(end - start, Vector3.up).normalized, Vector3.up).normalized;

            Vector3 leftStartCorner = start - 0.5f * width * rightDir;
            Vector3 rightStartCorner = start + 0.5f * width * rightDir;
            Vector3 leftEndCorner = end - 0.5f * width * rightDir;
            Vector3 rightEndCorner = end + 0.5f * width * rightDir;

            if (!IsPositionOnTerrain(leftStartCorner) || 
                !IsPositionOnTerrain(rightStartCorner) ||
                !IsPositionOnTerrain(leftEndCorner) ||
                !IsPositionOnTerrain(rightEndCorner))
            {
                return false; // If any corner is not on the terrain, the area is not free
            }

            HeightmapCoordinates coords = new(start, end, width);
            foreach (var coord in coords)
            {
                CoordinateStateHolder stateHolder = GetStateHolder(coord);
                CoordinateState state = stateHolder.GetState();
                if (stateHolder is OccupiedCoordinateState occupiedState)
                {                    
                    if (allowedElement != null && occupiedState.OccupyingElement == allowedElement)
                    {
                        continue; // Allowed element occupies this coordinate
                    }
                    
                    return false;
                }
                else if (state == CoordinateState.HeightSet)
                {                    
                    // If the height is set, we cannot build here
                    return false;
                }
            }
            return true;
        }


        

        public float GetRideableDistance(Vector3 start, Vector3 direction, float width, float height, float maxDistance)
        {
            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;

            // First check if path would leave terrain boundaries
            float boundaryDistance = GetDistanceToTerrainBoundary(start, direction);

            // Use the minimum of maxDistance and boundaryDistance
            float effectiveMaxDistance = Mathf.Min(maxDistance, boundaryDistance);

            // If we're already at or beyond the boundary, return 0
            if (effectiveMaxDistance <= 0)
                return 0;

            if (effectiveMaxDistance != maxDistance)
                effectiveMaxDistance -= 0.2f; // Avoid overshooting the boundary

            foreach (int2 coord in GetHeightmapCoordinatesForPath(start, start + direction * effectiveMaxDistance, width))
            {
                CoordinateStateHolder state = GetStateHolder(coord);
                if (state.GetState() == CoordinateState.Occupied || (state.GetState() == CoordinateState.HeightSet && GetHeightAt(coord) != height))
                {
                    return Vector3.Distance(start, Vector3.Project(HeightmapToWorldCoordinates(coord), direction));
                }
            }

            return maxDistance;
        }

        public static float GetHeightAt(int2 coord)
        {
            // Ensure coordinates are within bounds
            if (coord.x >= 0 && coord.x < Floor.terrainData.heightmapResolution &&
                coord.y >= 0 && coord.y < Floor.terrainData.heightmapResolution)
            {
                return HeightmapUnitsToWorldUnits(Floor.terrainData.GetHeight(coord.x, coord.y));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(coord), "Coordinate is out of bounds of the terrain heightmap resolution.");
            }
        }

        public void UnmarkTerrain(IEnumerable<int2> coordinates)
        {
            foreach (var coord in coordinates)
            {
                // Ensure coordinates are within bounds
                if (coord.x >= 0 && coord.x < Floor.terrainData.heightmapResolution &&
                    coord.y >= 0 && coord.y < Floor.terrainData.heightmapResolution)
                {
                    UntouchedTerrainMap[coord.y, coord.x] = new FreeCoordinateState();
                }
            }
        }

        public void MarkTerrainAs(CoordinateStateHolder state, IEnumerable<int2> coordinates)
        {
            foreach (var coord in coordinates)
            {
                // Ensure coordinates are within bounds
                if (coord.x >= 0 && coord.x < Floor.terrainData.heightmapResolution &&
                    coord.y >= 0 && coord.y < Floor.terrainData.heightmapResolution)
                {
                    UntouchedTerrainMap[coord.y, coord.x] = state;
                }
            }            
        }
        
        public static void FitObstacleOnFlat(IObstacleBuilder obstacle)
        {
            obstacle.GetTransform().forward = Vector3.ProjectOnPlane(obstacle.GetRideDirection(), Vector3.up).normalized;
            float newHeight = GetHeightAt(obstacle.GetTransform().position);
            obstacle.GetTransform().position = new Vector3(obstacle.GetTransform().position.x, newHeight, obstacle.GetTransform().position.z);
        }

        /// <summary>
        /// Gets the heightmap coordinates for a path with a set width between two points.
        /// </summary>
        /// <remarks><b>Iterates through the path from the left corner to the right, row-by-row from Start to end.</b></remarks>
        /// <param name="start">Start of the path.</param>
        /// <param name="end">End of the path.</param>
        /// <param name="width">Width of the path.</param>
        public static IEnumerable<int2> GetHeightmapCoordinatesForPath(Vector3 start, Vector3 end, float width)
        {
            float heightmapSpacing = GetHeightmapSpacing();
            int widthSteps = Mathf.CeilToInt(width / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(Vector3.Distance(start, end) / heightmapSpacing);

            Vector3 direction = (end - start).normalized;
            Vector3 directionNormal = Vector3.Cross(direction, Vector3.up).normalized;

            Vector3 leftStartCorner = start - 0.5f * width * directionNormal;
           
            for (int i = 0; i <= lengthSteps; i++)
            {
                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 position = leftStartCorner + j * heightmapSpacing * directionNormal + i * heightmapSpacing * direction;
                    int2 heightmapPosition = WorldToHeightmapCoordinates(position);
                    yield return heightmapPosition;
                }
            }
        }
        
        public static int2 WorldToHeightmapCoordinates(Vector3 worldPosition)
        {
            Vector3 terrainPosition = Floor.transform.position;
            Vector3 terrainSize = Floor.terrainData.size;
            int heightmapResolution = Floor.terrainData.heightmapResolution;

            // Calculate normalized positions
            float normalizedX = (worldPosition.x - terrainPosition.x) / terrainSize.x;
            float normalizedZ = (worldPosition.z - terrainPosition.z) / terrainSize.z;

            // Convert to heightmap coordinates
            int x = Mathf.Clamp(Mathf.FloorToInt(normalizedX * (heightmapResolution - 1)), 0, heightmapResolution - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt(normalizedZ * (heightmapResolution - 1)), 0, heightmapResolution - 1);
            return new int2(x, z);
        }

        public static Vector3 HeightmapToWorldCoordinates(int2 coord)
        {
            Vector3 terrainPosition = Floor.transform.position;
            Vector3 terrainSize = Floor.terrainData.size;
            int heightmapResolution = Floor.terrainData.heightmapResolution;

            // Calculate normalized positions
            float normalizedX = (float)coord.x / (heightmapResolution - 1);
            float normalizedZ = (float)coord.y / (heightmapResolution - 1);

            // Convert to world coordinates
            float worldX = terrainPosition.x + normalizedX * terrainSize.x;
            float worldZ = terrainPosition.z + normalizedZ * terrainSize.z;
            return new Vector3(worldX, 0, worldZ);
        }

        public static Vector3 GetNormalForWorldPosition(Vector3 worldPosition)
        {
            TerrainCollider terrainCollider = Floor.GetComponent<TerrainCollider>();
            if (terrainCollider.Raycast(new Ray(worldPosition + Vector3.up * 50, Vector3.down), out RaycastHit hit, Mathf.Infinity))
            {
                return hit.normal.normalized;
            }

            return Vector3.up; // Fallback if raycast fails
        }

        /// <summary>
        /// Translates from a Height in world units to a Height in heightmap units.
        /// </summary>
        public static float WorldUnitsToHeightmapUnits(float worldUnits)
        {
            return (worldUnits - Floor.transform.position.y) / Floor.terrainData.size.y;
        }

        public static float HeightmapUnitsToWorldUnits(float heightmapUnits)
        {
            return heightmapUnits * Floor.terrainData.size.y + Floor.transform.position.y;
        }

        /// <summary>
        /// Gets the spacing in world units between heightmap points on a terrain.
        /// </summary>
        /// <returns>The smaller of the two spacings in the X and Z directions.</returns>
        public static float GetHeightmapSpacing()
        {
            TerrainData terrainData = Floor.terrainData;
            Vector3 terrainSize = terrainData.size;
            int heightmapResolution = terrainData.heightmapResolution;

            float spacingX = terrainSize.x / (heightmapResolution - 1);
            float spacingZ = terrainSize.z / (heightmapResolution - 1);

            return Mathf.Min(spacingX, spacingZ)/5; // divide to make sure that no heightmap points are missed
        }

        /// <summary>
        /// Gets the world space terrain height at a given world space position.
        /// </summary>        
        public static float GetHeightAt(Vector3 position)
        {
            float height = Floor.SampleHeight(position) + Floor.transform.position.y;
            return height;
        }

        /// <summary>
        /// Calculates the distance from a point to the terrain boundary in a given direction.
        /// </summary>
        /// <param name="start">Starting point</param>
        /// <param name="direction">Direction vector (should be normalized)</param>
        /// <returns>Distance to terrain boundary, or float.MaxValue if no boundary is hit</returns>
        public float GetDistanceToTerrainBoundary(Vector3 start, Vector3 direction)
        {
            Vector3 terrainPosition = Floor.transform.position;
            Vector3 terrainSize = Floor.terrainData.size;

            // If starting position is already outside terrain, return 0
            if (!IsPositionOnTerrain(start))
                return 0f;

            // Calculate distances to each boundary plane
            float distanceToMaxX = float.MaxValue;
            float distanceToMinX = float.MaxValue;
            float distanceToMaxZ = float.MaxValue;
            float distanceToMinZ = float.MaxValue;

            // X boundaries
            if (Mathf.Abs(direction.x) > 0.0001f)
            {
                if (direction.x > 0)
                    distanceToMaxX = (terrainPosition.x + terrainSize.x - start.x) / direction.x;
                else
                    distanceToMinX = (terrainPosition.x - start.x) / direction.x;
            }

            // Z boundaries
            if (Mathf.Abs(direction.z) > 0.0001f)
            {
                if (direction.z > 0)
                    distanceToMaxZ = (terrainPosition.z + terrainSize.z - start.z) / direction.z;
                else
                    distanceToMinZ = (terrainPosition.z - start.z) / direction.z;
            }

            // Return the smallest positive distance
            float minDistance = float.MaxValue;
            if (distanceToMaxX > 0) minDistance = Mathf.Min(minDistance, distanceToMaxX);
            if (distanceToMinX > 0) minDistance = Mathf.Min(minDistance, distanceToMinX);
            if (distanceToMaxZ > 0) minDistance = Mathf.Min(minDistance, distanceToMaxZ);
            if (distanceToMinZ > 0) minDistance = Mathf.Min(minDistance, distanceToMinZ);

            return minDistance;
        }

        /// <summary>
        /// Checks if a given position is on a specific terrain.
        /// </summary>
        public bool IsPositionOnTerrain(Vector3 position)
        {
            Vector3 terrainPosition = Floor.transform.position;
            Vector3 terrainSize = Floor.terrainData.size;

            return position.x >= terrainPosition.x &&
                   position.x <= terrainPosition.x + terrainSize.x &&
                   position.z >= terrainPosition.z &&
                   position.z <= terrainPosition.z + terrainSize.z;
        }
       
        public static void ConfirmChanges()
        {
            Floor.terrainData.SyncHeightmap();
        }

        public TerrainManagerData GetSerializableData() => new TerrainManagerData(this);

        public void LoadFromData(TerrainManagerData data)
        {
            InitUntouchedTerrainMap();

            GlobalHeightLevel = data.globalHeight;

            slopeChanges.Clear();

            for (int i = 0; i < data.slopes.Count; i++)
            {
                SlopeChange slope = Instantiate(DataManager.Instance.slopeChangePrefab, Vector3.zero, Quaternion.identity).GetComponent<SlopeChange>();
                slope.transform.SetParent(transform);
                slopeChanges.Add(slope);
                slope.LoadFromData(data.slopes[i]);                
            }

            // only after the terrain manager is loaded, we can set the heightmap coordinates of the line elements
            foreach (ILineElement element in Line.Instance)
            {
                HeightmapCoordinates slopeCoords = element.GetUnderlyingSlopeHeightmapCoordinates();
                slopeCoords?.MarkAs(new HeightSetCoordinateState());

                HeightmapCoordinates coords = element.GetObstacleHeightmapCoordinates();
                coords?.MarkAs(new OccupiedCoordinateState(element));
            }

            float[,] heightmap = data.heightmap.ToHeightmap();

            Floor.terrainData.SetHeights(0, 0, heightmap);

        }
    }
}