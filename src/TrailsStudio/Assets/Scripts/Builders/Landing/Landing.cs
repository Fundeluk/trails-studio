using Assets.Scripts.Managers;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.Builders
{
    public class Landing : LandingBase, ILineElement, ISaveable<LandingData>
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

        public override void AddSlopeHeightmapCoords(HeightmapCoordinates coords)
        {
            if (slopeHeightmapCoordinates == null)
            {
                slopeHeightmapCoordinates = new(coords);
            }
            else
            {
                slopeHeightmapCoordinates.Add(coords);
            }

            slopeHeightmapCoordinates.MarkAs(new HeightSetCoordinateState());

            // overwrite the coordinates actually occupied by the landing to occupied state
            GetObstacleHeightmapCoordinates().MarkAs(new OccupiedCoordinateState(this));
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
            Vector3 closestPointOnTakeoffRideDirection = MathHelper.GetNearestPointOnLine(PairedTakeoff.GetTransitionEnd(), PairedTakeoff.GetRideDirection(), GetLandingPoint());
            float distanceToTakeoffRideDirection = Vector3.Distance(closestPointOnTakeoffRideDirection, GetLandingPoint());

            return new List<(string name, string value)>
            {
                ("Type", "Landing"),
                ("Slope", $"{GetSlopeAngle() * Mathf.Rad2Deg,10:0}°"),
                ("Height", $"{GetHeight(),10:0.##}m"),
                ("Length", $"{GetLength(),10:0.##}m"),
                ("Width",$"{GetWidth(),10:0.##}m"),
                ("Jump length", $"{Vector3.Distance(GetLandingPoint(), PairedTakeoff.GetTransitionEnd()), 10:0.##}m"),
                ("Rotation from takeoff", $"{GetRotation(),10:0}°"),
                ("Shift to side from takeoff's direction", $"{distanceToTakeoffRideDirection, 10:0.#}m")
            };
        }


        public override void DestroyUnderlyingGameObject()
        {
            PairedTakeoff.SetLanding(null);
            RemoveFromHeightmap();
            base.DestroyUnderlyingGameObject();
        }

        public int GetIndex() => lineIndex;

        public LandingData GetSerializableData() => new LandingData(this);

        public void LoadFromData(LandingData data)
        {
            lineIndex = data.lineIndex;
            PairedTakeoff = Line.Instance[lineIndex - 1] as Takeoff;
            transform.SetPositionAndRotation(data.position, data.rotation);
            ExitSpeed = data.exitSpeed;
            meshGenerator.SetDefaultDirtMaterial();
            meshGenerator.SetBatch(data.height, data.width, data.thickness, data.slopeAngle);    
            
            if (data.slopeHeightmapCoordinates != null)
            {
                slopeHeightmapCoordinates = data.slopeHeightmapCoordinates.ToHeightmapCoordinates();
            }
            else
            {
                slopeHeightmapCoordinates = null;
            }            

            RecalculateCameraTargetPosition();

            PairedTakeoff.SetLanding(this);

        }
    }

}