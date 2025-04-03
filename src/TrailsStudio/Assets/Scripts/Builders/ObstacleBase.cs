using Assets.Scripts.Managers;
using System.Collections;
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

        protected HeightmapBounds bounds;

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


        public virtual void Initialize(T meshGenerator, Terrain terrain, GameObject cameraTarget, HeightmapBounds bounds)
        {
            this.meshGenerator = meshGenerator;
            this.terrain = terrain;
            this.cameraTarget = cameraTarget;            
            this.bounds = bounds;
        }

        public void RecalculateCameraTargetPosition()
        {
            cameraTarget.transform.position = Vector3.Lerp(GetStartPoint(), GetEndPoint(), 0.5f) + (0.5f * GetHeight() * GetTransform().up);
        }

        public void RecalculateHeightmapBounds()
        {
            Bounds bounds = new(meshGenerator.transform.position, Vector3.zero);

            bounds.Encapsulate(GetEndPoint());
            Vector3 startPoint = GetEndPoint() - GetRideDirection() * GetLength();
            bounds.Encapsulate(startPoint);

            Vector3 sideDirection = Vector3.Cross(GetRideDirection(), Vector3.down);

            bounds.Encapsulate(GetEndPoint() + sideDirection * GetBottomWidth() / 2);
            bounds.Encapsulate(GetEndPoint() - sideDirection * GetBottomWidth() / 2);

            bounds.Encapsulate(startPoint + sideDirection * GetBottomWidth() / 2);
            bounds.Encapsulate(startPoint - sideDirection * GetBottomWidth() / 2);

            bounds.size = new Vector3(bounds.size.x + 0.5f, bounds.size.y, bounds.size.z + 0.5f);

            //TerrainManager.DrawBoundsGizmos(bounds, 20);
            //Debug.Log("Takeoff bounds: " + bounds);
            this.bounds = TerrainManager.BoundsToHeightmapBounds(bounds, terrain);
            //TerrainManager.DebugRaiseBoundCorners(bounds, 10f);
        }

        public virtual void DestroyUnderlyingGameObject()
        {
            TerrainManager.Instance.UnmarkOccupiedTerrain(bounds);
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

        public HeightmapBounds GetHeightmapBounds() => bounds;      
    }
}