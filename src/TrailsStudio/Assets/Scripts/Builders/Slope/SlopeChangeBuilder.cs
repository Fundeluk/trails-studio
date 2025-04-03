using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Assets.Scripts.Builders
{
    public class SlopeChangeBuilder : SlopeChangeBase
    {
        public void Initialize(Vector3 start, float heightDifference = 0, float length = 0)
        {
            this.length = length;
            this.start = start;
            this.startHeight = start.y;
            this.endHeight = startHeight + heightDifference;
            this.highlight = GetComponent<DecalProjector>();
            transform.position = Vector3.Lerp(start, start + length * Line.Instance.GetCurrentRideDirection(), 0.5f);            
            UpdateHighlight();
        }

        public void SetLength(float length)
        {
            this.length = length;
            UpdateHighlight();
        }

        public void SetHeightDifference(float heightDifference)
        {
            this.endHeight = startHeight + heightDifference;
            UpdateHighlight();
        }

        public SlopeChange Build()
        {
            enabled = false;

            SlopeChange slopeChange = GetComponent<SlopeChange>();
            slopeChange.Initialize(start, endHeight, length);      
            slopeChange.enabled = true;

            return slopeChange;
        }        
    }
}