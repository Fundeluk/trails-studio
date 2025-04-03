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
    

    public class TerrainManager : Singleton<TerrainManager>
    {
        public GameObject slopeBuilderPrefab;

        /// <summary>
        /// For each terrain, maps each position on the heightmap to a boolean value that tells if it has something built over it or not
        /// </summary>
        public Dictionary<Terrain, bool[,]> untouchedTerrainMap = new();

        public List<SlopeChange> slopeModifiers = new();

        private SlopeChange activeSlope = null;

        public SlopeChange GetActiveSlope()
        {
            return activeSlope;
        }

        public void SetActiveSlope(SlopeChange slope)
        {
            if (slope == null)
            {
                UIManager.Instance.ToggleSlopeButton(true);
            }
            else
            {
                UIManager.Instance.ToggleSlopeButton(false);
            }

            activeSlope = slope;
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
            BuildManager.Instance.activeSlopeChange = slope;
        }

        /// <summary>
        /// Marks a path from start to end as occupied.
        /// </summary>
        /// <returns>A list of heightmap coordinates that are affected by the path.</returns>
        public List<int2> MarkPathAsOccupied(ILineElement start, ILineElement end) 
        {
            Vector3 startPos = start.GetEndPoint();
            Vector3 endPos = end.GetStartPoint();

            float width = Mathf.Max(start.GetBottomWidth(), end.GetBottomWidth());
            Terrain terrain = GetTerrainForPosition(start.GetTransform().position);
            float length = Vector3.Distance(startPos, endPos);
            Vector3 rideDir = (endPos - startPos).normalized;
            Vector3 rideDirNormal = Vector3.Cross(rideDir, Vector3.up).normalized;

            List<int2> affectedCoordinates = new();

            Vector3 leftStartCorner = startPos - 0.5f * width * rideDirNormal;

            // TODO account for a span of multiple terrains

            float heightmapSpacing = GetHeightmapSpacing(terrain);
            int widthSteps = Mathf.CeilToInt(width / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(length / heightmapSpacing);

            for (int i = 0; i <= lengthSteps; i++)
            {
                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 position = leftStartCorner + j * heightmapSpacing * rideDirNormal + i * heightmapSpacing * rideDir;

                    int2 heightmapPosition = WorldToHeightmapCoordinates(position, terrain);
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
            // TODO redraw terrain afterwards
        }

        public void UnmarkOccupiedTerrain(HeightmapBounds bounds)
        {
            for (int i = bounds.startX; i < bounds.startX + bounds.width; i++)
            {
                for (int j = bounds.startZ; j < bounds.startZ + bounds.height; j++)
                {
                    untouchedTerrainMap[bounds.terrain][j, i] = false;
                }
            }
            // TODO redraw terrain afterwards
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

        public static void SitOnSlope(ObstacleBase<TakeoffMeshGenerator> obstacle, Terrain terrain)
        {
            float terrainHeight = terrain.SampleHeight(new Vector3(obstacle.GetTransform().position.x, 0, obstacle.GetTransform().position.z))
                                      + terrain.transform.position.y;

            Vector3 newPosition = obstacle.GetTransform().position;
            newPosition.y = terrainHeight;
            obstacle.GetTransform().position = newPosition;

            Vector3 normal = TerrainManager.GetNormalForWorldPosition(obstacle.GetStartPoint(), terrain);

            Quaternion newRotation = Quaternion.FromToRotation(obstacle.GetTransform().up, normal) * obstacle.GetTransform().rotation;

            obstacle.GetTransform().rotation = newRotation;

            obstacle.RecalculateCameraTargetPosition();
            obstacle.RecalculateHeightmapBounds();
        }

        public static void SitOnSlope(ObstacleBase<LandingMeshGenerator> obstacle, Terrain terrain)
        {
            float terrainHeight = terrain.SampleHeight(new Vector3(obstacle.GetTransform().position.x, 0, obstacle.GetTransform().position.z))
                                      + terrain.transform.position.y;

            Vector3 newPosition = obstacle.GetTransform().position;
            newPosition.y = terrainHeight;
            obstacle.GetTransform().position = newPosition;

            Vector3 normal = TerrainManager.GetNormalForWorldPosition(obstacle.GetEndPoint(), terrain);

            Quaternion newRotation = Quaternion.FromToRotation(obstacle.GetTransform().up, normal) * obstacle.GetTransform().rotation;

            obstacle.GetTransform().rotation = newRotation;

            obstacle.RecalculateCameraTargetPosition();
            obstacle.RecalculateHeightmapBounds();
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

        public static HeightmapBounds BoundsToHeightmapBounds(Bounds bounds, Terrain terrain)
        {
            HeightmapBounds heightmapBounds = new()
            {
                terrain = terrain
            };
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