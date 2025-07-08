using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders
{
    public abstract class SlopeChangeBase : MonoBehaviour
    {
        public event EventHandler<ParamChangeEventArgs<Vector3>> PositionChanged;
        protected void OnPositionChanged(Vector3 newPosition)
        {
            PositionChanged?.Invoke(this, new ParamChangeEventArgs<Vector3>("Position", newPosition));
        }

        public event EventHandler<ParamChangeEventArgs<float>> HeightDiffChanged;
        protected void OnHeightDiffChanged(float newHeightDiff)
        {
            HeightDiffChanged?.Invoke(this, new ParamChangeEventArgs<float>("Height Difference", newHeightDiff));
        }

        public event EventHandler<ParamChangeEventArgs<float>> LengthChanged;
        protected void OnLengthChanged(float newLength)
        {
            LengthChanged?.Invoke(this, new ParamChangeEventArgs<float>("Length", newLength));
        }

        protected DecalProjector highlight;

        public Vector3 Start { get; protected set; }
        protected float startHeight;
        protected float endHeight;

        public float HeightDifference
        {
            get => endHeight - startHeight;
        }

        /// <summary>
        /// Width between last two waypoints
        /// </summary>
        protected float width;

        public float Length { get; protected set; }
        protected bool finished = false;
        protected ILineElement previousLineElement;

        /// <summary>
        /// Angle of the slope in radians. Negative if the slope is going down.
        /// </summary>
        public float Angle { get; protected set; }

        private void Awake()
        {
            highlight = GetComponent<DecalProjector>();
        }


        protected virtual void OnEnable()
        {
            previousLineElement = Line.Instance.GetLastLineElement();
        }

        /// <summary>
        /// Calculates the angle of a slope with provided params in radians.
        /// </summary>        
        public static float GetSlopeAngle(float length, float heightDiff)
        {
            float angle = 90 * Mathf.Deg2Rad - Mathf.Atan(length / Mathf.Abs(heightDiff));
            if (heightDiff < 0)
            {
                angle = -angle;
            }

            return angle;
        }

        protected void UpdateAngle()
        {
            Angle = GetSlopeAngle(Length, HeightDifference);
        }

        protected virtual void UpdateHighlight()
        {
            Vector3 rideDirection = Vector3.ProjectOnPlane(Line.Instance.GetCurrentRideDirection(), Vector3.up);
            Vector3 rideDirNormal = Vector3.Cross(rideDirection, Vector3.up).normalized;

            Vector3 end = Start + Length * rideDirection;

            Vector3 position = Vector3.Lerp(Start, end, 0.5f);
            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, rideDirNormal);

            transform.SetPositionAndRotation(position, rotation);    
            
            highlight.size = new Vector3(Length, width, 20);
        }        
    }
}