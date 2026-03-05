using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System;
using LineSystem;
using Managers;
using Misc;
using Obstacles;
using TerrainEditing.Slope;

namespace TerrainEditing
{
    
    public class TerrainManager : Singleton<TerrainManager>, ISaveable<TerrainManagerData>
    {
        public GameObject slopeBuilderPrefab;
        
        public static float maxHeight;

        private float terrainTileSize;

        private int heightmapResolution;

        public float GlobalHeightLevel { get; private set; } = 0f;

        
        
        /// <summary>
        /// Maps coordinates (multiples of <see cref="terrainTileSize"/>) to corresponding terrains.
        /// </summary>
        public Dictionary<(int, int), Terrain> Terrains { get; private set; }
        
        /// <summary>
        /// For each terrain, maps each position on the heightmap to a <see cref="CoordinateStateHolder"/>
        /// that tells if it has something built over it or not
        /// </summary>
        public readonly Dictionary<Terrain, CoordinateStateHolder[,]> TerrainStateMap = new();

        /// <summary>
        /// Contains finished <see cref="SlopeChange"/> instances.
        /// </summary>
        public readonly List<SlopeChange> slopeChanges = new();

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
        
        private void PopulateTerrainGrid()
        {
            Terrains.Clear();
            foreach (var terrain in Terrain.activeTerrains)
            {
                int gridX = Mathf.RoundToInt(terrain.transform.position.x / terrainTileSize);
                int gridZ = Mathf.RoundToInt(terrain.transform.position.z / terrainTileSize);
                Terrains[(gridX, gridZ)] = terrain;
            }
        }

        private void InitTerrainStateMap()
        {
            int hmr = Terrain.activeTerrain.terrainData.heightmapResolution;
            
            // we can reuse the same instance since free coordinates don't need to store any unique information.
            var freeState = new FreeCoordinateState(); 
            
            foreach (var terrain in Terrain.activeTerrains)
            {
                TerrainStateMap[terrain] = new CoordinateStateHolder[hmr, hmr];
                
                for (int i = 0; i < hmr; i++)
                {
                    for (int j = 0; j < hmr; j++)
                    {
                        TerrainStateMap[terrain][i, j] = freeState;
                    }
                }
            }
        }

