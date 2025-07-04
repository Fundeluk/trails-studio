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

        public void CanBuild(bool canBuild)
        {
            if (canBuild)
            {
                highlight.material.color = Color.green;
            }
            else
            {
                highlight.material.color = Color.red;
            }
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

        //TODO when setting upwards slope, validate if the end can be reached

        /// <summary>
        /// Sets the position of the slope's start point and updates the highlight.
        /// </summary>        
        public void SetPosition(Vector3 position)
        {
            Vector3 rideDir = Vector3.ProjectOnPlane(Line.Instance.GetCurrentRideDirection(), Vector3.up).normalized;
            Start = position;
            transform.position = Vector3.Lerp(Start, Start + rideDir * Length, 0.5f);
            UpdateHighlight();
            float speedFromLast = Line.Instance.GetLastLineElement().GetExitSpeed();

            // this should not happen, validated in SlopePositioner
            if (!PhysicsManager.TryCalculateExitSpeed(speedFromLast, Vector3.Distance(Line.Instance.GetLastLineElement().GetEndPoint(), position), out _))
            {
                throw new InsufficientSpeedException("Cannot set position for slope change as the speed at the start point is insufficient to reach it.");
            }
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