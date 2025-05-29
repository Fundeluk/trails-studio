using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Assets.Scripts.Builders
{
    public class SlopeChangeBuilder : SlopeChangeBase, IBuilder
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            this.Length = 0;
            this.Start = transform.position;
            this.startHeight = Start.y;
            this.endHeight = startHeight;
            width = previousLineElement.GetBottomWidth();
            this.highlight = GetComponent<DecalProjector>();
            previousLineElement = Line.Instance.GetLastLineElement();
            highlight.enabled = true;
            UpdateHighlight();
        }

        protected override void UpdateHighlight()
        {
            if (Length == 0)
            {
                highlight.size = new Vector3(0.1f, width, 20);
            }
            else
            {
                base.UpdateHighlight();
            }
        }

        public void SetLength(float length)
        {
            Vector3 rideDir = Vector3.ProjectOnPlane(Line.Instance.GetCurrentRideDirection(), Vector3.up).normalized;
            if (!IsBuildable(Start, length, rideDir))
            {
                UIManager.Instance.ShowMessage($"Cannot set length to {length}m as the slope would be colliding with another terrain change or an obstacle.");
                return;
            }

            this.Length = length;
            transform.position = Vector3.Lerp(Start, Start + length * rideDir, 0.5f);

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

        public bool IsBuildable(Vector3 start, float length, Vector3 direction)
        {
            return TerrainManager.Instance.IsAreaFree(start, start + length * direction, width);
        }

        public SlopeChange Build()
        {
            enabled = false;

            SlopeChange slopeChange = GetComponent<SlopeChange>();
            slopeChange.Initialize(Start, endHeight, Length);      
            slopeChange.enabled = true;

            return slopeChange;
        }

        /// <summary>
        /// Sets the position of the slope's start point and updates the highlight.
        /// </summary>        
        public float SetPosition(Vector3 position)
        {
            Vector3 rideDir = Vector3.ProjectOnPlane(Line.Instance.GetCurrentRideDirection(), Vector3.up).normalized;
            Start = position;
            transform.position = Vector3.Lerp(Start, Start + rideDir * Length, 0.5f);
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
            TerrainManager.Instance.ActiveSlope = null;
            Destroy(gameObject);
        }
    }
}