using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Builders
{
    public interface IBuilder
    {
        public float SetPosition(Vector3 position);

        public void SetRotation(Quaternion rotation);

        public void SetRideDirection(Vector3 rideDirection);

        public Vector3 GetStartPoint();

        public Vector3 GetEndPoint();

        public Transform GetTransform();

        public Vector3 GetRideDirection();

        public void CanBuild(bool canBuild);

        public void DestroyUnderlyingGameObject();
    }

    public interface IObstacleBuilder : IBuilder
    {
        public void SetHeight(float height);

        public void SetWidth(float width);

        public void SetThickness(float thickness);        

        public void RecalculateCameraTargetPosition();

        public HeightmapCoordinates GetObstacleHeightmapCoordinates();
    }
}