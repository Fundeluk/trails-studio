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

            // clone the TerrainData so this chunk is completely independent
            // otherwise this chunk will edit original prefab asset
            TerrainData originalData = terrain.terrainData;
            TerrainData uniqueData = Instantiate(originalData);
        
            // reassign the unique data to both the terrain and the collider
            terrain.terrainData = uniqueData;
            terrainCollider.terrainData = uniqueData;

            // flatten the heightmap to the global height level
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
        
            uniqueData.SetHeights(0, 0, heights);
        }
    }
}
    
