using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using Assets.Scripts.Utilities;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Rectangular subset of a terrain heightmap.
    /// </summary>
    public struct HeightmapBounds
    {
        public Terrain terrain;

        public int startX;
        public int startZ;
        /// <summary>
        /// X axis
        /// </summary>
        public int width;
        /// <summary>
        /// Z axis
        /// </summary>
        public int height;
    }

    public class SlopeChange
    {        
        // heightmap bounds is basically an axis aligned bounding square in the heightmap
        // that is useful because any data passed to or from the heightmap is in form of a square subset of the heightmap.
        // the heightmap coordinates that the slope affects may be a subset of that bounding box, so we need to store that subset as well.
        public Dictionary<Terrain, List<int2>> affectedTerrainCoordinates = new(); // <- coordinates in terrain heightmaps that are actually affected by the change

        // WORLD UNITS
        public float angle; // angle of the slope
        public float startHeight;
        public float endHeight;
        public float length;
        public float width;
        public Vector3 rideDirection;
        public readonly Vector3 startPoint;
        public readonly Vector3 endPoint;

        /// <summary>
        /// Creates a slope with a given angle between two points.
        /// </summary>
        /// <param name="angle">Angle in degrees. Negative value means downwards slope, positive upwards.</param>
        public SlopeChange(float angle, Vector3 start, Vector3 end, float width)
        {
            this.length = Vector3.Distance(start, end);
            this.startHeight = start.y;
            this.angle = angle;
            if (angle < 0)
            {
                this.endHeight = startHeight - Mathf.Tan(-angle * Mathf.Deg2Rad) * length;
            }
            else if (angle > 0)
            {
                this.endHeight = startHeight + Mathf.Tan(angle * Mathf.Deg2Rad) * length;
            }
            else
            {
                Debug.LogError("Angle must be non-zero.");
            }
            this.startPoint = start;
            this.endPoint = end;
            this.rideDirection = (end - start).normalized;
            this.width = width;
        }

        /// <summary>
        /// Creates a slope with a given height difference between two points.
        /// </summary>
        /// <param name="heightDifference">Height difference between endpoints in metres. Negative value means downwards slope, positive upwards.</param>
        public SlopeChange(Vector3 start, Vector3 end, float heightDifference, float width)
        {
            this.startHeight = start.y;
            this.endHeight = startHeight + heightDifference;
            this.startPoint = start;
            this.endPoint = end;
            this.rideDirection = (end - start).normalized;
            this.length = Vector3.Distance(start, end);
            this.width = width;
        }

        public void SetHeightDifference(float heightDifference)
        {
            this.endHeight = startHeight + heightDifference;
        }

        public void ChangeTerrain()
        {
            Terrain startTerrain = TerrainManager.GetTerrainForPosition(startPoint);
            Terrain endTerrain = TerrainManager.GetTerrainForPosition(endPoint);
            
            Terrain terrain;
            if (startTerrain == endTerrain)
            {
                terrain = startTerrain;
            }
            else
            {
                //TODO handle multiple terrains
                terrain = startTerrain;
            }
            
            Vector3 rideDirNormal = Vector3.Cross(rideDirection, Vector3.up);

            Vector3 leftStartCorner = startPoint - 0.5f * width * rideDirNormal;        

            // TODO account for a span of multiple terrains

            affectedTerrainCoordinates[terrain] = new List<int2>();

            float[,] heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
            float heightmapSpacing = TerrainManager.GetHeightmapSpacing(terrain);
            int widthSteps = Mathf.CeilToInt(width / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(length / heightmapSpacing);

            for (int i = 0; i <= lengthSteps; i++)
            {
                float heightAtWidth = startHeight + (endHeight - startHeight) * (i / (float)lengthSteps); // world units

                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 position = leftStartCorner + j * heightmapSpacing * rideDirNormal + i * heightmapSpacing * rideDirection;
                    
                    int2 heightmapPosition = TerrainManager.WorldToHeightmapCoordinates(position, terrain);

                    affectedTerrainCoordinates[terrain].Add(heightmapPosition);

                    heights[heightmapPosition.x, heightmapPosition.y] = TerrainManager.WorldUnitsToHeightmapUnits(heightAtWidth, terrain);
                }
            }

            terrain.terrainData.SetHeights(0, 0, heights);
            TerrainManager.Instance.MarkTerrainAsOccupied(terrain, affectedTerrainCoordinates[terrain]);

            // go through all active terrains and adjust the heights so that points with no obstacles over them are the same height as endheight
            TerrainManager.Instance.SetHeight(endHeight);
        }

        public void Undo()
        {
            foreach (var terrain in affectedTerrainCoordinates.Keys)
            {
                float[,] heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
                foreach (var coord in affectedTerrainCoordinates[terrain])
                {
                    heights[coord.x, coord.y] = TerrainManager.WorldUnitsToHeightmapUnits(startHeight, terrain);
                }
                terrain.terrainData.SetHeights(0, 0, heights);
            }
        }
    }

    public class TerrainManager : Singleton<TerrainManager>
    {
        /// <summary>
        /// For each terrain, maps each position on the heightmap to a boolean value that tells if it has something built over it or not
        /// </summary>
        public Dictionary<Terrain, bool[,]> untouchedTerrainMap = new();

        public List<SlopeChange> slopeChanges = new();

        public void AddSlopeChange(SlopeChange change)
        {
            slopeChanges.Add(change);
        }

        /// <summary>
        /// For all active terrains, sets the terrain (apart from occupied positions) to a given height.
        /// </summary>
        /// <param name="height">The terrain height to set</param>
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

        /// <summary>
        /// Marks a path from start to end as occupied.
        /// </summary>
        /// <returns>A list of heightmap coordinates that are affected by the path.</returns>
        public List<int2> MarkPathAsOccupied(Vector3 start, Vector3 end, float width) 
        {
            Terrain terrain = GetTerrainForPosition(start);
            float length = Vector3.Distance(start, end);
            Vector3 rideDir = (end - start).normalized;
            Vector3 rideDirNormal = Vector3.Cross(rideDir, Vector3.up);

            List<int2> affectedCoordinates = new();

            Vector3 leftStartCorner = start - 0.5f * width * rideDirNormal;

            // TODO account for a span of multiple terrains

            float heightmapSpacing = TerrainManager.GetHeightmapSpacing(terrain);
            int widthSteps = Mathf.CeilToInt(width / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(length / heightmapSpacing);

            for (int i = 0; i <= lengthSteps; i++)
            {
                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 position = leftStartCorner + j * heightmapSpacing * rideDirNormal + i * heightmapSpacing * rideDir;

                    int2 heightmapPosition = TerrainManager.WorldToHeightmapCoordinates(position, terrain);
                    affectedCoordinates.Add(heightmapPosition);

                    untouchedTerrainMap[terrain][heightmapPosition.x, heightmapPosition.y] = true;
                }
            }    

            return affectedCoordinates;
        }

        public void UnmarkOccupiedTerrain(List<int2> coords, Terrain terrain)
        {
            foreach (var coord in coords)
            {
                untouchedTerrainMap[terrain][coord.x, coord.y] = false;
            }
        }

        public void MarkTerrainAsOccupied(Terrain terrain, List<int2> affectedCoordinates)
        {
            if (!untouchedTerrainMap.ContainsKey(terrain))
            {
                untouchedTerrainMap[terrain] = new bool[terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution];
            }

            foreach (var coord in affectedCoordinates)
            {
                // Ensure coordinates are within bounds
                if (coord.x >= 0 && coord.x < terrain.terrainData.heightmapResolution &&
                    coord.y >= 0 && coord.y < terrain.terrainData.heightmapResolution)
                {
                    untouchedTerrainMap[terrain][coord.x, coord.y] = true;
                }

            }            
        }


        public void MarkTerrainAsOccupied(HeightmapBounds bounds)
        {
            Debug.Log("Marking terrain as occupied");
            if (!untouchedTerrainMap.ContainsKey(bounds.terrain))
            {
                untouchedTerrainMap[bounds.terrain] = new bool[bounds.terrain.terrainData.heightmapResolution, bounds.terrain.terrainData.heightmapResolution];
            }

            for (int i = bounds.startX; i < bounds.startX + bounds.width; i++)
            {
                for (int j = bounds.startZ; j < bounds.startZ + bounds.height; j++)
                {
                    untouchedTerrainMap[bounds.terrain][j, i] = true;
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
            return new int2(z, x);
        }

        /// <summary>
        /// Translates from a height in world units to a height in heightmap units.
        /// </summary>
        public static float WorldUnitsToHeightmapUnits(float worldUnits, Terrain terrain)
        {
            return worldUnits / terrain.terrainData.size.y;
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

            return Mathf.Min(spacingX, spacingZ)/2; // divide by 2 to make sure that no heightmap points are missed
        }

        public static HeightmapBounds BoundsToHeightmapBounds(Bounds bounds, Terrain terrain)
        {
            HeightmapBounds heightmapBounds = new();
            heightmapBounds.terrain = terrain;
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;

            int heightmapResolution = terrain.terrainData.heightmapResolution;

            // Calculate normalized positions
            float normalizedX1 = (bounds.min.x - terrainPosition.x) / terrainSize.x;
            float normalizedX2 = (bounds.max.x - terrainPosition.x) / terrainSize.x;
            float normalizedZ1 = (bounds.min.z - terrainPosition.z) / terrainSize.z;
            float normalizedZ2 = (bounds.max.z - terrainPosition.z) / terrainSize.z;

            // Convert to heightmap coordinates
            heightmapBounds.startX = Mathf.Clamp(Mathf.FloorToInt(normalizedX1 * (heightmapResolution - 1)), 0, heightmapResolution - 1);
            heightmapBounds.startZ = Mathf.Clamp(Mathf.FloorToInt(normalizedZ1 * (heightmapResolution - 1)), 0, heightmapResolution - 1);
            heightmapBounds.width = Mathf.Clamp(Mathf.CeilToInt(normalizedX2 * (heightmapResolution - 1)), 0, heightmapResolution - 1) - heightmapBounds.startX;
            heightmapBounds.height = Mathf.Clamp(Mathf.CeilToInt(normalizedZ2 * (heightmapResolution - 1)), 0, heightmapResolution - 1) - heightmapBounds.startZ;

            return heightmapBounds;
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
        }

        // Update is called once per frame
        void Update()
        {

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

        public static void DebugRaiseBoundCorners(HeightmapBounds bounds, float raiseAmount)
        {
            TerrainData terrainData = bounds.terrain.terrainData;
            float[,] heights = terrainData.GetHeights(bounds.startX, bounds.startZ, bounds.width, bounds.height);

            Debug.Log("Heightmap bounds: startX: " + bounds.startX + ", startZ: " + bounds.startZ + ", lengthX: " + bounds.width + ", lengthZ: " + bounds.height);
            Debug.Log("Heightmap size: X: " + heights.GetUpperBound(0) + ", Z: " + heights.GetUpperBound(1));

            heights[0, 0] += raiseAmount;
            heights[0, bounds.width - 1] += raiseAmount;
            heights[bounds.height - 1, 0] += raiseAmount;
            heights[bounds.height - 1, bounds.width - 1] += raiseAmount;

            terrainData.SetHeights(bounds.startX, bounds.startZ, heights);
        }

    }
}