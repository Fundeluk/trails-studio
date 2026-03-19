using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System;
using System.Linq;
using LineSystem;
using Managers;
using Misc;
using Obstacles;
using TerrainEditing.Slope;

namespace TerrainEditing
{
    
    public partial class TerrainManager : Singleton<TerrainManager>, ISaveable<TerrainManagerData>
    {
        /// <summary>
        /// Maps 2D coordinates (multiples of <see cref="terrainTileSize"/>) to corresponding terrains
        /// and their coordinate states (see <see cref="CoordinateState"/>)
        /// </summary>
        public class MultiTerrainMap
        {
            private readonly Dictionary<int2, (Terrain terrain, CoordinateStateHolder[,] coordStates)> terrainStateMap =
                new();
            public readonly float TerrainTileSize = Terrain.activeTerrain.terrainData.size.x;
            public readonly int HeightmapResolution = Terrain.activeTerrain.terrainData.heightmapResolution;

            private void InitFromActiveTerrains()
            {
                // we can reuse the same instance since free coordinates don't need to store any unique information.
                var freeState = new FreeCoordinateState(); 
                
                foreach (var terrain in Terrain.activeTerrains)
                {
                    int2 key = GetIndex(terrain);
                    
                    var coordStates = new CoordinateStateHolder[HeightmapResolution, HeightmapResolution];
                
                    for (int i = 0; i < HeightmapResolution; i++)
                    {
                        for (int j = 0; j < HeightmapResolution; j++)
                        {
                            coordStates[i, j] = freeState;
                        }
                    }
                    
                    terrainStateMap[key] = (terrain, coordStates);
                }
            }

            private Vector3 GetTerrainPositionFromIndex(int2 index)
            {
                float xPos = index.x * TerrainTileSize;
                float zPos = index.y * TerrainTileSize;

                return new(xPos, 0, zPos);
            }

            private void AddTerrainAt(int2 index)
            {
                // Check if terrain already exists
                if (terrainStateMap.TryGetValue(index, out _))
                {
                    Debug.LogWarning($"Terrain already exists at grid {index}");
                    return;
                }

                // Calculate world position
                Vector3 position = GetTerrainPositionFromIndex(index);

                // Instantiate Prefab
                GameObject newTerrainObj = Instantiate(TerrainManager.Instance.terrainTilePrefab, position, Quaternion.identity);
                newTerrainObj.name = $"Terrain_{index.x}_{index.y}";
                newTerrainObj.transform.SetParent(TerrainManager.Instance.transform);

                Terrain newTerrain = newTerrainObj.GetComponent<Terrain>();
                TerrainCollider newCollider = newTerrainObj.GetComponent<TerrainCollider>();

                // CRITICAL: Clone the TerrainData. Otherwise, modifying this terrain will modify the Prefab itself!
                newTerrain.terrainData = Instantiate(newTerrain.terrainData);
            
                // Ensure collider uses the new data
                if (newCollider != null)
                {
                    newCollider.terrainData = newTerrain.terrainData;
                }

                // Register in our map
                AddToMap(index, newTerrain);

                // Connect to neighbors
                ConnectNeighbors(index, newTerrain);
            }
            
            public void EnsureTerrainAt(Vector3 worldPosition)
            {
                int2 index = GetIndex(worldPosition);
                
                if (!terrainStateMap.ContainsKey(index))
                {
                    AddTerrainAt(index);
                }
            }
            
