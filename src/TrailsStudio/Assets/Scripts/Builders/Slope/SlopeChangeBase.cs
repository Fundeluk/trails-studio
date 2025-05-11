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
        protected float length;
        protected bool finished = false;

        protected virtual void UpdateHighlight()
        {
            if (finished || length == 0)
            {
                highlight.enabled = false;
                return;
            }
            else
            {
                highlight.enabled = true;
            }

            Vector3 rideDirection = Line.Instance.GetCurrentRideDirection();
            Vector3 rideDirNormal = Vector3.Cross(rideDirection, Vector3.up).normalized;

            Vector3 position = Vector3.Lerp(Start, Start + length * rideDirection, 0.5f);
            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, rideDirNormal);

            highlight.transform.SetPositionAndRotation(position, rotation);

            DecalProjector decalProjector = highlight.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(length, Line.Instance.GetLastLineElement().GetBottomWidth(), 20);
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