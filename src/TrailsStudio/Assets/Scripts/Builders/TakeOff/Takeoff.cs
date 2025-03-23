using Assets.Scripts.Managers;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders.TakeOff
{
    public class Takeoff : MonoBehaviour, ILineElement
    {
        private readonly TakeoffMeshGenerator meshGenerator;

        private readonly GameObject cameraTarget;

        private LandingMeshGenerator.Landing landing = null;

        private readonly GameObject pathProjector;

        private readonly Terrain terrain;

        private HeightmapBounds heightmapBounds;

        private int lineIndex;

        private readonly ILineElement previousLineElement;

        private readonly List<int2> pathHeightmapCoordinates;

        private void UpdatePathProjector()
        {
            Vector3 takeoffStart = GetTransform().position - GetRideDirection().normalized * meshGenerator.CalculateRadiusLength();

            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, GetRideDirection());
            pathProjector.transform.SetPositionAndRotation(Vector3.Lerp(previousLineElement.GetEndPoint(), takeoffStart, 0.5f) + Vector3.up, rotation);

            float distance = Vector3.Distance(previousLineElement.GetEndPoint(), takeoffStart);
            float width = Mathf.Lerp(previousLineElement.GetBottomWidth(), GetBottomWidth(), 0.5f);

            DecalProjector decalProjector = pathProjector.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(width, distance, 10);
        }

        private void RecalculateCameraTargetPosition()
        {
            cameraTarget.transform.position = GetTransform().position + (0.5f * GetHeight() * GetTransform().up);
        }

        public Takeoff(int lineIndex, Terrain terrain)
        {
            this.lineIndex = lineIndex;
            this.meshGenerator = meshGenerator;
            cameraTarget = new GameObject("Camera Target");
            cameraTarget.transform.SetParent(meshGenerator.transform);
            RecalculateCameraTargetPosition();

            previousLineElement = Line.Instance.GetLastLineElement();

            pathProjector = Instantiate(Line.Instance.pathProjectorPrefab);
            pathProjector.transform.SetParent(meshGenerator.transform);

            UpdatePathProjector();

            this.terrain = terrain;

            pathHeightmapCoordinates = TerrainManager.Instance.MarkPathAsOccupied(previousLineElement, this);

            RecalculateHeightmapBounds();
        }

        public HeightmapBounds GetHeightmapBounds() => heightmapBounds;

        public Terrain GetTerrain() => terrain;


        public int GetIndex() => lineIndex;

        private void RecalculateHeightmapBounds()
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

        public float GetBottomWidth() => meshGenerator.width + 2 * meshGenerator.height * TakeoffMeshGenerator.sideSlope;

        public Vector3 GetEndPoint() => GetTransform().position + GetRideDirection().normalized * (meshGenerator.thickness + GetHeight() * TakeoffMeshGenerator.sideSlope);

        public Vector3 GetStartPoint() => GetTransform().position - GetRideDirection().normalized * meshGenerator.CalculateRadiusLength();
        public float GetHeight() => meshGenerator.height;

        public float GetLength() => meshGenerator.CalculateRadiusLength() + meshGenerator.thickness + GetHeight() * TakeoffMeshGenerator.sideSlope;

        public Vector3 GetRideDirection() => meshGenerator.transform.forward.normalized;

        public Transform GetTransform() => meshGenerator.transform;

        public GameObject GetCameraTarget() => cameraTarget;

        public void SetEndPoint(Vector3 endPoint)
        {
            throw new System.InvalidOperationException("Cannot set end point of takeoff.");
        }

        public void SetHeight(float height)
        {
            meshGenerator.height = height;
            meshGenerator.GenerateTakeoffMesh();
            RecalculateCameraTargetPosition();
            UpdatePathProjector();
        }

        public void SetLanding(LandingMeshGenerator.Landing landing)
        {
            this.landing = landing;
        }

        public void SetLength(float length)
        {
            // TODO it may make sense in the future to edit the takeoff by changing supposed length
            throw new System.InvalidOperationException("Cannot set length of takeoff.");
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            meshGenerator.transform.forward = rideDirection;
            RecalculateHeightmapBounds();
        }

        public float GetRadius() => meshGenerator.radius;

        public void SetRadius(float radius)
        {
            meshGenerator.radius = radius;
            meshGenerator.GenerateTakeoffMesh();
            UpdatePathProjector();
            RecalculateHeightmapBounds();
        }

        public float GetWidth() => meshGenerator.width;

        public void SetWidth(float width)
        {
            meshGenerator.width = width;
            meshGenerator.GenerateTakeoffMesh();
            UpdatePathProjector();
            RecalculateHeightmapBounds();
        }

        public float GetThickness() => meshGenerator.thickness;

        public void SetThickness(float thickness)
        {
            meshGenerator.thickness = thickness;
            meshGenerator.GenerateTakeoffMesh();
            RecalculateCameraTargetPosition();
            RecalculateHeightmapBounds();
        }

        public void DestroyUnderlyingGameObject()
        {
            TerrainManager.Instance.UnmarkOccupiedTerrain(pathHeightmapCoordinates, terrain);
            landing?.DestroyUnderlyingGameObject();
            Destroy(pathProjector);
            Destroy(cameraTarget);
            Destroy(meshGenerator.gameObject);
        }
    }

}