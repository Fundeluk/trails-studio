using UnityEngine;

namespace TerrainEditing
{
    [RequireComponent(typeof(Terrain), typeof(TerrainCollider))]
    public class TerrainChunkSetup : MonoBehaviour
    {
        void Awake()
        {
            InitializeTerrain();
        }

        private void InitializeTerrain()
        {
            Terrain terrain = GetComponent<Terrain>();
            TerrainCollider terrainCollider = GetComponent<TerrainCollider>();

            // 1. CLONE the TerrainData so this chunk is completely independent
            // If you don't do this, editing this chunk will edit your original prefab asset!
            TerrainData originalData = terrain.terrainData;
            TerrainData uniqueData = Instantiate(originalData);
            
            // Reassign the unique data to both the terrain and the collider
            terrain.terrainData = uniqueData;
            terrainCollider.terrainData = uniqueData;

            // 2. Flatten the heightmap to 0.5
            int res = uniqueData.heightmapResolution;
            float[,] heights = new float[res, res];
            
            float normalizedGlobalHeight = TerrainManager.WorldHeightToHeightmapHeight(TerrainManager.Instance.GlobalHeightLevel);
            
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    heights[y, x] = normalizedGlobalHeight;
                }
            }
            
            // Apply the heights. (0,0) is the starting X,Y coordinate on the heightmap.
            uniqueData.SetHeights(0, 0, heights);
        }
    }
}