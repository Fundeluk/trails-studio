using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System;
using LineSystem;
using Managers;
using Misc;
using Obstacles;
using Obstacles.TakeOff;
using TerrainEditing.Slope;

namespace TerrainEditing
{
    public partial class TerrainManager : Singleton<TerrainManager>, ISaveable<TerrainManagerData>
    {
        [SerializeField]
        private GameObject slopeBuilderPrefab;
        
        [SerializeField]
        private GameObject terrainTilePrefab;
        
        public static float MaxHeight { get; private set; }

        private float terrainTileSize;

        private int heightmapResolution;
        
        /// <summary>
        /// The spacing between heightmap points on a terrain in world units.
        /// </summary>
        public float HeightmapSpacing { get; private set; }

        /// <summary>
        /// Global height of terrain in world units.
        /// </summary>
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
            MaxHeight = Terrain.activeTerrain.terrainData.heightmapScale.y/2; // the terrain default height is set to half of its size, so max height is half of the size

            terrainTileSize = Terrain.activeTerrain.terrainData.size.x;

            heightmapResolution = Terrain.activeTerrain.terrainData.heightmapResolution;
            
            HeightmapSpacing = GetHeightmapSpacing();

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
        
        public void EnsureTerrainAt(Vector3 worldPosition) => multiTerrainMap.EnsureTerrainAt(worldPosition);

        public void ClearUnusedTerrains() => multiTerrainMap.ClearUnusedTerrains();

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
        
        /// <summary>
        /// For all active terrains, sets the terrain (apart from occupied positions) to a given Height.
        /// </summary>
        /// <remarks><b>Expensive</b>, do not call frequently!</remarks>
        /// <param name="height">The terrain Height to set</param>
        public void SetHeight(float height)
        {            
            if (height < -MaxHeight || height > MaxHeight)
            {      
                StudioUIManager.Instance.ShowMessage($"Height must be between {-MaxHeight} and {MaxHeight}!", 2f);
                height = Mathf.Clamp(height, -MaxHeight, MaxHeight);
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
                InternalDebug.LogWarning("Trying to remove a slope that is not in the list of slopes.");
            }

            if (ActiveSlope == slope)
            {
                ActiveSlope = null;
            }
            else
            {
                InternalDebug.LogWarning("Trying to remove an inactive slope.");
            }            
        }
        
        public CoordinateStateHolder GetTerrainStateAt(Vector3 worldPos)
        {
            multiTerrainMap.EnsureTerrainAt(worldPos);
            var (terrain, index) = multiTerrainMap.GetHeightmapCoordinate(worldPos);
            return multiTerrainMap.GetStateHolder(terrain, index);
        }

        /// <summary>
        /// Calculates which Terrain tiles are touched by the path and splits the coordinates accordingly.
        /// </summary>
        public HeightmapCoordinates GetCoordinatesForArea(Vector3 start, Vector3 end, float width)
        {
            var data = new Dictionary<Terrain, HashSet<int2>>();
            
            int widthSteps = Mathf.CeilToInt(width / HeightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(Vector3.Distance(start, end) / HeightmapSpacing);

            Vector3 direction = (end - start).normalized;
            Vector3 directionNormal = Vector3.Cross(direction, Vector3.up).normalized;
            Vector3 leftStartCorner = start - 0.5f * width * directionNormal;

            Terrain currentTerrain = null;
            int2 lastGridKey = new(int.MinValue, int.MinValue);

            for (int i = 0; i <= lengthSteps; i++)
            {
                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 worldPos = leftStartCorner + j * HeightmapSpacing * directionNormal + i * HeightmapSpacing * direction;
                    
                    EnsureTerrainAt(worldPos);

                    // Grid Lookup Optimization
                    int2 gridCoords = multiTerrainMap.GetIndex(worldPos);

                    if (!lastGridKey.Equals(gridCoords))
                    {
                        lastGridKey = gridCoords;
                        currentTerrain = multiTerrainMap[lastGridKey].terrain;
                    }
                    
                    if (!data.TryGetValue(currentTerrain, out var set))
                    {
                        set = new HashSet<int2>();
                        data[currentTerrain] = set;
                    }
                    
                    set.Add(multiTerrainMap.GetHeightmapCoordinate(worldPos).coord);
                    
                }
            }

            return new HeightmapCoordinates(data);
        }
        
        /// <summary>
        /// Checks if an area from start to end of some width is unoccupied.
        /// </summary>
        /// <param name="allowedElement">A <see cref="ILineElement"/> that will not be considered occupying the queried area.</param> 

