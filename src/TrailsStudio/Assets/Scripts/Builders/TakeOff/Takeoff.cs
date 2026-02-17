using Assets.Scripts.Managers;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders
{
    public class Takeoff : TakeoffBase, ILineElement, ISaveable<TakeoffData>
    {
        [SerializeField]
        protected GameObject pathProjectorPrefab;

        protected GameObject pathProjector;

        public Landing PairedLanding { get; private set; }

        int lineIndex;

        public void Initialize(TakeoffMeshGenerator meshGenerator, GameObject cameraTarget, ILineElement previousLineElement, float entrySpeed)
        {
            base.Initialize(meshGenerator, cameraTarget, previousLineElement);
            this.EntrySpeed = entrySpeed;
            meshGenerator.SetDefaultDirtMaterial();
            lineIndex = Line.Instance.AddLineElement(this);
            this.pathProjector = Instantiate(pathProjectorPrefab);
            this.pathProjector.transform.SetParent(transform);
            this.transform.SetParent(Line.Instance.transform);
            UpdatePathProjector();
            InitTrajectoryRenderer();
            DrawTrajectory();
        }        

        protected void UpdatePathProjector()
        {

            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, GetRideDirection());
            Vector3 position = Vector3.Lerp(previousLineElement.GetEndPoint(), GetStartPoint(), 0.5f);
            position.y = TerrainManager.maxHeight;
            pathProjector.transform.SetPositionAndRotation(position, rotation);

            float distance = Vector3.Distance(previousLineElement.GetEndPoint(), GetStartPoint());
            float width = Mathf.Lerp(previousLineElement.GetBottomWidth(), GetBottomWidth(), 0.5f);

            DecalProjector decalProjector = pathProjector.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(width, distance, 10);
        }

        public int GetIndex() => lineIndex;        

        public void SetLanding(Landing landing)
        {
            this.PairedLanding = landing;
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
        
        public TakeoffBuilder Revert()
        {
            Destroy(pathProjector);
            Destroy(trajectoryRenderer.gameObject);

            enabled = false;

            // when reverting a takeoff, it has to be the last element in the line, so remove it
            Line.Instance.RemoveLastLineElement();

            RemoveFromHeightmap();

            TakeoffBuilder builder = GetComponent<TakeoffBuilder>();
            builder.Initialize(meshGenerator, cameraTarget, previousLineElement);
            builder.ResetAfterRevert();
            BuildManager.Instance.activeBuilder = builder;
            builder.enabled = true;

            return builder;
        }

        public List<(string name, string value)> GetLineElementInfo()
        {
            var output = new List<(string name, string value)>
            {
                ("Type", "Takeoff"),
                ("Radius", $"{GetRadius(),10:0.#}m"),
                ("End Angle", $"{GetEndAngle() * Mathf.Rad2Deg,10:0}°"),
                ("Height", $"{GetHeight(),10:0.##}m"),
                ("Length", $"{GetLength(),10:0.##}m"),
                ("Width", $"{GetWidth(),10:0.##}m"),
                ("Jump length", $"{Vector3.Distance(PairedLanding.GetLandingPoint(), GetTransitionEnd()),10:0.##}m"),
                ("Distance from previous line element's end point", $"{Vector3.Distance(previousLineElement.GetEndPoint(), GetStartPoint()),10:0.##}m"),
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
            if (PairedLanding != null)
            {
                PairedLanding.DestroyUnderlyingGameObject();
            }

            Destroy(pathProjector);

            RemoveFromHeightmap();

            base.DestroyUnderlyingGameObject();
        }

        public TakeoffData GetSerializableData() => new TakeoffData(this);

        public void LoadFromData(TakeoffData data)
        {
            this.transform.SetParent(Line.Instance.transform);
            transform.SetPositionAndRotation(data.position, data.rotation);

            lineIndex = data.lineIndex;

            meshGenerator = GetComponent<TakeoffMeshGenerator>();
            previousLineElement = Line.Instance[lineIndex - 1];

            this.pathProjector = Instantiate(pathProjectorPrefab);
            this.pathProjector.transform.SetParent(transform);
            UpdatePathProjector();

            meshGenerator.SetDefaultDirtMaterial();
            meshGenerator.SetBatch(data.height, data.width, data.thickness, data.radius);            
            EntrySpeed = data.entrySpeed;

            if (data.slopeHeightmapCoordinates != null)
            {
                slopeHeightmapCoordinates = data.slopeHeightmapCoordinates.ToHeightmapCoordinates();
            }
            else
            {
                slopeHeightmapCoordinates = null;
            }

            RecalculateCameraTargetPosition();

            MatchingTrajectory = data.trajectory.ToTrajectory();
            InitTrajectoryRenderer();
            DrawTrajectory();
        }
    }
}