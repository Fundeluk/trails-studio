using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders.TakeOff
{
    public abstract class TakeoffBase : MonoBehaviour
    {
        [SerializeField]
        protected TakeoffMeshGenerator meshGenerator;

        [SerializeField]
        protected Terrain terrain;

        [SerializeField]
        protected GameObject cameraTarget;

        [SerializeField]
        protected GameObject pathProjector;

        protected ILineElement previousLineElement;

        protected HeightmapBounds heightmapBounds;

        public virtual void Initialize(TakeoffMeshGenerator meshGenerator, Terrain terrain, GameObject cameraTarget, GameObject pathProjector, ILineElement previousLineElement, HeightmapBounds bounds)
        {
            this.meshGenerator = meshGenerator;
            this.terrain = terrain;
            this.cameraTarget = cameraTarget;
            this.pathProjector = pathProjector;
            this.previousLineElement = previousLineElement;
            heightmapBounds = bounds;
        }

        protected void UpdatePathProjector()
        {
            Vector3 takeoffStart = transform.position - GetRideDirection().normalized * meshGenerator.CalculateRadiusLength();

            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, GetRideDirection());
            pathProjector.transform.SetPositionAndRotation(Vector3.Lerp(previousLineElement.GetEndPoint(), takeoffStart, 0.5f) + Vector3.up, rotation);

            float distance = Vector3.Distance(previousLineElement.GetEndPoint(), takeoffStart);
            float width = Mathf.Lerp(previousLineElement.GetBottomWidth(), GetBottomWidth(), 0.5f);

            DecalProjector decalProjector = pathProjector.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(width, distance, 10);
        }

        protected void RecalculateCameraTargetPosition()
        {
            cameraTarget.transform.position = GetTransform().position + (0.5f * GetHeight() * GetTransform().up);
        }

        protected void RecalculateHeightmapBounds()
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
            heightmapBounds = TerrainManager.BoundsToHeightmapBounds(bounds, terrain);
            //TerrainManager.DebugRaiseBoundCorners(heightmapBounds, 10f);
        }

        public Terrain GetTerrain() => terrain;

        public float GetBottomWidth() => meshGenerator.Width + 2 * meshGenerator.Height * TakeoffMeshGenerator.sideSlope;

        public Vector3 GetEndPoint() => GetTransform().position + GetRideDirection().normalized * (meshGenerator.Thickness + GetHeight() * TakeoffMeshGenerator.sideSlope);

        public Vector3 GetStartPoint() => GetTransform().position - GetRideDirection().normalized * meshGenerator.CalculateRadiusLength();
        public float GetHeight() => meshGenerator.Height;

        public float GetLength() => meshGenerator.CalculateRadiusLength() + meshGenerator.Thickness + GetHeight() * TakeoffMeshGenerator.sideSlope;

        public float GetThickness() => meshGenerator.Thickness;

        public float GetWidth() => meshGenerator.Width;

        public float GetRadius() => meshGenerator.Radius;

        public Vector3 GetRideDirection() => meshGenerator.transform.forward.normalized;

        public Transform GetTransform() => meshGenerator.transform;

        public GameObject GetCameraTarget() => cameraTarget;

        public HeightmapBounds GetHeightmapBounds() => heightmapBounds;        
    }
}