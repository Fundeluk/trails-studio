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

}