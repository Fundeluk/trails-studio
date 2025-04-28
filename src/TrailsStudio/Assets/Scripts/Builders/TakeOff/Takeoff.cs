using Assets.Scripts.Managers;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders
{
    public class Takeoff : TakeoffBase, ILineElement
    {
        [SerializeField]
        protected GameObject pathProjectorPrefab;

        protected GameObject pathProjector;

        Landing landing = null;

        int lineIndex;

        public override void Initialize(TakeoffMeshGenerator meshGenerator, Terrain terrain, GameObject cameraTarget, ILineElement previousLineElement)
        {
            base.Initialize(meshGenerator, terrain, cameraTarget, previousLineElement);
            meshGenerator.GetComponent<MeshRenderer>().material = material;
            lineIndex = Line.Instance.AddLineElement(this);
            this.pathProjector = Instantiate(pathProjectorPrefab);
            this.pathProjector.transform.SetParent(transform);
            this.transform.SetParent(Line.Instance.transform);
            UpdatePathProjector();
        }        

        protected void UpdatePathProjector()
        {
            Vector3 takeoffStart = transform.position - GetRideDirection().normalized * meshGenerator.CalculateRadiusLength();

            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, GetRideDirection());
            Vector3 position = Vector3.Lerp(previousLineElement.GetEndPoint(), takeoffStart, 0.5f);
            position.y = TerrainManager.maxHeight;
            pathProjector.transform.SetPositionAndRotation(position, rotation);

            float distance = Vector3.Distance(previousLineElement.GetEndPoint(), takeoffStart);
            float width = Mathf.Lerp(previousLineElement.GetBottomWidth(), GetBottomWidth(), 0.5f);

            DecalProjector decalProjector = pathProjector.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(width, distance, 10);
        }

        public int GetIndex() => lineIndex;        

        public void SetLanding(Landing landing)
        {
            this.landing = landing;
        }

        private void RemoveFromHeightmap()
        {
            GetHeightmapCoordinates().UnmarkAsOccupied();
            if (slopeHeightmapCoordinates.HasValue)
            {
                slopeHeightmapCoordinates.Value.UnmarkAsOccupied();
            }
            if (slope != null)
            {
                slope.RemoveWaypoint(this);
            }
        }
        
        public TakeoffBuilder Revert()
        {
            Destroy(pathProjector);
            enabled = false;

            Line.Instance.line.RemoveAt(GetIndex());

            RemoveFromHeightmap();

            TakeoffBuilder builder = GetComponent<TakeoffBuilder>();
            builder.Initialize(meshGenerator, terrain, cameraTarget, previousLineElement);
            BuildManager.Instance.activeBuilder = builder;
            builder.enabled = true;

            TerrainManager.SitFlushOnTerrain(builder, GetStartPoint);

            return builder;
        }

        public List<(string name, string value)> GetLineElementInfo()
        {
            return new List<(string name, string value)>
            {
                ("Type", "Takeoff"),
                ("Radius", $"{GetRadius(),10:0}m"),
                ("Height", $"{GetHeight(),10:0.00}m"),
                ("Length", $"{GetLength(),10:0.00}m"),
                ("Width",$"{GetWidth(),10:0.00}m"),
            };
        }
        

        public override void DestroyUnderlyingGameObject()
        {
            if (landing != null)
            {
                landing.DestroyUnderlyingGameObject();
            }

            Destroy(pathProjector);

            RemoveFromHeightmap();

            base.DestroyUnderlyingGameObject();
        }        
    }
}