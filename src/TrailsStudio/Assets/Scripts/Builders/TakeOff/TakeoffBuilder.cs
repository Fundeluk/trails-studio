using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Builders.TakeOff
{
    public class TakeoffBuilder : MonoBehaviour
    {
        private readonly TakeoffMeshGenerator meshGenerator;

        public void SetHeight(float height)
        {
            meshGenerator.height = height;
            meshGenerator.GenerateTakeoffMesh();
        }
        public void SetWidth(float width)
        {
            meshGenerator.width = width;
            meshGenerator.GenerateTakeoffMesh();
        }
        public void SetThickness(float thickness)
        {
            meshGenerator.thickness = thickness;
            meshGenerator.GenerateTakeoffMesh();
        }
        public void SetRadius(float radius)
        {
            meshGenerator.radius = radius;
            meshGenerator.GenerateTakeoffMesh();
        }
    }
}