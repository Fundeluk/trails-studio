﻿using Assets.Scripts.Managers;
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

        List<int2> pathHeightmapCoordinates = new();        

        public override void Initialize(TakeoffMeshGenerator meshGenerator, Terrain terrain, GameObject cameraTarget, ILineElement previousLineElement, HeightmapBounds bounds)
        {
            base.Initialize(meshGenerator, terrain, cameraTarget, previousLineElement, bounds);
            meshGenerator.GetComponent<MeshRenderer>().material = material;
            lineIndex = Line.Instance.AddLineElement(this);
            this.pathProjector = Instantiate(pathProjectorPrefab);
            this.pathProjector.transform.SetParent(transform);
            UpdatePathProjector();
            pathHeightmapCoordinates = TerrainManager.Instance.MarkPathAsOccupied(previousLineElement, this);
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

        public void SetPath(List<int2> pathHeightmapCoordinates)
        {
            this.pathHeightmapCoordinates = pathHeightmapCoordinates;
        }

        public int GetIndex() => lineIndex;        

        public void SetLanding(Landing landing)
        {
            this.landing = landing;
        }
        
        public TakeoffBuilder Revert()
        {
            Destroy(pathProjector);
            enabled = false;

            Line.Instance.line.RemoveAt(GetIndex());

            TakeoffBuilder builder = GetComponent<TakeoffBuilder>();
            builder.Initialize(meshGenerator, terrain, cameraTarget, previousLineElement, bounds);
            BuildManager.Instance.activeBuilder = builder;
            builder.enabled = true;

            return builder;
        }        

        public override void DestroyUnderlyingGameObject()
        {
            if (landing != null)
            {
                landing.DestroyUnderlyingGameObject();
            }

            Line.Instance.line.RemoveAt(GetIndex());

            Destroy(pathProjector);
            base.DestroyUnderlyingGameObject();
        }        
    }
}