            private void ConnectNeighbors(int2 gridCoords, Terrain currentTerrain)
            {
                // Determine neighbor coordinates
                int2 leftCoords   = gridCoords + new int2(-1, 0);
                int2 rightCoords  = gridCoords + new int2(1, 0);
                int2 topCoords    = gridCoords + new int2(0, 1);  // Unity Z+ is Top
                int2 bottomCoords = gridCoords + new int2(0, -1); // Unity Z- is Bottom

                // Retrieve neighbors (returns null if missing)
                Terrain left   = GetTerrainOrDefault(leftCoords);
                Terrain right  = GetTerrainOrDefault(rightCoords);
                Terrain top    = GetTerrainOrDefault(topCoords);
                Terrain bottom = GetTerrainOrDefault(bottomCoords);

                // 1. Connect the new terrain to its neighbors
                // SetNeighbors args: (left, top, right, bottom)
                currentTerrain.SetNeighbors(left, top, right, bottom);

                // 2. Connect the neighbors back to the new terrain
                // For the LEFT neighbor, the new terrain is on its RIGHT.
                if (left != null)   left.SetNeighbors(left.leftNeighbor, left.topNeighbor, currentTerrain, left.bottomNeighbor);
                
                // For the RIGHT neighbor, the new terrain is on its LEFT.
                if (right != null)  right.SetNeighbors(currentTerrain, right.topNeighbor, right.rightNeighbor, right.bottomNeighbor);
                
                // For the TOP neighbor, the new terrain is on its BOTTOM.
                if (top != null)    top.SetNeighbors(top.leftNeighbor, top.topNeighbor, top.rightNeighbor, currentTerrain);
                
                // For the BOTTOM neighbor, the new terrain is on its TOP.
                if (bottom != null) bottom.SetNeighbors(bottom.leftNeighbor, currentTerrain, bottom.rightNeighbor, bottom.bottomNeighbor);
                
                // Sync just in case to update LOD/stitching visual immediately
                currentTerrain.Flush();
            }

            public MultiTerrainMap()
            {
                InitFromActiveTerrains();
            }

            public MultiTerrainMap(MultiTerrainMapData data)
            {
                InitFromActiveTerrains();

                var heightSetCoordinateState = new HeightSetCoordinateState();
                
                foreach (var terrainDataWrapper in data.multiTerrainData)
                {
                    int2 terrainIndex = terrainDataWrapper.terrainIndex;
                    
                    if (!terrainStateMap.ContainsKey(terrainIndex))
                    {
                        AddTerrainAt(terrainIndex);
                    }
                    
                    var (terrain, stateMap) = terrainStateMap[terrainIndex];
                    float[,] heightmap = terrain.terrainData.GetHeights(0, 0, HeightmapResolution, HeightmapResolution);

                    var serializableCoords = terrainDataWrapper.coordinates;                    
                    foreach (var serializableCoord in serializableCoords)
                    {
                        var (heightmapIndex, normalizedHeight, state, occupyingElementIndex) = serializableCoord;

                        if (state is CoordinateState.HeightSet)
                        {
                            heightmap[heightmapIndex.y, heightmapIndex.x] = normalizedHeight;
                            stateMap[heightmapIndex.x, heightmapIndex.y] = heightSetCoordinateState;
                        }
                        else if (state is CoordinateState.Occupied)
                        {
                            stateMap[heightmapIndex.x, heightmapIndex.y] = new OccupiedCoordinateState(Line.Instance
                                [occupyingElementIndex]);
                        }
                    }
                    
                    terrain.terrainData.SetHeights(0, 0, heightmap);
                }
            }
            
            public Terrain GetTerrainForWorldPosition(Vector3 worldPosition)
            {
                int2 index = GetIndex(worldPosition);
            
                if (terrainStateMap.TryGetValue(index, out var terrainCoordPair))
                {
                    return terrainCoordPair.terrain;
                }

                Debug.Log($"No terrain found for world position {worldPosition}. Expected tile coordinates: ({index.x}, {index.y}).");
                return null;
            }

            public IEnumerator<KeyValuePair<int2, (Terrain terrain, CoordinateStateHolder[,] coordStates)>> GetEnumerator()
            {
                return terrainStateMap.GetEnumerator();
            }
            
            public void Clear()
            {
                terrainStateMap.Clear();
            }

            public bool Contains(KeyValuePair<int2, (Terrain terrain, CoordinateStateHolder[,] coordStates)> item)
            {
                return terrainStateMap.Contains(item);
            }

            public int2 GetIndex(Terrain terrain)=>GetIndex(terrain.GetPosition());
            

            public int2 GetIndex(Vector3 position)
            {
                float tileScaledX = position.x / TerrainTileSize;
                float tileScaledZ = position.z / TerrainTileSize;
                
                int tileX = Mathf.FloorToInt(tileScaledX);
                int tileZ = Mathf.FloorToInt(tileScaledZ);
                
                return new int2(tileX, tileZ);
            }

            public Terrain GetTerrainOrDefault(int2 index)=> terrainStateMap.TryGetValue(index, out var value) ? value.terrain : null;
            

            public int Count => terrainStateMap.Count;
            
