using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders.TakeOff
{
    public class TakeoffBuilder : TakeoffBase, IBuilder
    {
        // Default values for new takeoffs
        [Header("Default Parameters")]
        [SerializeField] float defaultHeight = 2.0f;
        [SerializeField] float defaultWidth = 3.0f;
        [SerializeField] float defaultThickness = 0.5f;
        [SerializeField] float defaultRadius = 2.0f;
        [SerializeField] int defaultResolution = 10;

        [SerializeField] Material material;
        
        void Awake()
        {
            if (meshGenerator == null)
            {
                meshGenerator.GetComponent<TakeoffMeshGenerator>();
            }           

            Initialize();
        }

        public void Initialize()
        {
            meshGenerator.GetComponent<MeshRenderer>().material = material;
            meshGenerator.SetBatch(defaultHeight, defaultWidth, defaultThickness, defaultRadius, defaultResolution);
            BuildManager.Instance.activeBuilder = this;
        }

        public void SetHeight(float height)
        {
            meshGenerator.Height = height;
            meshGenerator.GenerateTakeoffMesh();
            RecalculateCameraTargetPosition();
            UpdatePathProjector();
        }

        public void SetWidth(float width)
        {
            meshGenerator.Width = width;
            meshGenerator.GenerateTakeoffMesh();
            UpdatePathProjector();
            RecalculateHeightmapBounds();
        }

        public void SetThickness(float thickness)
        {
            meshGenerator.Thickness = thickness;
            meshGenerator.GenerateTakeoffMesh();
            RecalculateCameraTargetPosition();
            RecalculateHeightmapBounds();
        }

        public void SetRadius(float radius)
        {
            meshGenerator.Radius = radius;
            meshGenerator.GenerateTakeoffMesh();
            UpdatePathProjector();
            RecalculateHeightmapBounds();
        }

        public void SetPosition(Vector3 position)
        {
            meshGenerator.transform.position = position;
            RecalculateCameraTargetPosition();
            UpdatePathProjector();
            RecalculateHeightmapBounds();
        }

        public void SetRotation(Quaternion rotation)
        {
            meshGenerator.transform.rotation = rotation;
            RecalculateCameraTargetPosition();
            UpdatePathProjector();
            RecalculateHeightmapBounds();
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            transform.forward = rideDirection;
            meshGenerator.GenerateTakeoffMesh();
            UpdatePathProjector();
            RecalculateHeightmapBounds();
        }


        /// <returns>The distance between the start point and the takeoff edge.</returns>
        public float GetCurrentRadiusLength()
        {
            return (transform.position - GetStartPoint()).magnitude;
        }


        public Takeoff Build()
        {
            Destroy(this);

            Takeoff takeoff = gameObject.AddComponent<Takeoff>();

            takeoff.Initialize(meshGenerator, terrain, cameraTarget, pathProjector, previousLineElement, heightmapBounds);

            BuildManager.Instance.activeBuilder = null;

            return takeoff;
        }

        public void Cancel()
        {
            BuildManager.Instance.activeBuilder = null;
            Destroy(gameObject);
        }
    }
}