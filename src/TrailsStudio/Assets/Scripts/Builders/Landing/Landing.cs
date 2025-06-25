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
        
        public void Initialize(LandingMeshGenerator meshGenerator, GameObject cameraTarget, ILineElement previousLineElement, float exitSpeed)
        {
            base.Initialize(meshGenerator, cameraTarget, previousLineElement);
            ExitSpeed = exitSpeed;
            meshGenerator.SetDefaultDirtMaterial();
            PairedTakeoff = Line.Instance.GetLastLineElement() as Takeoff;
            lineIndex = Line.Instance.AddLineElement(this);
            transform.SetParent(Line.Instance.transform);
        }

        private void RemoveFromHeightmap()
        {
            GetObstacleHeightmapCoordinates().MarkAs(new FreeCoordinateState());
            slopeHeightmapCoordinates?.MarkAs(new FreeCoordinateState());
            if (slope != null)
            {
                slope.RemoveWaypoint(this);
            }
            slopeHeightmapCoordinates = null;
        }

        public List<(string name, string value)> GetLineElementInfo()
        {
            return new List<(string name, string value)>
            {
                ("Type", "Landing"),
                ("Slope", $"{GetSlopeAngle() * Mathf.Rad2Deg,10:0}°"),
                ("Height", $"{GetHeight(),10:0.00}m"),
                ("Length", $"{GetLength(),10:0.00}m"),
                ("Width",$"{GetWidth(),10:0.00}m"),
            };
        }


        public override void DestroyUnderlyingGameObject()
        {
            PairedTakeoff.SetLanding(null);
            RemoveFromHeightmap();
            base.DestroyUnderlyingGameObject();
        }

        public int GetIndex() => lineIndex;        
    }

}