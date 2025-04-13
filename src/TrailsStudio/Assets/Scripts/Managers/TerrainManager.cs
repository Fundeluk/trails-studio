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
    /// <summary>
    /// Struct that contains the coordinates of a heightmap in a terrain. Optimized for writing to heightmap.
    /// </summary>
    public struct HeightmapCoordinates : IEnumerable<int2>
    {
        public Terrain terrain;

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

        public HashSet<int2> coordinates;

        public readonly IEnumerator<int2> GetEnumerator()
        {
            return ((IEnumerable<int2>)coordinates).GetEnumerator();
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)coordinates).GetEnumerator();
        }

        public HeightmapCoordinates(Vector3 start, Vector3 end, float width)
        {
            terrain = TerrainManager.GetTerrainForPosition(start);
            coordinates = new();

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            float heightmapSpacing = TerrainManager.GetHeightmapSpacing(terrain);
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
                    int2 heightmapPosition = TerrainManager.WorldToHeightmapCoordinates(position, terrain);

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

        public HeightmapCoordinates(Terrain terrain, IEnumerable<int2> coords)
        {
            this.terrain = terrain;
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

            startY = minX;
            startX = minY;
            width = maxX - minX + 1;
            height = maxY - minY + 1;
        }

        public HeightmapCoordinates(Terrain terrain, int startX, int startY, int width, int height, IEnumerable<int2> coords)
        {
            this.terrain = terrain;
            coordinates = (HashSet<int2>)coords;
            this.startX = startX;
            this.startY = startY;
            this.width = width;
            this.height = height;            
        }

        public void Add(Vector3 start, Vector3 end, float width)
        {
            float heightmapSpacing = TerrainManager.GetHeightmapSpacing(terrain);
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
                    int2 heightmapPosition = TerrainManager.WorldToHeightmapCoordinates(position, terrain);

                    // Update min and max coordinates
                    startX = Mathf.Min(startX, heightmapPosition.x);
                    startY = Mathf.Min(startY, heightmapPosition.y);
                    width = Mathf.Max(width, heightmapPosition.x - startX + 1);
                    height = Mathf.Max(height, heightmapPosition.y - startY + 1);

                    coordinates.Add(heightmapPosition);
                }
            }
        }

        public void Add(HeightmapCoordinates other)
        {
            // Ensure both HeightmapCoordinates are on the same terrain
            if (terrain != other.terrain)
            {
                throw new InvalidOperationException("Cannot merge HeightmapCoordinates from different terrains.");
            }

            coordinates.UnionWith(other.coordinates);

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

        public readonly void MarkAsOccupied() => TerrainManager.Instance.MarkTerrainAsOccupied(terrain, coordinates);
        public readonly void UnmarkAsOccupied() => TerrainManager.Instance.UnmarkOccupiedTerrain(terrain, coordinates);

        public readonly void SetHeight(float height)
        {
            float heightmapValue = TerrainManager.WorldUnitsToHeightmapUnits(height, terrain);
            float[,] heights = terrain.terrainData.GetHeights(startX, startY, width, this.height);

            foreach (var coord in coordinates)
            {
                int x = coord.x - startX;
                int y = coord.y - startY;
                heights[y, x] = heightmapValue;                
            }

            terrain.terrainData.SetHeights(startX, startY, heights);
        }

        /// <summary>
        /// For debugging purposes. Raises the corners of the terrain by 0.5f to show the area occupied by the coordinates.
        /// </summary>
        public readonly void RaiseCorners()
        {
            float[,] heights = terrain.terrainData.GetHeights(startX, startY, width, this.height);

            // Raise the corners
            heights[0, 0] += 0.5f;
            heights[0, width - 1] += 0.5f;
            heights[height - 1, 0] += 0.5f;
            heights[height - 1, width - 1] += 0.5f;

            terrain.terrainData.SetHeights(startX, startY, heights);
        }
    }    

    public class TerrainManager : Singleton<TerrainManager>
    {
        public GameObject slopeBuilderPrefab;

        /// <summary>
        /// For each terrain, maps each position on the heightmap to a boolean value that tells if it has something built over it or not
        /// </summary>
        public Dictionary<Terrain, bool[,]> untouchedTerrainMap = new();

        public List<SlopeChange> slopeModifiers = new();

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
                }
                else
                {
                    UIManager.Instance.GetSidebar().SlopeButtonEnabled = false;
                }             
            }
        }

        /// <summary>
        /// For all active terrains, sets the terrain (apart from occupied positions) to a given Height.
        /// </summary>
        /// <param name="height">The terrain Height to set</param>
        public void SetHeight(float height)
        {
            foreach (Terrain terrain in GetAllActiveTerrains())
            {
                if (!untouchedTerrainMap.ContainsKey(terrain))
                {
                    untouchedTerrainMap[terrain] = new bool[terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution];
                }

                float heightMapValue = WorldUnitsToHeightmapUnits(height, terrain);

                float[,] heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
                for (int i = 0; i < terrain.terrainData.heightmapResolution; i++)
                {
                    for (int j = 0; j < terrain.terrainData.heightmapResolution; j++)
                    {                        
                        if (!untouchedTerrainMap[terrain][i, j])
                        {
                            heights[i, j] = heightMapValue;
                        }                        
                    }
                }
                terrain.terrainData.SetHeights(0, 0, heights);
            }
        }

        public SlopePositionHighlighter StartSlopeBuild()
        {
            ILineElement lastLineElement = Line.Instance.GetLastLineElement();

            GameObject builder = Instantiate(slopeBuilderPrefab, lastLineElement.GetEndPoint() + lastLineElement.GetRideDirection().normalized, SlopePositionHighlighter.GetRotationForDirection(lastLineElement.GetRideDirection()));
            builder.transform.SetParent(transform);

            return builder.GetComponent<SlopePositionHighlighter>();
        }

        public void AddSlope(SlopeChange slope)
        {
            slopeModifiers.Add(slope);
            ActiveSlope = slope;
        }        

        public void UnmarkOccupiedTerrain(Terrain terrain, IEnumerable<int2> coordinates)
        {
            foreach (var coord in coordinates)
            {
                // Ensure coordinates are within bounds
                if (coord.x >= 0 && coord.x < terrain.terrainData.heightmapResolution &&
                    coord.y >= 0 && coord.y < terrain.terrainData.heightmapResolution)
                {
                    untouchedTerrainMap[terrain][coord.y, coord.x] = false;
                }
            }
            // TODO redraw terrain afterwards
        }

        public void MarkTerrainAsOccupied(Terrain terrain, IEnumerable<int2> coordinates)
        {
            if (!untouchedTerrainMap.ContainsKey(terrain))
            {
                untouchedTerrainMap[terrain] = new bool[terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution];
            }

            int counter = 0;

            foreach (var coord in coordinates)
            {
                // Ensure coordinates are within bounds
                if (coord.x >= 0 && coord.x < terrain.terrainData.heightmapResolution &&
                    coord.y >= 0 && coord.y < terrain.terrainData.heightmapResolution)
                {
                    untouchedTerrainMap[terrain][coord.y, coord.x] = true;
                    counter++;
                }
            }            
        }      

        /// <summary>
        /// Makes the obstacle sit flush with the terrain under the pointToCheck by adjusting its height and rotation.
        /// If any end of the obstacle is above the terrain, the terrain gets raised to the height of the obstacle.
        /// </summary>
        /// <param name="pivot">Delegate function that returns the current pointToCheck point. The changes in position and rotation are calculated from this point.</param>        
        public static void SitFlushOnTerrain(IObstacleBuilder obstacle, Func<Vector3> getPointToCheck)
        {
            Terrain terrain = obstacle.GetTerrain();

            // Get the current pointToCheck point using the delegate
            Vector3 pointToCheck = getPointToCheck();

            Vector3 normal = GetNormalForWorldPosition(pointToCheck, terrain);

            Quaternion newRotation = Quaternion.FromToRotation(obstacle.GetTransform().up, normal) * obstacle.GetTransform().rotation;

            obstacle.SetRotation(newRotation);

            // Get the UPDATED pointToCheck point after rotation
            pointToCheck = getPointToCheck();

            float terrainHeight = terrain.SampleHeight(new Vector3(pointToCheck.x, 0, pointToCheck.z))
                                      + terrain.transform.position.y;

            float heightDifference = terrainHeight - pointToCheck.y;

            Vector3 newPosition = obstacle.GetTransform().position;
            newPosition.y += heightDifference;
            obstacle.SetPosition(newPosition);            
        }       


        /// <summary>
        /// Gets the heightmap coordinates for a path with a set width between two points.
        /// </summary>
        /// <remarks><b>Iterates through the path from the left corner to the right, row-by-row from start to end.</b></remarks>
        /// <param name="start">Start of the path.</param>
        /// <param name="end">End of the path.</param>
        /// <param name="width">Width of the path.</param>
        public static IEnumerable<int2> GetHeightmapCoordinatesForPath(Vector3 start, Vector3 end, float width)
        {
            Terrain terrain = GetTerrainForPosition(start);
            float heightmapSpacing = GetHeightmapSpacing(terrain);
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
                    int2 heightmapPosition = WorldToHeightmapCoordinates(position, terrain);
                    yield return heightmapPosition;
                }
            }
        }
        
        public static int2 WorldToHeightmapCoordinates(Vector3 worldPosition, Terrain terrain)
        {
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            int heightmapResolution = terrain.terrainData.heightmapResolution;
            // Calculate normalized positions
            float normalizedX = (worldPosition.x - terrainPosition.x) / terrainSize.x;
            float normalizedZ = (worldPosition.z - terrainPosition.z) / terrainSize.z;
            // Convert to heightmap coordinates
            int x = Mathf.Clamp(Mathf.FloorToInt(normalizedX * (heightmapResolution - 1)), 0, heightmapResolution - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt(normalizedZ * (heightmapResolution - 1)), 0, heightmapResolution - 1);
            return new int2(x, z);
        }

        public static Vector3 HeightmapToWorldCoordinates(int2 coord, Terrain terrain)
        {
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            int heightmapResolution = terrain.terrainData.heightmapResolution;
            // Calculate normalized positions
            float normalizedX = (float)coord.x / (heightmapResolution - 1);
            float normalizedZ = (float)coord.y / (heightmapResolution - 1);
            // Convert to world coordinates
            float worldX = terrainPosition.x + normalizedX * terrainSize.x;
            float worldZ = terrainPosition.z + normalizedZ * terrainSize.z;
            return new Vector3(worldX, 0, worldZ);
        }

        public static Vector3 GetNormalForWorldPosition(Vector3 worldPosition, Terrain terrain)
        {
            float x = worldPosition.x / terrain.terrainData.size.x;
            float z = worldPosition.z / terrain.terrainData.size.z;
            return terrain.terrainData.GetInterpolatedNormal(x, z);
        }

        /// <summary>
        /// Translates from a Height in world units to a Height in heightmap units.
        /// </summary>
        public static float WorldUnitsToHeightmapUnits(float worldUnits, Terrain terrain)
        {
            return (worldUnits - terrain.transform.position.y) / terrain.terrainData.size.y;
        }

        /// <summary>
        /// Gets the spacing in world units between heightmap points on a terrain.
        /// </summary>
        /// <returns>The smaller of the two spacings in the X and Z directions.</returns>
        public static float GetHeightmapSpacing(Terrain terrain)
        {
            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainSize = terrainData.size;
            int heightmapResolution = terrainData.heightmapResolution;

            float spacingX = terrainSize.x / (heightmapResolution - 1);
            float spacingZ = terrainSize.z / (heightmapResolution - 1);

            return Mathf.Min(spacingX, spacingZ)/5; // divide to make sure that no heightmap points are missed
        }        

        /// <summary>
        /// Gets all active terrains in the scene.
        /// </summary>
        private static List<Terrain> GetAllActiveTerrains()
        {
            Terrain[] terrains = Terrain.activeTerrains;
            return new List<Terrain>(terrains);
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

        /// <summary>
        /// Finds the terrain that a given position is on.
        /// </summary>
        public static Terrain GetTerrainForPosition(Vector3 position)
        {
            List<Terrain> terrains = GetAllActiveTerrains();

            foreach (Terrain terrain in terrains)
            {
                if (IsPositionOnTerrain(position, terrain))
                {
                    return terrain;
                }
            }

            return null;
        }

        // Use this for initialization
        void Start()
        {
            foreach (Terrain terrain in GetAllActiveTerrains())
            {
                untouchedTerrainMap[terrain] = new bool[terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution];
            }

            // TODO quickfix so that the terrain under rollin is marked as occupied
            Line.Instance.line[0].GetHeightmapCoordinates().MarkAsOccupied();
        }      

        //https://gist.github.com/unitycoder/58f4b5d80f423d29e35c814a9556f9d9
        public static void DrawBoundsGizmos(Bounds b, float delay = 0)
        {
            // bottom
            var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
            var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
            var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
            var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

            Debug.DrawLine(p1, p2, Color.blue, delay);
            Debug.DrawLine(p2, p3, Color.red, delay);
            Debug.DrawLine(p3, p4, Color.yellow, delay);
            Debug.DrawLine(p4, p1, Color.magenta, delay);

            // top
            var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
            var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
            var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
            var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

            Debug.DrawLine(p5, p6, Color.blue, delay);
            Debug.DrawLine(p6, p7, Color.red, delay);
            Debug.DrawLine(p7, p8, Color.yellow, delay);
            Debug.DrawLine(p8, p5, Color.magenta, delay);

            // sides
            Debug.DrawLine(p1, p5, Color.white, delay);
            Debug.DrawLine(p2, p6, Color.gray, delay);
            Debug.DrawLine(p3, p7, Color.green, delay);
            Debug.DrawLine(p4, p8, Color.cyan, delay);
        }
    }
}