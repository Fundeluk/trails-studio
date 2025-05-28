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
        public float Length { get; protected set; }
        protected bool finished = false;
        protected ILineElement previousLineElement;

        /// <summary>
        /// Angle of the slope in radians. Negative if the slope is going down.
        /// </summary>
        public float Angle { get; protected set; }

        protected virtual void UpdateHighlight()
        {
            if (finished || Length == 0)
            {
                highlight.enabled = false;
                return;
            }
            else
            {
                highlight.enabled = true;
            }

            Vector3 rideDirection = Vector3.ProjectOnPlane(Line.Instance.GetCurrentRideDirection(), Vector3.up);
            Vector3 rideDirNormal = Vector3.Cross(rideDirection, Vector3.up).normalized;

            Vector3 end = Start + Length * rideDirection;

            Vector3 position = Vector3.Lerp(Start, end, 0.5f);
            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, rideDirNormal);

            highlight.transform.SetPositionAndRotation(position, rotation);

            DecalProjector decalProjector = highlight.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(Length, Line.Instance.GetLastLineElement().GetBottomWidth(), 20);
        }

        private void OnDisable()
        {
            if (highlight != null)
            {
                highlight.enabled = false;
            }
        }
    }
}