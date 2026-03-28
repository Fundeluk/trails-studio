using System.Collections.Generic;
using LineSystem;
using Managers;
using Misc;
using Obstacles.TakeOff;
using TerrainEditing;
using UnityEngine;

namespace Obstacles.Landing
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

        private void RemoveFromHeightmap()
        {
            GetObstacleHeightmapCoordinates().MarkAs(new FreeCoordinateState());
            if (Slope != null)
            {
                Slope.RemoveWaypoint(this);
            }
        }

        public List<(string name, string value)> GetLineElementInfo()
        {
            Vector3 closestPointOnTakeoffRideDirection = MathHelper.GetNearestPointOnLine(PairedTakeoff.GetTransitionEnd(), PairedTakeoff.GetRideDirection(), GetLandingPoint());
            float distanceToTakeoffRideDirection = Vector3.Distance(closestPointOnTakeoffRideDirection, GetLandingPoint());

            var output = new List<(string name, string value)>
            {
                ("Type", "Landing"),
                ("Landing area slope", $"{GetSlopeAngle() * Mathf.Rad2Deg,10:0}°"),
                ("Height", $"{GetHeight(),10:0.##}m"),
                ("Length", $"{GetLength(),10:0.##}m"),
                ("Width",$"{GetWidth(),10:0.##}m"),
                ("Jump length", $"{Vector3.Distance(GetLandingPoint(), PairedTakeoff.GetTransitionEnd()), 10:0.##}m"),
                ("Rotation from takeoff", $"{GetRotation(),10:0}°"),
                ("Exit speed", $"{PhysicsManager.MsToKmh(ExitSpeed),10:0}km/h"),
                ("Shift to side from takeoff's direction", $"{distanceToTakeoffRideDirection, 10:0.#}m")
            };

            Vector3 rideDirXz = Vector3.ProjectOnPlane(GetRideDirection(), Vector3.up).normalized;
            float slopeAngleDeg = Vector3.SignedAngle(rideDirXz, GetRideDirection(), -Vector3.Cross(Vector3.up, GetRideDirection()));

            if (slopeAngleDeg != 0)
            {
                output.Add(("Slope change angle", $"{slopeAngleDeg,10:0}°"));
            }

            return output;
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
            transform.SetParent(Line.Instance.transform);

            meshGenerator = GetComponent<LandingMeshGenerator>();
            meshGenerator.SetDefaultDirtMaterial();
            meshGenerator.SetBatch(data.height, data.width, data.thickness, data.slopeAngle);    

            lineIndex = data.lineIndex;
            PairedTakeoff = Line.Instance[lineIndex - 1] as Takeoff;
            PreviousLineElement = PairedTakeoff;
            transform.SetPositionAndRotation(data.position, data.rotation);
            ExitSpeed = data.exitSpeed;

            RecalculateCameraTargetPosition();

            PairedTakeoff.SetLanding(this);

            MatchingTrajectory = PairedTakeoff.MatchingTrajectory;
        }       
    }

}