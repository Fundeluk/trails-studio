using System.Collections;
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
        public int width;
        /// <summary>
        /// Z axis
        /// </summary>
        public int height;

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
            width = 0;
            height = 0;
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
            this.width = maxX - minX + 1;
            this.height = maxY - minY + 1;
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
            width = maxX - minX + 1;
            height = maxY - minY + 1;
        }

        public HeightmapCoordinates(int startX, int startY, int width, int height, IEnumerable<int2> coords)
        {
            coordinates = (HashSet<int2>)coords;
            this.startX = startX;
            this.startY = startY;
            this.width = width;
            this.height = height;            
        }        

        public HeightmapCoordinates(HeightmapCoordinates toClone)
        {
            coordinates = new(toClone.coordinates);
            startX = toClone.startX;
            startY = toClone.startY;
            width = toClone.width;
            height = toClone.height;
        }

        public void Add(HeightmapCoordinates other)
        {
            if (other == null || other.coordinates.Count == 0 || other.height == 0 || other.width == 0)
            {
                return;
            }           

            if (coordinates.Count == 0 || height == 0 || width == 0)
            {
                startX = other.startX;
                startY = other.startY;
                width = other.width;
                height = other.height;
                coordinates = new HashSet<int2>(other.coordinates);
                return;
            }

            // Add all coordinates from the other instance
            coordinates.UnionWith(other.coordinates);

            // Calculate the new startX and startY (smallest of the mins)
            int newStartX = Mathf.Min(startX, other.startX);
            int newStartY = Mathf.Min(startY, other.startY);

            // Calculate the new maxX and maxY (largest of the maxes)
            int thisMaxX = startX + width - 1;
            int thisMaxY = startY + height - 1;
            int otherMaxX = other.startX + other.width - 1;
            int otherMaxY = other.startY + other.height - 1;

            int newMaxX = Mathf.Max(thisMaxX, otherMaxX);
            int newMaxY = Mathf.Max(thisMaxY, otherMaxY);

            // Update the fields
            startX = newStartX;
            startY = newStartY;
            width = newMaxX - newStartX + 1;
            height = newMaxY - newStartY + 1;
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
            int maxX = Mathf.Max(startX + width - 1, coordinate.x);
            int maxY = Mathf.Max(startY + height - 1, coordinate.y);
            width = maxX - startX + 1;
            height = maxY - startY + 1;
        }

        public void MarkAs(CoordinateStateHolder state) => TerrainManager.Instance.MarkTerrainAs(state, coordinates);

        /// <summary>
        /// Sets the height of the coordinates to height in world space.
        /// </summary>
        /// <param name="height">Height to set in world space.</param>
        public void SetHeight(float height)
        {
            if (coordinates.Count == 0 || this.height == 0 || width == 0)
            {
                return;
            }

            float heightmapValue = TerrainManager.WorldUnitsToHeightmapUnits(height);
            float[,] heights = TerrainManager.Floor.terrainData.GetHeights(startX, startY, width, this.height);

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
            float[,] heights = TerrainManager.Floor.terrainData.GetHeights(startX, startY, width, this.height);

            // Raise the corners
            heights[0, 0] += 0.5f;
            heights[0, width - 1] += 0.5f;
            heights[height - 1, 0] += 0.5f;
            heights[height - 1, width - 1] += 0.5f;

            TerrainManager.Floor.terrainData.SetHeights(startX, startY, heights);
        }
    }    

    public class TerrainManager : Singleton<TerrainManager>
    {
        public GameObject slopeBuilderPrefab;

        public static float maxHeight;

        public static Terrain Floor { get; private set; }

        /// <summary>
        /// For each terrain, maps each position on the heightmap to a boolean value that tells if it has something built over it or not
        /// </summary>
        public CoordinateStateHolder[,] untouchedTerrainMap;

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
                    UIManager.Instance.GetSidebar().SlopeButtonEnabled = true;
                    UIManager.Instance.GetDeleteUI().DeleteSlopeButtonEnabled = false;
                }
                else
                {
                    UIManager.Instance.GetSidebar().SlopeButtonEnabled = false;
                }             
            }
        }

        private void Awake()
        {
            Floor = Terrain.activeTerrain;            
            maxHeight = Floor.terrainData.size.y/2;
            untouchedTerrainMap = new CoordinateStateHolder[Floor.terrainData.heightmapResolution, Floor.terrainData.heightmapResolution];
            for (int i = 0; i < Floor.terrainData.heightmapResolution; i++)
            {
                for (int j = 0; j < Floor.terrainData.heightmapResolution; j++)
                {
                    untouchedTerrainMap[i, j] = new FreeCoordinateState();
                }
            }
        }

        void Start()
        {
            ILineElement rollin = Line.Instance.line[0];
            rollin.GetObstacleHeightmapCoordinates().MarkAs(new OccupiedCoordinateState(rollin));
        }

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
            float heightMapValue = WorldUnitsToHeightmapUnits(height);

            float[,] heights = Floor.terrainData.GetHeights(0, 0, Floor.terrainData.heightmapResolution, Floor.terrainData.heightmapResolution);
            for (int i = 0; i < Floor.terrainData.heightmapResolution; i++)
            {
                for (int j = 0; j < Floor.terrainData.heightmapResolution; j++)
                {                        
                    if (untouchedTerrainMap[i, j].GetState() == CoordinateState.Free)
                    {
                        heights[i, j] = heightMapValue;
                    }                        
                }
            }
            Floor.terrainData.SetHeightsDelayLOD(0, 0, heights);            
        }

        public SlopePositioner StartSlopeBuild()
        {
            ILineElement lastLineElement = Line.Instance.GetLastLineElement();

            GameObject builder = Instantiate(slopeBuilderPrefab, lastLineElement.GetEndPoint() + lastLineElement.GetRideDirection().normalized, SlopePositioner.GetRotationForDirection(lastLineElement.GetRideDirection()));
            builder.transform.SetParent(transform);

            ActiveSlope = builder.GetComponent<SlopeChange>();

            return builder.GetComponent<SlopePositioner>();
        }

        public void AddSlope(SlopeChange slope)
        {
            slopeChanges.Add(slope);
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
                return untouchedTerrainMap[coord.y, coord.x];
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(coord), "Coordinate is out of bounds of the terrain heightmap resolution.");
            }
        }

        public CoordinateStateHolder GetStateHolder(Vector3 position)
        {           
            int2 coord = WorldToHeightmapCoordinates(position);
            return GetStateHolder(coord);            
        }

        /// <summary>
        /// Checks if an area from start to end of some width is unoccupied.
        /// </summary>        
        /// <returns></returns>
        public bool IsAreaFree(Vector3 start, Vector3 end, float width, ILineElement allowedElement = null, float? height = null)
        {
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

            foreach (int2 coord in GetHeightmapCoordinatesForPath(start, start + direction * maxDistance, width))
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
                    untouchedTerrainMap[coord.y, coord.x] = new FreeCoordinateState();
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
                    untouchedTerrainMap[coord.y, coord.x] = state;
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
        /// Checks if a given position is on a specific terrain.
        /// </summary>
        private static bool IsPositionOnTerrain(Vector3 position, Terrain terrain)
        {
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;

            return position.x >= terrainPosition.x &&
                   position.x <= terrainPosition.x + terrainSize.x &&
                   position.z >= terrainPosition.z &&
                   position.z <= terrainPosition.z + terrainSize.z;
        }
       
        public static void ConfirmChanges()
        {
            Floor.terrainData.SyncHeightmap();
        }
    }
}