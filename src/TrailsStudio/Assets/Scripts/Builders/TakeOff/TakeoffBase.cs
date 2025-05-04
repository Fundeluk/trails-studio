using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders
{
    public abstract class TakeoffBase : ObstacleBase<TakeoffMeshGenerator>
    {
        public override void Initialize(TakeoffMeshGenerator meshGenerator, Terrain terrain, GameObject cameraTarget, ILineElement previousLineElement)
        {
            base.Initialize(meshGenerator, terrain, cameraTarget, previousLineElement);            
        }                

        public override Vector3 GetEndPoint() => GetTransform().position + GetRideDirection().normalized * (meshGenerator.Thickness + GetHeight() * GetSideSlope());

        public override Vector3 GetStartPoint() => GetTransform().position - GetRideDirection().normalized * meshGenerator.CalculateRadiusLength();

        public override float GetLength() => meshGenerator.CalculateRadiusLength() + meshGenerator.Thickness + GetHeight() * GetSideSlope();

        public float GetEndAngle() => meshGenerator.GetEndAngle();

        public float GetRadius() => meshGenerator.Radius;
    }
}