using System.Collections;
using System.Collections.Generic;
using Managers;
using Unity.Mathematics;
using UnityEngine;

namespace TerrainEditing
{
    public partial class TerrainManager
    {
        /// <summary>
        /// Class that contains the coordinates of a heightmap in a terrain. Optimized for writing to heightmap.
        /// </summary>
        public class HeightmapCoordinates : IEnumerable<(Terrain, int2)>
        {
            private class TerrainPatch
            {
                /// <summary>
                /// Coordinates in unbounded (zero-based) heightmap space.
                /// </summary>
                public readonly HashSet<int2> Coordinates = new();
                public int MinX = int.MaxValue;
                public int MinY = int.MaxValue;
                public int MaxX = int.MinValue;
                public int MaxY = int.MinValue;

                public int Width => MaxX - MinX + 1;
                public int Height => MaxY - MinY + 1;

                /// <summary>
                /// Adds a coordinate to the heightmap coordinates and updates the startX, startY, width, and height accordingly.
                /// </summary>
                /// <param name="coord">Coordinate in unbounded heightmap space.</param>
                public void Add(int2 coord)
                {
                    Coordinates.Add(coord);
                    MinX = Mathf.Min(MinX, coord.x);
                    MinY = Mathf.Min(MinY, coord.y);
                    MaxX = Mathf.Max(MaxX, coord.x);
                    MaxY = Mathf.Max(MaxY, coord.y);
                }
            }

            // The core data structure: One patch per Terrain
            private readonly Dictionary<Terrain, TerrainPatch> patches = new();
            
            /// <summary>
            /// Iterates over ALL coordinates across ALL terrains. 
            /// </summary>
            public IEnumerator<(Terrain, int2)> GetEnumerator()
            {
                foreach (var kvp in patches)
                {
                    foreach (var coord in kvp.Value.Coordinates)
                    {
                        yield return (kvp.Key, coord);
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public HeightmapCoordinates()
            {
                patches = new();
            }

            // Constructor that accepts the pre-calculated dictionary (usually from TerrainManager)
            public HeightmapCoordinates(Dictionary<Terrain, HashSet<int2>> data)
            {
                foreach (var kvp in data)
                {                
                    Add(kvp.Key, kvp.Value);
                }
            }

            // Clone constructor
            public HeightmapCoordinates(HeightmapCoordinates toClone)
            {
                if (toClone == null) return;
                foreach (var kvp in toClone.patches)
                {
                    Terrain terrain = kvp.Key;
                    TerrainPatch sourcePatch = kvp.Value;
                    
                    // Deep copy
                    Add(terrain, sourcePatch.Coordinates);
                }
            }
            
            public HeightmapCoordinates(SerializableHeightmapCoordinates serializable)
            {
                foreach (var serializablePatch in serializable.patches)
                {
                    Terrain terrain = TerrainManager.Instance.multiTerrainMap.GetTerrainOrDefault(serializablePatch.terrainIndex);
                    if (terrain != null)
                    {
                        Add(terrain, new HashSet<int2>(serializablePatch.coordinates));
                    }
                    else
                    {
                        Debug.LogWarning($"Terrain with index {serializablePatch.terrainIndex} not found. Skipping patch.");
                    }
                }
            }
            
            public void Add(Terrain terrain, HashSet<int2> coordinates)
            {
                if (!patches.TryGetValue(terrain, out TerrainPatch patch))
                {
                    patch = new TerrainPatch();
                    patches[terrain] = patch;
                }

                foreach (var coordinate in coordinates)
                {
                    patch.Add(coordinate);
                }
            }

            public void Add(HeightmapCoordinates other)
            {
                if (other == null) return;

                foreach (var kvp in other.patches)
                {
                    Terrain otherTerrain = kvp.Key;
                    TerrainPatch otherPatch = kvp.Value;
                    
                    Add(otherTerrain, otherPatch.Coordinates);
                }
            }

            public void MarkAs(CoordinateStateHolder state)
            {
                foreach (var kvp in patches)
                {
                    // Pass the specific terrain and its coordinates to the manager
                    TerrainManager.Instance.MarkTerrainAs(state, kvp.Key, kvp.Value.Coordinates);
                }
            }
            /// <summary>
            /// Sets the height of the coordinates to height in world space.
            /// </summary>
            /// <param name="worldHeight">Height to set in world space.</param>
            public void SetHeight(float worldHeight)
            {
                foreach (var kvp in patches)
                {
                    Terrain terrain = kvp.Key;
                    TerrainPatch patch = kvp.Value;

                    // 1. Get current heights for the bounds of this patch
                    float[,] heights = terrain.terrainData.GetHeights(patch.MinX, patch.MinY, patch.Width, patch.Height);
                    
                    // 2. Convert world height to this specific terrain's local height units
                    float localHeight = TerrainManager.WorldHeightToHeightmapHeight(worldHeight);

                    // 3. Modify only the specific pixels
                    foreach (var coord in patch.Coordinates)
                    {
                        // Convert unbounded coordinate to array local index
                        int x = coord.x - patch.MinX;
                        int y = coord.y - patch.MinY;
                        
                        heights[y, x] = localHeight;
                    }

                    // 4. Apply back
                    terrain.terrainData.SetHeightsDelayLOD(patch.MinX, patch.MinY, heights);
                }
            }
            

            
            /// <summary>
            /// Raises corners slightly for debugging.
            /// </summary>
            public void RaiseCorners()
            {
                foreach (var kvp in patches)
                {
                    Terrain terrain = kvp.Key;
                    TerrainPatch patch = kvp.Value;

                    float[,] heights = terrain.terrainData.GetHeights(patch.MinX, patch.MinY, patch.Width, patch.Height);

                    foreach (var coord in patch.Coordinates)
                    {
                        int x = coord.x - patch.MinX;
                        int y = coord.y - patch.MinY;
                        heights[y, x] += 0.001f; // Small indentation
                    }

                    terrain.terrainData.SetHeights(patch.MinX, patch.MinY, heights);
                }
            }

            public List<SerializableHeightmapCoordinates.SerializablePatch> ToSerializable()
            {
                var serializablePatches = new List<SerializableHeightmapCoordinates.SerializablePatch>();

                foreach (var kvp in patches)
                {
                    Terrain terrain = kvp.Key;
                    int2 terrainIndex = TerrainManager.Instance.multiTerrainMap.GetIndex(terrain);
                    
                    TerrainPatch patch = kvp.Value;

                    var serializablePatch = new SerializableHeightmapCoordinates.SerializablePatch
                    {
                        terrainIndex = terrainIndex,
                        minX = patch.MinX,
                        minY = patch.MinY,
                        maxX = patch.MaxX,
                        maxY = patch.MaxY,
                        coordinates = new List<int2>(patch.Coordinates)
                    };

                    serializablePatches.Add(serializablePatch);
                }

                return serializablePatches;
            }
        }
        
    }

}