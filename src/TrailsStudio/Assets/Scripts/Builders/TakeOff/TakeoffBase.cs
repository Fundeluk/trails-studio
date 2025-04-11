using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders
{
    public abstract class TakeoffBase : ObstacleBase<TakeoffMeshGenerator>
    {
        protected ILineElement previousLineElement;

        public virtual void Initialize(TakeoffMeshGenerator meshGenerator, Terrain terrain, GameObject cameraTarget, ILineElement previousLineElement)
        {
            base.Initialize(meshGenerator, terrain, cameraTarget);            
            this.previousLineElement = previousLineElement;
        }                

        public override Vector3 GetEndPoint() => GetTransform().position + GetRideDirection().normalized * (meshGenerator.Thickness + GetHeight() * TakeoffMeshGenerator.sideSlope);

        public override Vector3 GetStartPoint() => GetTransform().position - GetRideDirection().normalized * meshGenerator.CalculateRadiusLength();

        public override float GetLength() => meshGenerator.CalculateRadiusLength() + meshGenerator.Thickness + GetHeight() * TakeoffMeshGenerator.sideSlope;

        public float GetRadius() => meshGenerator.Radius;
    }
}