using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Builders
{
    public class Landing : LandingBase, ILineElement
    {
        private int lineIndex;

        public override void Initialize(LandingMeshGenerator meshGenerator, Terrain terrain, GameObject cameraTarget, HeightmapBounds bounds)
        {
            base.Initialize(meshGenerator, terrain, cameraTarget, bounds);
            meshGenerator.GetComponent<MeshRenderer>().material = material;
            takeoff = Line.Instance.GetLastLineElement() as Takeoff;
            lineIndex = Line.Instance.AddLineElement(this);
        }        

        public override void DestroyUnderlyingGameObject()
        {
            Line.Instance.line.RemoveAt(GetIndex());
            takeoff.SetLanding(null);
            base.DestroyUnderlyingGameObject();
        }

        public int GetIndex() => lineIndex;        

    }

}