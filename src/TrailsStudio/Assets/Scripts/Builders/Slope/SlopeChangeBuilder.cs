using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Assets.Scripts.Builders
{
    public class SlopeChangeBuilder : SlopeChangeBase, IBuilder
    {
        public void Initialize(Vector3 start, float heightDifference = 0, float length = 0)
        {
            this.Length = length;
            this.Start = start;
            this.startHeight = start.y;
            this.endHeight = startHeight + heightDifference;
            this.highlight = GetComponent<DecalProjector>();
            previousLineElement = Line.Instance.GetLastLineElement();
            transform.position = Vector3.Lerp(start, start + length * Line.Instance.GetCurrentRideDirection(), 0.5f);         
            UpdateHighlight();
        }

        // TODO check that the slope wont collide with occupied positions on the terrain before setting params below
        public void SetLength(float length)
        {
            this.Length = length;

            UpdateAngle();
            UpdateHighlight();
        }

        protected float UpdateAngle()
        {
            float heightDifference = endHeight - startHeight;
            Angle = 90 * Mathf.Deg2Rad - Mathf.Atan(Length / Mathf.Abs(heightDifference));
            if (heightDifference < 0)
            {
                Angle = -Angle;
            }
            return Angle;
        }

        public void SetHeightDifference(float heightDifference)
        {
            this.endHeight = startHeight + heightDifference;

            UpdateAngle();

            UpdateHighlight();
        }

        public float GetHeightDifference()
        {
            return endHeight - startHeight;
        }

        public SlopeChange Build()
        {
            enabled = false;

            SlopeChange slopeChange = GetComponent<SlopeChange>();
            slopeChange.Initialize(Start, endHeight, Length);      
            slopeChange.enabled = true;

            return slopeChange;
        }

        public float SetPosition(Vector3 position)
        {
            Start = position;
            UpdateHighlight();
            float speedAtStart = Line.Instance.GetLastLineElement().GetExitSpeed();
            speedAtStart = PhysicsManager.CalculateExitSpeed(speedAtStart, Vector3.Distance(Line.Instance.GetLastLineElement().GetEndPoint(), position));
            return PhysicsManager.CalculateExitSpeed(speedAtStart, Length, Angle);

        }

        public void SetRotation(Quaternion rotation)
        {
            highlight.transform.rotation = rotation;
            UpdateHighlight();
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            Vector3 rideDirNormal = Vector3.Cross(rideDirection, Vector3.up).normalized;
            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, rideDirNormal);
            highlight.transform.rotation = rotation;
            UpdateHighlight();
        }

        public Vector3 GetStartPoint()
        {
            return Start;
        }

        public Vector3 GetEndPoint()
        {
            return Start + Length * Line.Instance.GetCurrentRideDirection();
        }

        public Transform GetTransform()
        {
            return highlight.transform;
        }

        public Vector3 GetRideDirection()
        {
            return Line.Instance.GetCurrentRideDirection();
        }

        public void DestroyUnderlyingGameObject()
        {
            Destroy(gameObject);
        }
    }
}