using Assets.Scripts.Managers;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders.TakeOff
{
    public class Takeoff : TakeoffBase, ILineElement
    {
        [SerializeField]
        Material material;

        LandingMeshGenerator.Landing landing = null;

        int lineIndex;

        List<int2> pathHeightmapCoordinates = new();        

        public override void Initialize(TakeoffMeshGenerator meshGenerator, Terrain terrain, GameObject cameraTarget, GameObject pathProjector, ILineElement previousLineElement, HeightmapBounds bounds)
        {
            base.Initialize(meshGenerator, terrain, cameraTarget, pathProjector, previousLineElement, bounds);
            meshGenerator.GetComponent<MeshRenderer>().material = material;
            lineIndex = Line.Instance.GetLineLength();
            pathHeightmapCoordinates = TerrainManager.Instance.MarkPathAsOccupied(previousLineElement, this);
        }        

        public Takeoff(int lineIndex, Terrain terrain)
        {
            this.lineIndex = lineIndex;
            //this.meshGenerator = meshGenerator;
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
       
        public int GetIndex() => lineIndex;        

        public void SetLanding(LandingMeshGenerator.Landing landing)
        {
            this.landing = landing;
        }
        
        public TakeoffBuilder Revert()
        {
            Destroy(this);

            TakeoffBuilder builder = gameObject.AddComponent<TakeoffBuilder>();

            builder.Initialize(meshGenerator, terrain, cameraTarget, pathProjector, previousLineElement, heightmapBounds);

            return builder;
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