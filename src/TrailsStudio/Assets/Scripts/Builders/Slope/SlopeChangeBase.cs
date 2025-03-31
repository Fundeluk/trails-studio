using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders
{
    public abstract class SlopeChangeBase : MonoBehaviour
    {
        protected GameObject highlight;

        protected Vector3 start;
        protected float startHeight;
        protected float endHeight;
        protected float length;

        protected void UpdateHighlight()
        {
            if (length == 0)
            {
                highlight.SetActive(false);
                return;
            }
            else
            {
                highlight.SetActive(true);
            }

            Vector3 rideDirection = Line.Instance.GetLastLineElement().GetRideDirection();
            Vector3 newPos = Vector3.Lerp(start, start + length * rideDirection, 0.5f);
            newPos.y = Mathf.Max(startHeight, endHeight);

            Quaternion newRot = SlopePositionHighlighter.GetRotationForDirection(rideDirection);
            highlight.transform.SetPositionAndRotation(newPos, newRot);

            DecalProjector decalProjector = highlight.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(length, Line.Instance.GetLastLineElement().GetBottomWidth(), 10);
        }        
    }
}