using Assets.Scripts.Managers;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Assets.Scripts.Builders
{
    public class SlopeChangeBuilder : SlopeChangeBase
    {
        private GameObject camTarget;

        public void Initialize(Vector3 start, GameObject highlight, float heightDifference = 0, float length = 0)
        {
            this.length = length;
            this.start = start;
            this.startHeight = start.y;
            this.endHeight = startHeight + heightDifference;
            this.highlight = highlight;
            this.highlight.transform.position = Vector3.Lerp(start, start + length * Line.Instance.GetLastLineElement().GetRideDirection(), 0.5f);            
            UpdateHighlight();
            TerrainManager.Instance.activeSlopeBuilder = this;

            camTarget = new GameObject("SlopeChangeBuilderCamTarget");
            camTarget.transform.position = highlight.transform.position;
        }

        private void UpdateCamTargetPosition()
        {
            camTarget.transform.position = highlight.transform.position;
        }

        public GameObject GetCamTarget() => camTarget;

        public void SetLength(float length)
        {
            this.length = length;
            UpdateHighlight();
            UpdateCamTargetPosition();
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
            slopeChange.Initialize(start, endHeight, length, highlight);      
            slopeChange.enabled = true;

            return slopeChange;
        }


    }
}