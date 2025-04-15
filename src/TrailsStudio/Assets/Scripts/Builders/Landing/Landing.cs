using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Builders
{
    public class Landing : LandingBase, ILineElement
    {
        private int lineIndex;
        
        public override void Initialize(LandingMeshGenerator meshGenerator, Terrain terrain, GameObject cameraTarget, ILineElement previousLineElement)
        {
            base.Initialize(meshGenerator, terrain, cameraTarget, previousLineElement);
            meshGenerator.GetComponent<MeshRenderer>().material = material;
            takeoff = Line.Instance.GetLastLineElement() as Takeoff;
            lineIndex = Line.Instance.AddLineElement(this);
            this.transform.SetParent(Line.Instance.transform);
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