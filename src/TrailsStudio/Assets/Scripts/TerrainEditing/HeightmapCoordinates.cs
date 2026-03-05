using System.Collections;
using System.Collections.Generic;
using Managers;
using Unity.Mathematics;
using UnityEngine;

namespace TerrainEditing
{
    /// <summary>
    /// Class that contains the coordinates of a heightmap in a terrain. Optimized for writing to heightmap.
    /// </summary>
    public class HeightmapCoordinates : IEnumerable<int2>
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
        /// Note: This loses the context of which terrain the coordinate belongs to.
        /// </summary>
        public IEnumerator<int2> GetEnumerator()
        {
            foreach (var patch in patches.Values)
            {
                foreach (var coord in patch.Coordinates)
                {
                    yield return coord;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public HeightmapCoordinates() { }

        // Constructor that accepts the pre-calculated dictionary (usually from TerrainManager)
        public HeightmapCoordinates(Dictionary<Terrain, HashSet<int2>> data)
        {
            foreach (var kvp in data)
            {
                foreach (var coord in kvp.Value)
                {
                    Add(kvp.Key, coord);
                }
            }
        }

        public HeightmapCoordinates(Vector3 start, Vector3 end, float width)
        {
            Coordinates = new();

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

                    Coordinates.Add(heightmapPosition);
                }
            }

            StartX = minX;
            StartY = minY;
            this.ArrayWidth = maxX - minX + 1;
            this.ArrayHeight = maxY - minY + 1;
        }

        public HeightmapCoordinates(IEnumerable<int2> coords, Terrain terrain)
        {
            Coordinates = new();
            this.terrain = terrain;

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
                Coordinates.Add(coord);
            }

            StartX = minX;
            StartY = minY;
            ArrayWidth = maxX - minX + 1;
            ArrayHeight = maxY - minY + 1;
        }

        public HeightmapCoordinates(int startX, int startY, int width, int height, IEnumerable<int2> coords, Terrain terrain)
        {
            Coordinates = new HashSet<int2>();
            this.terrain = terrain;

            foreach (int2 coord in coords)
            {
                Coordinates.Add(coord);
            }

            this.StartX = startX;
            this.StartY = startY;
            this.ArrayWidth = width;
            this.ArrayHeight = height;            
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
                foreach(var coord in sourcePatch.Coordinates)
                {
                    Add(terrain, coord);
                }
            }
        }
        
        public void Add(Terrain terrain, int2 coordinate)
        {
            if (!patches.TryGetValue(terrain, out TerrainPatch patch))
            {
                patch = new TerrainPatch();
                patches[terrain] = patch;
            }
            patch.Add(coordinate);
        }

        public void Add(HeightmapCoordinates other)
        {
            if (other == null) return;

            foreach (var kvp in other.patches)
            {
                Terrain terrain = kvp.Key;
                TerrainPatch otherPatch = kvp.Value;
                
                foreach (var coord in otherPatch.Coordinates)
                {
                    Add(terrain, coord);
                }
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
                float localHeight = TerrainManager.WorldUnitsToHeightmapUnits(worldHeight);

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
    }

}