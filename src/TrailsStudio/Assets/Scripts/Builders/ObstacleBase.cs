using Assets.Scripts.Managers;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.Builders
{
    public abstract class ObstacleBase<T> : MonoBehaviour where T : MeshGeneratorBase
    {
        [SerializeField]
        protected T meshGenerator;

        [SerializeField]
        protected Material material;

        protected Terrain terrain;

        protected GameObject cameraTarget;

        /// <summary>
        /// If the obstacle is built on a slope, this is the set of coordinates that are occupied as a result of the build.
        /// </summary>
        protected HeightmapCoordinates? slopeHeightmapCoordinates = null;

        public virtual void Initialize()
        {
            if (meshGenerator == null)
            {
                meshGenerator = GetComponent<T>();
            }

            terrain = TerrainManager.GetTerrainForPosition(transform.position);
            cameraTarget = new GameObject("Camera Target");
            cameraTarget.transform.SetParent(transform);
        }


        public virtual void Initialize(T meshGenerator, Terrain terrain, GameObject cameraTarget)
        {
            this.meshGenerator = meshGenerator;
            this.terrain = terrain;
            this.cameraTarget = cameraTarget;            
        }

        /// <summary>
        /// Adds the coordinates of the heightmap that are occupied by the slope to <see cref="slopeHeightmapCoordinates"/> and marks them as occuppied.
        /// </summary>
        public void AddSlopeHeightmapCoords(HeightmapCoordinates coords)
        {
            if (!slopeHeightmapCoordinates.HasValue)
            {
                slopeHeightmapCoordinates = new HeightmapCoordinates(GetTerrain(), coords);
            }
            else
            {
                slopeHeightmapCoordinates.Value.Add(coords);
            }

            coords.MarkAsOccupied();
        }

        public void RecalculateCameraTargetPosition()
        {
            cameraTarget.transform.position = Vector3.Lerp(GetStartPoint(), GetEndPoint(), 0.5f) + (0.5f * GetHeight() * GetTransform().up);
        }        

        public virtual void DestroyUnderlyingGameObject()
        {
            // TODO fix terrain unmarking
            //TerrainManager.Instance.UnmarkOccupiedTerrain(GetTerrain(), GetHeightmapCoordinates());
            Destroy(cameraTarget);
            Destroy(meshGenerator.gameObject);
        }

        public abstract Vector3 GetEndPoint();

        public abstract Vector3 GetStartPoint();

        public abstract float GetLength();

        public Terrain GetTerrain() => terrain;

        public float GetBottomWidth() => meshGenerator.Width + 2 * meshGenerator.Height * TakeoffMeshGenerator.sideSlope;

        public float GetHeight() => meshGenerator.Height;

        public float GetThickness() => meshGenerator.Thickness;

        public float GetWidth() => meshGenerator.Width;

        public Vector3 GetRideDirection() => meshGenerator.transform.forward.normalized;

        public Transform GetTransform() => meshGenerator.transform;

        public GameObject GetCameraTarget() => cameraTarget;

        public HeightmapCoordinates GetHeightmapCoordinates() => new HeightmapCoordinates(GetStartPoint(), GetEndPoint(), GetBottomWidth());      
    }
}