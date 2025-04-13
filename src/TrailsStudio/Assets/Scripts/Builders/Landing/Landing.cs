﻿using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Builders
{
    public class Landing : LandingBase, ILineElement
    {
        private int lineIndex;

        public void OnDrawGizmos()
        {
            Vector3 rideDirNormal = Vector3.Cross(GetRideDirection(), Vector3.up).normalized;
            Gizmos.color = Color.red;
            Gizmos.DrawCube(GetStartPoint() - rideDirNormal * GetBottomWidth() / 2, Vector3.one * 0.5f);
            Gizmos.DrawCube(GetStartPoint() + rideDirNormal * GetBottomWidth() / 2, Vector3.one * 0.5f);
        }

        public override void Initialize(LandingMeshGenerator meshGenerator, Terrain terrain, GameObject cameraTarget)
        {
            base.Initialize(meshGenerator, terrain, cameraTarget);
            meshGenerator.GetComponent<MeshRenderer>().material = material;
            takeoff = Line.Instance.GetLastLineElement() as Takeoff;
            lineIndex = Line.Instance.AddLineElement(this);
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


        public override void DestroyUnderlyingGameObject()
        {
            Line.Instance.line.RemoveAt(GetIndex());
            takeoff.SetLanding(null);
            RemoveFromHeightmap();
            base.DestroyUnderlyingGameObject();
        }

        public int GetIndex() => lineIndex;        

    }

}