            private void AddToMap(int2 key, Terrain terrain)
            {
                var freeCoordinateState = new FreeCoordinateState();
                var coordStates = new CoordinateStateHolder[HeightmapResolution, HeightmapResolution];
                for (int i = 0; i < HeightmapResolution; i++)                {
                    for (int j = 0; j < HeightmapResolution; j++)                    {
                        coordStates[i, j] = freeCoordinateState;
                    }
                }
                terrainStateMap.Add(key, (terrain, coordStates));
            }

            public bool ContainsTerrain(Terrain terrain)
            {
                int2 key = GetIndex(terrain);
                return terrainStateMap.ContainsKey(key);
            }

            public bool Remove(int2 key)
            {
                return terrainStateMap.Remove(key);
            }
            
            public bool TryGetValue(int2 key, out Terrain terrain) 
            {
                if (terrainStateMap.TryGetValue(key, out var value))
                {
                    terrain = value.terrain;
                    return true;
                }
                
                terrain = null;
                return false;
            }

            public bool TryGetValue(Terrain terrain, out CoordinateStateHolder[,] coordStates)
            {
                if (terrainStateMap.TryGetValue(GetIndex(terrain), out var value))
                {
                    coordStates = value.coordStates;
                    return true;
                }
                
                coordStates = null;
                return false;
            }

            public (Terrain terrain, CoordinateStateHolder[,] coordStates) this[int2 key]
            {
                get => terrainStateMap[key];
                set => terrainStateMap[key] = value;
            }
            
            public CoordinateStateHolder[,] this[Terrain terrain] 
            {
                get => terrainStateMap[GetIndex(terrain)].coordStates;
                set => terrainStateMap[GetIndex(terrain)] = (terrain, value);
            }

            public ICollection<int2> Keys => terrainStateMap.Keys;

            public ICollection<(Terrain terrain, CoordinateStateHolder[,] coordStates)> Values => terrainStateMap.Values;
        }
        
        [SerializeField]
        private GameObject slopeBuilderPrefab;
        
        [SerializeField]
        private GameObject terrainTilePrefab;
        
        public static float MAX_HEIGHT;

        private float terrainTileSize;

        private int heightmapResolution;
        
        /// <summary>
        /// The spacing between heightmap points on a terrain in world units.
        /// </summary>
        private float heightmapSpacing;

        public float GlobalHeightLevel { get; private set; } = 0f;

        private MultiTerrainMap multiTerrainMap;

        /// <summary>
        /// Contains finished <see cref="SlopeChange"/> instances.
        /// </summary>
        public readonly List<SlopeChange> SlopeChanges = new();

