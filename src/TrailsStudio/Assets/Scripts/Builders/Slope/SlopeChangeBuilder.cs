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
            this.Length = length;
            this.Start = start;
            this.startHeight = start.y;
            this.endHeight = startHeight + heightDifference;
            this.highlight = GetComponent<DecalProjector>();
            transform.position = Vector3.Lerp(start, start + length * Line.Instance.GetCurrentRideDirection(), 0.5f);         
            UpdateHighlight();
        }

        // TODO check that the slope wont collide with occupied positions on the terrain before setting params below
        public void SetLength(float length)
        {
            this.Length = length;
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
            slopeChange.Initialize(Start, endHeight, Length);      
            slopeChange.enabled = true;

            return slopeChange;
        }        
    }
}