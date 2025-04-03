using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders
{
    public abstract class SlopeChangeBase : MonoBehaviour
    {
        protected DecalProjector highlight;

        protected Vector3 start;
        protected float startHeight;
        protected float endHeight;
        protected float length;

        protected void UpdateHighlight()
        {
            if (length == 0)
            {
                highlight.enabled = false;
                return;
            }
            else
            {
                highlight.enabled = true;
            }

            Vector3 rideDirection = Line.Instance.GetCurrentRideDirection();
            transform.position = Vector3.Lerp(start, start + length * rideDirection, 0.5f);
            
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