        private void Awake()
        {
            maxHeight = Terrain.activeTerrain.terrainData.heightmapScale.y/2; // the terrain default height is set to half of its size, so max height is half of the size

            terrainTileSize = Terrain.activeTerrain.terrainData.size.x;

            heightmapResolution = Terrain.activeTerrain.terrainData.heightmapResolution;
            
            PopulateTerrainGrid();
            InitTerrainStateMap();
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

        public Terrain GetTerrainForWorldPosition(Vector3 worldPosition)
        {
            float tileScaledX = worldPosition.x / terrainTileSize;
            float tileScaledZ = worldPosition.z / terrainTileSize;
            
            int tileX = Mathf.FloorToInt(tileScaledX);
            int tileZ = Mathf.FloorToInt(tileScaledZ);
            
            if (Terrains.TryGetValue((tileX, tileZ), out Terrain terrain))
            {
                return terrain;
            }

            Debug.Log($"No terrain found for world position {worldPosition}. Expected tile coordinates: ({tileX}, {tileZ}).");
            return null;
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

            foreach (var terrain in Terrain.activeTerrains)
            {
                float[,] heights = terrain.terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
                CoordinateStateHolder[,] stateMap = TerrainStateMap[terrain];
                
                for (int i = 0; i < heightmapResolution; i++)
                {
                    for (int j = 0; j < heightmapResolution; j++)
                    {                        
                        if (stateMap[i, j].GetState() == CoordinateState.Free)
                        {
                            heights[i, j] = heightMapValue;
                        }                        
                    }
                }
                terrain.terrainData.SetHeightsDelayLOD(0, 0, heights);    
            }
               
            
            GlobalHeightLevel = height;
        }

        public SlopePositioner StartSlopeBuild()
        {
            ILineElement lastLineElement = Line.Instance.GetLastLineElement();

            GameObject builder = Instantiate(slopeBuilderPrefab, lastLineElement.GetEndPoint() + lastLineElement.GetRideDirection().normalized, SlopePositioner.GetRotationForDirection(lastLineElement.GetRideDirection()));
            builder.transform.SetParent(transform);

            ActiveSlope = builder.GetComponent<SlopeChange>();

            slopeChanges.Add(ActiveSlope);

            StudioUIManager.Instance.GetSidebar().DeleteSlopeButtonEnabled = true;
            StudioUIManager.Instance.GetSidebar().DeleteButtonEnabled = false;

            return builder.GetComponent<SlopePositioner>();
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
        /// <param name="terrain">The terrain which the coord refers to.y</param>
        /// <param name="coord">Coordinate in unbounded heightmap space.</param>
        public CoordinateStateHolder GetStateHolder(Terrain terrain, int2 coord)
        {
            // Ensure coordinates are within bounds
            if (coord.x >= 0 && coord.x < heightmapResolution && coord.y >= 0 && coord.y < heightmapResolution)
            {
                if (TerrainStateMap.TryGetValue(terrain, out CoordinateStateHolder[,] stateMap))
                {
                    return stateMap[coord.y, coord.x];
                }
                else
                {
                    throw new ArgumentException("Terrain not found in TerrainStateMap.", nameof(terrain));
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(coord), "Coordinate is out of bounds of the terrain heightmap resolution.");
            }
        }

        /// <summary>
        /// Calculates which Terrain tiles are touched by the path and splits the coordinates accordingly.
        /// </summary>
        public HeightmapCoordinates GetCoordinatesForArea(Vector3 start, Vector3 end, float width)
        {
            var data = new Dictionary<Terrain, HashSet<int2>>();
            
            float spacing = GetHeightmapSpacing();
            int widthSteps = Mathf.CeilToInt(width / spacing);
            int lengthSteps = Mathf.CeilToInt(Vector3.Distance(start, end) / spacing);

            Vector3 direction = (end - start).normalized;
            Vector3 directionNormal = Vector3.Cross(direction, Vector3.up).normalized;
            Vector3 leftStartCorner = start - 0.5f * width * directionNormal;

            Terrain currentTerrain = null;
            (int, int) lastGridKey = (int.MinValue, int.MinValue);

            for (int i = 0; i <= lengthSteps; i++)
            {
                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 worldPos = leftStartCorner + j * spacing * directionNormal + i * spacing * direction;

                    // Grid Lookup Optimization
                    int gridX = Mathf.FloorToInt(worldPos.x / terrainTileSize);
                    int gridZ = Mathf.FloorToInt(worldPos.z / terrainTileSize);

                    if (gridX != lastGridKey.Item1 || gridZ != lastGridKey.Item2)
                    {
                        lastGridKey = (gridX, gridZ);
                        Terrains.TryGetValue(lastGridKey, out currentTerrain);
                    }

                    if (currentTerrain != null)
                    {
                        if (!data.TryGetValue(currentTerrain, out var set))
                        {
                            set = new HashSet<int2>();
                            data[currentTerrain] = set;
                        }
                        set.Add(WorldToLocalHeightmapCoordinates(currentTerrain, worldPos));
                    }
                }
            }

            return new HeightmapCoordinates(data);
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

        public float GetHeightAt(Terrain terrain, int2 coord)
        {
            // Ensure coordinates are within bounds
            if (coord.x >= 0 && coord.x < heightmapResolution &&
                coord.y >= 0 && coord.y < heightmapResolution)
            {
                return HeightmapUnitsToWorldUnits(terrain.terrainData.GetHeight(coord.x, coord.y));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(coord), "Coordinate is out of bounds of the terrain heightmap resolution.");
            }
        }

        public void MarkTerrainAs(CoordinateStateHolder state, Terrain terrain, IEnumerable<int2> coordinates)
        {
            foreach (var coord in coordinates)
            {
                // Ensure coordinates are within bounds
                if (coord.x >= 0 && coord.x < heightmapResolution &&
                    coord.y >= 0 && coord.y < heightmapResolution)
                {
                    TerrainStateMap[terrain][coord.y, coord.x] = state;
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
            int heightmapResolution = heightmapResolution;

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
            int heightmapResolution = heightmapResolution;

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

            return Mathf.Min(spacingX, spacingZ)/2; // divide to make sure that no heightmap points are missed
        }

        public HeightmapCoordinates DrawRamp(Vector3 start, Vector3 end, float heightDiff, float width, float startHeight)
        {
            return ModifyTerrainPath(start, end, width, startHeight, startHeight + heightDiff);
        }

        public HeightmapCoordinates DrawFlat(Vector3 start, Vector3 end, float height, float width)
        {
            return ModifyTerrainPath(start, end, width, height, height);
        }

        private HeightmapCoordinates ModifyTerrainPath(Vector3 start, Vector3 end, float width, float startHeight, float endHeight)
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

            float heightmapSpacing = GetHeightmapSpacing();
            int widthSteps = Mathf.CeilToInt(width / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(distanceToModify / heightmapSpacing);

            int2 leftSCorner = WorldToHeightmapCoordinates(leftStartCorner);
            // Calculate other corners to find bounding box
            int2 rightSCorner = WorldToHeightmapCoordinates(leftStartCorner + widthSteps * rideDirNormal);
            int2 leftECorner = WorldToHeightmapCoordinates(leftStartCorner + lengthSteps * rideDir);
            int2 rightECorner = WorldToHeightmapCoordinates(leftStartCorner + lengthSteps * rideDir + widthSteps * rideDirNormal);

            int minX = Mathf.Min(leftSCorner.x, rightSCorner.x, leftECorner.x, rightECorner.x);
            int maxX = Mathf.Max(leftSCorner.x, rightSCorner.x, leftECorner.x, rightECorner.x);
            int minY = Mathf.Min(leftSCorner.y, rightSCorner.y, leftECorner.y, rightECorner.y);
            int maxY = Mathf.Max(leftSCorner.y, rightSCorner.y, leftECorner.y, rightECorner.y);
            int hMapWidth = maxX - minX + 1;
            int hMapHeight = maxY - minY + 1;

            HashSet<int2> coordinates = new();

            float[,] heights = Floor.terrainData.GetHeights(minX, minY, hMapWidth, hMapHeight);

            // Check bounds for both start and end height
            if (CheckHeightBounds(ref startHeight) || CheckHeightBounds(ref endHeight))
            {
                // Message is handled inside CheckHeightBounds
            }

            for (int i = 0; i <= lengthSteps; i++)
            {
                float t = lengthSteps > 0 ? (float)i / lengthSteps : 0f;
                float heightAtLength = Mathf.Lerp(startHeight, endHeight, t); // world units
                float heightAtLengthMap = WorldUnitsToHeightmapUnits(heightAtLength); // heightmap units

                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 position = leftStartCorner + j * heightmapSpacing * rideDirNormal + i * heightmapSpacing * rideDir;
                    int2 heightmapPosition = WorldToHeightmapCoordinates(position);

                    // Check occupancy
                    if (GetStateHolder(heightmapPosition) is OccupiedCoordinateState)
                    {
                        continue;
                    }

                    coordinates.Add(heightmapPosition);

                    int x = heightmapPosition.x - minX;
                    int y = heightmapPosition.y - minY;

                    // Safety check strictly for array bounds
                    if (x >= 0 && x < hMapWidth && y >= 0 && y < hMapHeight)
                    {
                        heights[y, x] = heightAtLengthMap;
                    }
                }
            }

            Floor.terrainData.SetHeightsDelayLOD(minX, minY, heights);

            return new HeightmapCoordinates(coordinates);
        }

        private bool CheckHeightBounds(ref float height)
        {
            if (height < -maxHeight || height > maxHeight)
            {
                StudioUIManager.Instance.ShowMessage($"Height value {height}m is out of bounds [{-maxHeight}, {maxHeight}]. Clamping.",
                    5f, MessagePriority.High);
                height = Mathf.Clamp(height, -maxHeight, maxHeight);
                return true;
            }
            return false;
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
            InitTerrainStateMap();

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