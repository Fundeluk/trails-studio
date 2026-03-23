using System.Collections.Generic;
using System.Linq;
using LineSystem;
using Managers;
using Unity.Mathematics;
using UnityEngine;

namespace TerrainEditing
{
    public partial class TerrainManager
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
                float yPos = -MaxHeight;
                float zPos = index.y * TerrainTileSize;

                return new Vector3(xPos, yPos, zPos);
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
                
                // Register in our map
                AddToMap(newTerrain);

                // Connect to neighbors
                ConnectNeighbors(newTerrain);
            }
            
            public void EnsureTerrainAt(Vector3 worldPosition)
            {
                int2 index = GetIndex(worldPosition);
                
                if (!terrainStateMap.ContainsKey(index))
                {
                    AddTerrainAt(index);
                }
            }

            public void ClearUnusedTerrains()
            {
                List<Terrain> unused = new();
                foreach (var (terrain, coordStates) in terrainStateMap.Values)
                {
                    int counter = 0;
                    foreach (var coordState in  coordStates)
                    {
                        if (coordState.GetState() != CoordinateState.Free)
                        {
                            break;
                        }
                        
                        counter++;
                    }

                    if (counter == coordStates.Length)
                    {
                        unused.Add(terrain);
                    }
                    
                }

                foreach (var terrain in unused)
                {
                    RemoveTerrain(terrain);
                }
            }
            
            private  void RemoveTerrain(Terrain terrain) 
            {
                int2 index = GetIndex(terrain);
                
                terrainStateMap.Remove(index);
                
                Destroy(terrain.gameObject);
            }
            
            // TODO check whether terrains are connected after loading a save
            private void ConnectNeighbors(Terrain currentTerrain)
            {
                int2 gridCoords = GetIndex(currentTerrain);
                
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
                            stateMap[heightmapIndex.y, heightmapIndex.x] = heightSetCoordinateState;
                        }
                        else if (state is CoordinateState.Occupied)
                        {
                            stateMap[heightmapIndex.y, heightmapIndex.x] = new OccupiedCoordinateState(Line.Instance
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

            public CoordinateStateHolder GetStateHolder(Terrain terrain, int2 coord)
            {
                return terrainStateMap[GetIndex(terrain)].coordStates[coord.y, coord.x];
            }

            public bool Contains(KeyValuePair<int2, (Terrain terrain, CoordinateStateHolder[,] coordStates)> item)
            {
                return terrainStateMap.Contains(item);
            }
            
            public (Terrain terrain, int2 coord) GetHeightmapCoordinate(Vector3 worldPosition)
            {
                Terrain terrain = GetTerrainForWorldPosition(worldPosition);
                if (terrain == null)
                {
                    Debug.LogError($"No terrain found at world position {worldPosition}");
                    return (null, int2.zero);
                }
                
                Vector3 terrainOrigin = terrain.GetPosition();

                // Calculate normalized positions
                float normalizedX = (worldPosition.x - terrainOrigin.x) / TerrainTileSize;
                float normalizedZ = (worldPosition.z - terrainOrigin.z) / TerrainTileSize;

                // Convert to heightmap coordinates
                int x = Mathf.Clamp(Mathf.RoundToInt(normalizedX * (HeightmapResolution - 1)), 0, HeightmapResolution - 1);
                int z = Mathf.Clamp(Mathf.RoundToInt(normalizedZ * (HeightmapResolution - 1)), 0, HeightmapResolution - 1);

                return (terrain, new int2(x, z));
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
            
            private void AddToMap(Terrain terrain)
            {
                var freeCoordinateState = new FreeCoordinateState();
                var coordStates = new CoordinateStateHolder[HeightmapResolution, HeightmapResolution];
                for (int i = 0; i < HeightmapResolution; i++)                {
                    for (int j = 0; j < HeightmapResolution; j++)                    {
                        coordStates[i, j] = freeCoordinateState;
                    }
                }
                terrainStateMap.Add(GetIndex(terrain), (terrain, coordStates));
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
                
    }
}