        public bool IsAreaFree(Vector3 start, Vector3 end, float width, ILineElement allowedElement = null)
        {
            HeightmapCoordinates coords = GetCoordinatesForArea(start, end, width);
            foreach (var (terrain, coord) in coords)
            {
                CoordinateStateHolder stateHolder = multiTerrainMap.GetStateHolder(terrain, coord);
                CoordinateState state = stateHolder.GetState();
                if (stateHolder is OccupiedCoordinateState occupiedState)
                {                    
                    if (allowedElement != null && occupiedState.OccupyingElement == allowedElement)
                    {
                        continue; // Allowed element occupies this coordinate
                    }
                    InternalDebug.Log($"Coordinate {coord} on terrain {terrain.name} is occupied by {occupiedState.OccupyingElement.GetType().Name}, which is not the allowed element {allowedElement?.GetType().Name ?? "null"}.");
                    return false;
                }
                else if (state == CoordinateState.HeightSet)
                {                    
                    // If the height is set, we cannot build here
                    InternalDebug.Log($"Coordinate {coord} on terrain {terrain.name} has its height set, cannot build here.");
                    return false;
                }
            }
            return true;
        }
        

        /// <summary>
        /// Finds out how far you can go from start in direction with a certain width and height until you hit an occupied coordinate or a coordinate with a different height, up to a maximum distance.
        /// </summary>
        /// <param name="maxDistance">A bound on how far we search.</param>
        /// <param name="allowedElement">A <see cref="ILineElement"/> that will not be considered occupying the queried area.</param> 
        /// <returns>The distance in meters.</returns>
        public float GetRideableDistance(Vector3 start, Vector3 direction, float width, float height, float maxDistance, ILineElement allowedElement = null)
        {
            InternalDebug.Log($"Checking rideable distance from {start} in direction {direction} with width {width}, height {height}, and maxDistance {maxDistance}. Allowed element: {allowedElement?.GetType().Name ?? "null"}"); 
            // If we're already at or beyond the boundary, return 0
            if (maxDistance <= 0)
                return 0;
            
            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            
            foreach (var (terrain, coord) in GetCoordinatesForArea(start, start + direction * maxDistance, width))
            {
                CoordinateStateHolder state = multiTerrainMap.GetStateHolder(terrain, coord);
                
                if (state is OccupiedCoordinateState occupiedState)
                {                    
                    if (allowedElement != null && occupiedState.OccupyingElement == allowedElement)
                    {
                        continue; // Allowed element occupies this coordinate
                    }
                    InternalDebug.Log($"Coordinate {coord} on terrain {terrain.name} is occupied by {occupiedState.OccupyingElement.GetType().Name}, which is not the allowed element {allowedElement?.GetType().Name ?? "null"}.");
                    
                    return Vector3.Distance(start, 
                        Vector3.Project(multiTerrainMap.GetWorldPosition(terrain, coord) - start, direction));
                }
                
                if (state.GetState() == CoordinateState.HeightSet && !Mathf.Approximately(GetHeightAt(terrain, coord), height))
                {
                    InternalDebug.Log($"Coordinate {coord} on terrain {terrain.name} has its height set to {GetHeightAt(terrain, coord)}, which is different from the ride height {height}, cannot ride here.");
                    return Vector3.Distance(start, 
                        Vector3.Project(multiTerrainMap.GetWorldPosition(terrain, coord) - start, direction));
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

        private void MarkTerrainAs(CoordinateStateHolder state, Terrain terrain, IEnumerable<int2> coordinates) 
            => multiTerrainMap.MarkTerrainAs(state, terrain, coordinates);
        
        public void FitObstacleOnFlat(IObstacleBuilder obstacle)
        {
            obstacle.GetTransform().forward = Vector3.ProjectOnPlane(obstacle.GetRideDirection(), Vector3.up).normalized;
            float newHeight = GetHeightAt(obstacle.GetTransform().position);
            obstacle.GetTransform().position = new Vector3(obstacle.GetTransform().position.x, newHeight, obstacle.GetTransform().position.z);
        }

        public Vector3 GetNormalForWorldPosition(Vector3 worldPosition)
        {
            TerrainCollider terrainCollider = multiTerrainMap.GetTerrainForWorldPosition(worldPosition).GetComponent<TerrainCollider>();
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

            int widthSteps = Mathf.CeilToInt(width / HeightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(distanceToModify / HeightmapSpacing);

            // First pass: collect coordinates, heights, and bounding boxes per terrain
            var terrainData = new Dictionary<Terrain, (Dictionary<int2, float> coordHeights, int minX, int maxX, int minY, int maxY)>();
            var allCoordinates = new Dictionary<Terrain, HashSet<int2>>();

            for (int i = 0; i <= lengthSteps; i++)
            {
                float t = lengthSteps > 0 ? (float)i / lengthSteps : 0f;
                float heightAtLengthMap = WorldHeightToHeightmapHeight(Mathf.Lerp(startHeight, endHeight, t));

                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 worldPos = leftStartCorner + j * HeightmapSpacing * rideDirNormal + i * HeightmapSpacing * rideDir;
                    
                    var (terrain, hp) = multiTerrainMap.GetHeightmapCoordinate(worldPos);

                    // update main terrain
                    AddHeight(terrain, hp, heightAtLengthMap);

                    // propagate to neighbors if we are on an edge
                    // left edge => update left neighbor's right edge
                    if (hp.x == 0 && terrain.leftNeighbor)
                        AddHeight(terrain.leftNeighbor, new int2(heightmapResolution - 1, hp.y), heightAtLengthMap);
            
                    // right edge => update right neighbors left edge
                    if (hp.x == heightmapResolution - 1 && terrain.rightNeighbor)
                        AddHeight(terrain.rightNeighbor, new int2(0, hp.y), heightAtLengthMap);

                    // bottom edge => update bottom neighbors top edge
                    if (hp.y == 0 && terrain.bottomNeighbor)
                        AddHeight(terrain.bottomNeighbor, new int2(hp.x, heightmapResolution - 1), heightAtLengthMap);

                    // top edge => update top neighbors bottom edge
                    if (hp.y == heightmapResolution - 1 && terrain.topNeighbor)
                        AddHeight(terrain.topNeighbor, new int2(hp.x, 0), heightAtLengthMap);
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

            // Local helper to add height and handle dictionary logic safely
            void AddHeight(Terrain t, int2 p, float h)
            {
                if (t == null) return;

                if (terrainData.TryGetValue(t, out var entry))
                {
                    entry.coordHeights[p] = h;
                    terrainData[t] = (
                        entry.coordHeights,
                        Mathf.Min(entry.minX, p.x),
                        Mathf.Max(entry.maxX, p.x),
                        Mathf.Min(entry.minY, p.y),
                        Mathf.Max(entry.maxY, p.y)
                    );
                }
                else
                {
                    var coordHeights = new Dictionary<int2, float> { { p, h } };
                    terrainData[t] = (coordHeights, p.x, p.x, p.y, p.y);
                    allCoordinates[t] = new HashSet<int2>();
                }
                allCoordinates[t].Add(p);
            }
        }
        
        /// <summary>
        /// Gets the world space terrain height at a given world space position.
        /// </summary>        
        public float GetHeightAt(Vector3 position)
        {
            Terrain terrain = multiTerrainMap.GetTerrainForWorldPosition(position);
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

        public TerrainManagerData GetSerializableData() => new(this, multiTerrainMap);

        public void LoadFromData(TerrainManagerData data)
        {
            GlobalHeightLevel = data.globalHeightLevel;
            
            multiTerrainMap = new(data.multiTerrainMapData);
            
            SlopeChanges.Clear();

            foreach (var t in data.slopes)
            {
                SlopeChange slope = Instantiate(DataManager.Instance.slopeChangePrefab, Vector3.zero, Quaternion.identity).GetComponent<SlopeChange>();
                slope.transform.SetParent(transform);
                SlopeChanges.Add(slope);
                slope.LoadFromData(t);
            }
            
            // only after the terrain is loaded, we load heightmap coordinate states of the takeoff ride paths
            foreach (ILineElement element in Line.Instance)
            {
                if (element is Takeoff takeoff)
                {
                    takeoff.GetRidePathHeightmapCoordinates().MarkAs(new HeightSetCoordinateState());
                }
            }
            
            SetHeight(GlobalHeightLevel);
        }
        
        #if UNITY_EDITOR
        public void DebugDrawCoordinateStates()
        {
            if (multiTerrainMap == null) return;
        
            foreach (var mapEntry in multiTerrainMap.Values)
            {
                Terrain terrain = mapEntry.terrain;
                CoordinateStateHolder[,] coordStates = mapEntry.coordStates;
        
                if (terrain == null || coordStates == null) continue;
        
                int resolution = multiTerrainMap.HeightmapResolution;
        
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        CoordinateStateHolder stateHolder = coordStates[y, x];
                        CoordinateState state = stateHolder.GetState();
        
                        if (state != CoordinateState.Free)
                        {
                            // Calculate the world position of this heightmap coordinate
                            Vector3 worldPos = multiTerrainMap.GetWorldPosition(terrain, new int2(x, y));
                            // Adjust to the actual terrain height at that position
                            worldPos.y = terrain.SampleHeight(worldPos) + terrain.transform.position.y;
        
                            Color rayColor = Color.white;
                            if (state == CoordinateState.Occupied)
                                rayColor = Color.red;
                            else if (state == CoordinateState.HeightSet)
                                rayColor = Color.blue;
        
                            // Draw a ray pointing up for 5 seconds
                            Debug.DrawRay(worldPos, Vector3.up * 2f, rayColor, 5f);
                        }
                    }
                }
            }
        }
        #endif
        
    }
}