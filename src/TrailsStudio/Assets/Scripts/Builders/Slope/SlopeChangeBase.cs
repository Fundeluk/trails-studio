using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders
{
    public abstract class SlopeChangeBase : MonoBehaviour
    {
        protected DecalProjector highlight;

        public Vector3 Start { get; protected set; }
        protected float startHeight;
        protected float endHeight;

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