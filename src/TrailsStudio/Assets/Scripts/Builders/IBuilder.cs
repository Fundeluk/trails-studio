using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Builders
{
    public interface IBuilder
    {
        public void SetHeight(float height);

        public void SetWidth(float width);

        public void SetThickness(float thickness);

        public void SetPosition(Vector3 position);

        public void SetRotation(Quaternion rotation);

        public void SetRideDirection(Vector3 rideDirection);

        public Vector3 GetStartPoint();

        public Vector3 GetEndPoint();
    }
}