        private SlopeChange activeSlope = null;
        public SlopeChange ActiveSlope
        {
            get => activeSlope;
            set
            {
                activeSlope = value;
                if (activeSlope == null)
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
        

        private void Awake()
        {
            MAX_HEIGHT = Terrain.activeTerrain.terrainData.heightmapScale.y/2; // the terrain default height is set to half of its size, so max height is half of the size

            terrainTileSize = Terrain.activeTerrain.terrainData.size.x;

            heightmapResolution = Terrain.activeTerrain.terrainData.heightmapResolution;
            
            heightmapSpacing = GetHeightmapSpacing();

            multiTerrainMap = new MultiTerrainMap();
        }

        //void Start()
        //{
        //    RollIn rollIn = Line.Instance.GetRollIn();
        //    if (rollIn != null)
        //    {
        //        rollIn.GetObstacleHeightmapCoordinates().MarkAs(new OccupiedCoordinateState(rollIn));
        //    }
        //}
        
        public void EnsureTerrainAt(Vector3 worldPosition)
        {
            multiTerrainMap.EnsureTerrainAt(worldPosition);
        }

        public void ShowSlopeInfo()
        {
            foreach (SlopeChange slope in SlopeChanges)
            {
                slope.ShowInfo();
            }
        }

        public void HideSlopeInfo()
        {
            foreach (SlopeChange slope in SlopeChanges)
            {
                slope.HideInfo();
            }
        }

        private Terrain GetTerrainForWorldPosition(Vector3 worldPosition) => multiTerrainMap.GetTerrainForWorldPosition(worldPosition);

        public IEnumerable<(int2 index, Terrain terrain)> GetTerrains()
        {
            foreach (var terrain in Terrain.activeTerrains)
            {
                int2 index = multiTerrainMap.GetIndex(terrain);
                yield return (index, terrain);
            }
        } 

        /// <summary>
        /// For all active terrains, sets the terrain (apart from occupied positions) to a given Height.
        /// </summary>
        /// <remarks><b>Expensive</b>, do not call frequently!</remarks>
        /// <param name="height">The terrain Height to set</param>
        public void SetHeight(float height)
        {            
            if (height < -MAX_HEIGHT || height > MAX_HEIGHT)
            {                
                height = Mathf.Clamp(height, -MAX_HEIGHT, MAX_HEIGHT);
            }

            float heightMapValue = WorldHeightToHeightmapHeight(height);

            foreach (var terrain in Terrain.activeTerrains)
            {
                float[,] heights = terrain.terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
                CoordinateStateHolder[,] stateMap = multiTerrainMap[terrain];
                
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

            SlopeChanges.Add(ActiveSlope);

            StudioUIManager.Instance.GetSidebar().DeleteSlopeButtonEnabled = true;
            StudioUIManager.Instance.GetSidebar().DeleteButtonEnabled = false;

            return builder.GetComponent<SlopePositioner>();
        }

        public void RemoveSlope(SlopeChange slope)
        {
            if (SlopeChanges.Contains(slope))
            {
                SlopeChanges.Remove(slope);
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
        private CoordinateStateHolder GetStateHolder(Terrain terrain, int2 coord)
        {
            // Ensure coordinates are within bounds
            if (coord.x >= 0 && coord.x < heightmapResolution && coord.y >= 0 && coord.y < heightmapResolution)
            {
                if (multiTerrainMap.TryGetValue(terrain, out CoordinateStateHolder[,] stateMap))
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
            
            int widthSteps = Mathf.CeilToInt(width / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(Vector3.Distance(start, end) / heightmapSpacing);

            Vector3 direction = (end - start).normalized;
            Vector3 directionNormal = Vector3.Cross(direction, Vector3.up).normalized;
            Vector3 leftStartCorner = start - 0.5f * width * directionNormal;

            Terrain currentTerrain = null;
            int2 lastGridKey = new(int.MinValue, int.MinValue);

            for (int i = 0; i <= lengthSteps; i++)
            {
                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 worldPos = leftStartCorner + j * heightmapSpacing * directionNormal + i * heightmapSpacing * direction;

                    // Grid Lookup Optimization
                    int2 gridCoords = multiTerrainMap.GetIndex(worldPos);

                    if (!lastGridKey.Equals(gridCoords))
                    {
                        lastGridKey = gridCoords;
                        multiTerrainMap.TryGetValue(lastGridKey, out currentTerrain);
                    }

                    if (currentTerrain != null)
                    {
                        if (!data.TryGetValue(currentTerrain, out var set))
                        {
                            set = new HashSet<int2>();
                            data[currentTerrain] = set;
                        }
                        
                        set.Add(WorldToHeightmapCoordinates(worldPos));
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
            HeightmapCoordinates coords = GetCoordinatesForArea(start, end, width);
            foreach (var (terrain, coord) in coords)
            {
                CoordinateStateHolder stateHolder = GetStateHolder(terrain, coord);
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
            // If we're already at or beyond the boundary, return 0
            if (maxDistance <= 0)
                return 0;
            
            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;

            foreach (var (terrain, coord) in GetCoordinatesForArea(start, start + direction * maxDistance, width))
            {
                CoordinateStateHolder state = GetStateHolder(terrain, coord);
                if (state.GetState() == CoordinateState.Occupied 
                    || (state.GetState() == CoordinateState.HeightSet && !Mathf.Approximately(GetHeightAt(terrain, coord), height)))
                {
                    return Vector3.Distance(start, 
                        Vector3.Project(HeightmapToWorldCoordinates(terrain, coord), direction));
                }
            }

            return maxDistance;
        }

        /// <summary>
        /// Calculates the height in world units at a given heightmap coordinate on a terrain,
        /// by first getting the heightmap value and then translating it to world units.
        /// </summary>
        /// <returns>The height in world units.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when coord is out of heightmap bounds.</exception>
        private float GetHeightAt(Terrain terrain, int2 coord)
        {
            // Ensure coordinates are within bounds
            if (coord.x >= 0 && coord.x < heightmapResolution &&
                coord.y >= 0 && coord.y < heightmapResolution)
            {
                return HeightmapHeightToWorldHeight(terrain.terrainData.GetHeight(coord.x, coord.y));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(coord), "Coordinate is out of bounds of the terrain heightmap resolution.");
            }
        }

        public void MarkTerrainAs(CoordinateStateHolder state, Terrain terrain, IEnumerable<int2> coordinates)
        {
            if (!multiTerrainMap.ContainsTerrain(terrain))
            {
                Debug.LogError($"Terrain {terrain.name} is not initialized in StateMap.");
                return;
            }
            
            var map = multiTerrainMap[terrain];
            foreach (var coord in coordinates)
            {
                // Ensure coordinates are within bounds
                if (coord.x >= 0 && coord.x < heightmapResolution &&
                    coord.y >= 0 && coord.y < heightmapResolution)
                {
                    map[coord.y, coord.x] = state;
                }
            }            
        }
        
        public void FitObstacleOnFlat(IObstacleBuilder obstacle)
        {
            obstacle.GetTransform().forward = Vector3.ProjectOnPlane(obstacle.GetRideDirection(), Vector3.up).normalized;
            float newHeight = GetHeightAt(obstacle.GetTransform().position);
            obstacle.GetTransform().position = new Vector3(obstacle.GetTransform().position.x, newHeight, obstacle.GetTransform().position.z);
        }


        private int2 WorldToHeightmapCoordinates(Vector3 worldPosition)
        {
            Terrain terrain = GetTerrainForWorldPosition(worldPosition);
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;

            // Calculate normalized positions
            float normalizedX = (worldPosition.x - terrainPosition.x) / terrainSize.x;
            float normalizedZ = (worldPosition.z - terrainPosition.z) / terrainSize.z;

            // Convert to heightmap coordinates
            int x = Mathf.Clamp(Mathf.FloorToInt(normalizedX * (heightmapResolution - 1)), 0, heightmapResolution - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt(normalizedZ * (heightmapResolution - 1)), 0, heightmapResolution - 1);
            return new int2(x, z);
        }

        private Vector3 HeightmapToWorldCoordinates(Terrain terrain,  int2 coord)
        {
            Vector3 terrainSize = terrain.terrainData.size;
            
            if (coord.x < 0 || coord.x >= heightmapResolution || coord.y < 0 || coord.y >= heightmapResolution)
            {
                throw new ArgumentOutOfRangeException(nameof(coord), "Coordinate is out of bounds of the terrain heightmap resolution.");
            }
            
            Vector3 terrainPosition = terrain.transform.position;

            // Calculate normalized positions
            float normalizedX = (float)coord.x / (heightmapResolution - 1);
            float normalizedZ = (float)coord.y / (heightmapResolution - 1);

            // Convert to world coordinates
            float worldX = terrainPosition.x + normalizedX * terrainSize.x;
            float worldZ = terrainPosition.z + normalizedZ * terrainSize.z;
            return new Vector3(worldX, 0, worldZ);
        }

        public Vector3 GetNormalForWorldPosition(Vector3 worldPosition)
        {
            TerrainCollider terrainCollider = GetTerrainForWorldPosition(worldPosition).GetComponent<TerrainCollider>();
            if (terrainCollider.Raycast(new Ray(worldPosition + Vector3.up * 50, Vector3.down), out RaycastHit hit, Mathf.Infinity))
            {
                return hit.normal.normalized;
            }

            return Vector3.up; // Fallback if raycast fails
        }

        /// <summary>
        /// Translates from a Height in world units to a Height in heightmap units.
        /// </summary>
        public static float WorldHeightToHeightmapHeight(float worldUnits)
        {
            return (worldUnits - Terrain.activeTerrain.transform.position.y) / Terrain.activeTerrain.terrainData.size.y;
        }

        private static float HeightmapHeightToWorldHeight(float heightmapUnits)
        {
            return heightmapUnits * Terrain.activeTerrain.terrainData.size.y + Terrain.activeTerrain.transform.position.y;
        }

        /// <summary>
        /// Gets the spacing in world units between heightmap points on a terrain.
        /// </summary>
        /// <returns>The smaller of the two spacings in the X and Z directions.</returns>
        private float GetHeightmapSpacing() => terrainTileSize / (heightmapResolution - 1) / 2; // divide to make sure that no heightmap points are missed

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

            int widthSteps = Mathf.CeilToInt(width / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(distanceToModify / heightmapSpacing);

            // First pass: collect coordinates, heights, and bounding boxes per terrain
            var terrainData = new Dictionary<Terrain, (Dictionary<int2, float> coordHeights, int minX, int maxX, int minY, int maxY)>();
            var allCoordinates = new Dictionary<Terrain, HashSet<int2>>();

            for (int i = 0; i <= lengthSteps; i++)
            {
                float t = lengthSteps > 0 ? (float)i / lengthSteps : 0f;
                float heightAtLengthMap = WorldHeightToHeightmapHeight(Mathf.Lerp(startHeight, endHeight, t));

                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 worldPos = leftStartCorner + j * heightmapSpacing * rideDirNormal + i * heightmapSpacing * rideDir;

                    Terrain terrain = GetTerrainForWorldPosition(worldPos);
                    if (terrain == null) continue;

                    int2 hp = WorldToHeightmapCoordinates(worldPos);

                    if (GetStateHolder(terrain, hp) is OccupiedCoordinateState)
                        continue;

                    if (terrainData.TryGetValue(terrain, out var entry))
                    {
                        entry.coordHeights[hp] = heightAtLengthMap;
                        terrainData[terrain] = (
                            entry.coordHeights,Mathf.Min(entry.minX, hp.x),
                            Mathf.Max(entry.maxX, hp.x),
                            Mathf.Min(entry.minY, hp.y),
                            Mathf.Max(entry.maxY, hp.y)
                        );
                    }
                    else
                    {
                        var coordHeights = new Dictionary<int2, float> { { hp, heightAtLengthMap } };
                        terrainData[terrain] = (coordHeights, hp.x, hp.x, hp.y, hp.y);
                        allCoordinates[terrain] = new HashSet<int2>();
                    }

                    allCoordinates[terrain].Add(hp);
                }
            }

            // Second pass: load, modify, write heights per terrain using precomputed bounds
            foreach (var (terrain, (coordHeights, minX, maxX, minY, maxY)) in terrainData)
            {
                int hMapWidth = maxX - minX + 1;
                int hMapHeight = maxY - minY + 1;

                float[,] heights = terrain.terrainData.GetHeights(minX, minY, hMapWidth, hMapHeight);

                foreach (var (coord, targetHeight) in coordHeights)
                {
                    int x = coord.x - minX;
                    int y = coord.y - minY;

                    if (x >= 0 && x < hMapWidth && y >= 0 && y < hMapHeight)
                    {
                        heights[y, x] = targetHeight;
                    }
                }

                terrain.terrainData.SetHeightsDelayLOD(minX, minY, heights);
            }

            return new HeightmapCoordinates(allCoordinates);
        }
        
        /// <summary>
        /// Gets the world space terrain height at a given world space position.
        /// </summary>        
        public float GetHeightAt(Vector3 position)
        {
            Terrain terrain = GetTerrainForWorldPosition(position);
            float height = terrain.SampleHeight(position) + terrain.transform.position.y;
            return height;
        }

        public static void ConfirmChanges()
        {
            foreach (var terrain in Terrain.activeTerrains)
            {
                terrain.terrainData.SyncHeightmap();
            }
        }

        public TerrainManagerData GetSerializableData() => new TerrainManagerData(this, multiTerrainMap);

        public void LoadFromData(TerrainManagerData data)
        {
            GlobalHeightLevel = data.globalHeight;

            SlopeChanges.Clear();

            foreach (var t in data.slopes)
            {
                SlopeChange slope = Instantiate(DataManager.Instance.slopeChangePrefab, Vector3.zero, Quaternion.identity).GetComponent<SlopeChange>();
                slope.transform.SetParent(transform);
                SlopeChanges.Add(slope);
                slope.LoadFromData(t);
            }

            // only after the terrain manager is loaded, we can set the heightmap coordinates of the line elements
            // TODO this should not be needed as coordinate states are also kept in multiTerrainMapData
            // foreach (ILineElement element in Line.Instance)
            // {
            //     HeightmapCoordinates slopeCoords = element.GetUnderlyingSlopeHeightmapCoordinates();
            //     slopeCoords?.MarkAs(new HeightSetCoordinateState());
            //
            //     HeightmapCoordinates coords = element.GetObstacleHeightmapCoordinates();
            //     coords?.MarkAs(new OccupiedCoordinateState(element));
            // }
            
            multiTerrainMap = new(data.multiTerrainMapData);
        }
    }
}