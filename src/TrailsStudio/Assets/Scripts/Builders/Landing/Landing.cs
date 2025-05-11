using Assets.Scripts.Managers;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Builders
{
    public class Landing : LandingBase, ILineElement
    {
        private int lineIndex;
        
        public override void Initialize(LandingMeshGenerator meshGenerator, Terrain terrain, GameObject cameraTarget, ILineElement previousLineElement)
        {
            base.Initialize(meshGenerator, terrain, cameraTarget, previousLineElement);
            meshGenerator.GetComponent<MeshRenderer>().material = material;
            takeoff = Line.Instance.GetLastLineElement() as Takeoff;
            lineIndex = Line.Instance.AddLineElement(this);
            this.transform.SetParent(Line.Instance.transform);
        }

        private void RemoveFromHeightmap()
        {
            GetHeightmapCoordinates().UnmarkAsOccupied();
            if (slopeHeightmapCoordinates.HasValue)
            {
                slopeHeightmapCoordinates.Value.UnmarkAsOccupied();
            }
            if (slope != null)
            {
                slope.RemoveWaypoint(this);
            }
        }

        public List<(string name, string value)> GetLineElementInfo()
        {
            return new List<(string name, string value)>
            {
                ("Type", "Landing"),
                ("Slope", $"{GetSlope(),10:0}°"),
                ("Height", $"{GetHeight(),10:0.00}m"),
                ("Length", $"{GetLength(),10:0.00}m"),
                ("Width",$"{GetWidth(),10:0.00}m"),
            };
        }


        public override void DestroyUnderlyingGameObject()
        {
            takeoff.SetLanding(null);
            RemoveFromHeightmap();
            base.DestroyUnderlyingGameObject();
        }

        public int GetIndex() => lineIndex; 
        
        public float GetExitSpeed()
        {
            // TODO finish
            return 0;
        }

    }

}