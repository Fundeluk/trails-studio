using Assets.Scripts;
using Assets.Scripts.Managers;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using UnityEngine.WSA;


namespace Assets.Scripts.Builders
{
    public struct LandingPositionTrajectoryInfo
    {
        public Vector3 landingPosition;
        public readonly Trajectory trajectory;
        public readonly Trajectory.TrajectoryPoint edgePoint;

        public LandingPositionTrajectoryInfo(Vector3 position, Trajectory trajectory, Trajectory.TrajectoryPoint edgePoint)
        {
            landingPosition = position;
            this.trajectory = trajectory;
            this.edgePoint = edgePoint;
        }
    }

    /// <summary>
    /// Moves a highlight object anywhere after the last line element based on user input. <br/>
    /// Positions the highlight based on where the mouse is pointing on the terrain. Draws a line from the last line element to the highlight<br/>
    /// and shows distance from the line endpoint to the highlight + the Angle between the last line elements ride direction and the line.<br/>
    /// </summary>
    /// <remarks>Here, the highlight is the LandingBuilder mesh which is <b>attached to the same GameObject</b> as this highlighter.</remarks>
    public class LandingPositioner : Positioner
    {
        // the minimum and maximum distances between the last line element and new obstacle
        [Header("Build bounds")]
        [Tooltip("The minimum distance between the last line element and the new obstacle.")]
        public float minBuildDistance = 0.5f;
        [Tooltip("The maximum distance between the last line element and the new obstacle.")]
        public float maxBuildDistance = 15;

        [Tooltip("The minimum distance after the landing where the rideout area must be free of obstacles.")]
        public float landingClearanceDistance = 5f;

        [Header("Position highlight settings")]
        [SerializeField]
        GameObject positionHighlightPrefab;

        [SerializeField]
        float positionHighlightWidth = 0.5f;

        [SerializeField]
        float positionHighlightLength = 1f;

        public static float clearanceWidth = 1.5f;

        LandingBuilder builder;

        List<(LandingPositionTrajectoryInfo info, MeshCollider highlight)> allowedTrajectoryPositions = new();

        public override void OnEnable()
        {
            builder = GetComponent<LandingBuilder>();
            baseBuilder = builder;

            base.OnEnable();

            builder.PositionChanged += OnParamChanged;
            builder.RotationChanged += OnParamChanged;
            builder.SlopeChanged += OnParamChanged;
            builder.HeightChanged += OnParamChanged;
            builder.ExitSpeedChanged += OnParamChanged;

            builder.SetRideDirection(lastLineElement.GetRideDirection());

            UpdateValidPositionsForTrajectory();

            if (allowedTrajectoryPositions.Count == 0)
            {
                UIManager.Instance.ShowMessage("No valid landing positions found. Please adjust the takeoff angle or position.");
            }
            else
            {
                LandingPositionTrajectoryInfo trajectoryPoint = allowedTrajectoryPositions[allowedTrajectoryPositions.Count / 2].info;
                MatchLandingToTrajectoryPoint(trajectoryPoint);
            }

            CanMoveHighlight = false;

            UpdateLineRenderer();

            GetComponent<MeshRenderer>().enabled = true;

            UpdateMeasureText();
        }

        void OnParamChanged<T>(object sender, ParamChangeEventArgs<T> e)
        {
            // update the line renderer to show the new position
            UpdateLineRenderer();
            UpdateMeasureText();
        }



        protected override void OnDisable()
        {
            ClearPositionHighlights();
            base.OnDisable();
            builder.PositionChanged -= OnParamChanged;
            builder.RotationChanged -= OnParamChanged;
            builder.SlopeChanged -= OnParamChanged;
            builder.HeightChanged -= OnParamChanged;
            builder.ExitSpeedChanged -= OnParamChanged;
        }

        protected override void Awake()
        {
            terrainLayerMask = LayerMask.GetMask("Position Highlight");
        }

        protected override void FixedUpdate()
        {
            if (CanMoveHighlight)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayerMask))
                {
                    MatchLandingToTrajectoryPoint(hit.collider.GetComponent<PositionHolder>().TrajectoryPositionInfo);                    
                }
            }
        }

        MeshCollider CreatePositionHighlight(Vector3[] vertices, int[] triangles, LandingPositionTrajectoryInfo positionTrajectoryInfo)
        {
            GameObject posHighlight = Instantiate(positionHighlightPrefab, positionTrajectoryInfo.landingPosition, Quaternion.identity, Line.Instance.transform);
            MeshCollider meshCollider = posHighlight.GetComponent<MeshCollider>();
            MeshFilter filter = posHighlight.GetComponent<MeshFilter>();
            PositionHolder positionHolder = posHighlight.GetComponent<PositionHolder>();

            // Convert vertices from world space to local space relative to the new GameObject
            Vector3[] localVertices = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                localVertices[i] = posHighlight.transform.InverseTransformPoint(vertices[i]);
            }

            Mesh mesh = new()
            {
                vertices = localVertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshCollider.sharedMesh = mesh;
            filter.mesh = mesh;

            positionHolder.Init(positionTrajectoryInfo);

            return meshCollider;
        }

        List<MeshCollider> CreatePositionHighlights(List<LandingPositionTrajectoryInfo> positions)
        {
            int pointCount = positions.Count;

            if (pointCount == 0)
            {
                return new List<MeshCollider>();
            }

            List<MeshCollider> highlights = new(pointCount);           

            // Create vertices for the ribbon
            Vector3[] vertices = new Vector3[4];
            int[] triangles = new int[6] {0, 2, 1, 1, 2, 3 };

            Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up);
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

            Vector3 toNext = right * positionHighlightWidth / 2;
            Vector3 toForward = forward * positionHighlightLength / 2;
            

            for (int i = 0; i < pointCount; i++)
            {
                Vector3 position = positions[i].landingPosition;
                
                vertices[0] = position - toNext + toForward;
                vertices[1] = position - toNext - toForward;
                vertices[2] = position + toNext + toForward;
                vertices[3] = position + toNext - toForward;

                highlights.Add(CreatePositionHighlight(vertices, triangles, positions[i]));                
            }            

            return highlights;
        }

        void ClearPositionHighlights()
        {
            if (allowedTrajectoryPositions == null || allowedTrajectoryPositions.Count == 0)
            {
                return;
            }

            foreach (var highlight in allowedTrajectoryPositions)
            {
                Destroy(highlight.highlight.gameObject);
            }
            allowedTrajectoryPositions.Clear();
        }

        void UpdateValidPositionsForTrajectory(float normalizedAngleStep = 0.05f)
        {
            ClearPositionHighlights();

            var result = new List<(LandingPositionTrajectoryInfo positionTrajectoryInfo, MeshCollider highlight)>();

            var trajectoryInfos = new List<LandingPositionTrajectoryInfo>();
            for (float normalizedAngle = -1; normalizedAngle <= 1; normalizedAngle += normalizedAngleStep)
            {
                Trajectory trajectory = PhysicsManager.GetFlightTrajectory(builder.PairedTakeoff, normalizedAngle);                
                LandingPositionTrajectoryInfo trajectoryInfo = GetValidPointFromTrajectory(trajectory);
                if (ValidatePosition(trajectoryInfo.landingPosition))
                {
                    trajectoryInfos.Add(trajectoryInfo);
                }
            }

            List<MeshCollider> highlights = CreatePositionHighlights(trajectoryInfos);

            for (int i = 0; i < trajectoryInfos.Count; i++)
            {
                result.Add((trajectoryInfos[i], highlights[i]));
            }

            allowedTrajectoryPositions = result;
        }

        LandingPositionTrajectoryInfo GetValidPointFromTrajectory(Trajectory trajectory)
        {
            LinkedListNode<Trajectory.TrajectoryPoint> point = trajectory.Apex;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                SlopeChange slopeChange = TerrainManager.Instance.ActiveSlope;
                float minDistance = float.MaxValue;
                LandingPositionTrajectoryInfo bestPoint = new(new(point.Value.position.x, TerrainManager.GetHeightAt(point.Value.position), point.Value.position.z), trajectory, point.Value);

                while (point != null)
                {
                    (Vector3 position, Vector3 edgePosition, Vector3 landingDir) = slopeChange.GetLandingInfoForPosition(builder, point.Value.position);
                    float distance = Vector3.Distance(edgePosition, point.Value.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestPoint = new(position, trajectory, new(edgePosition, landingDir));
                        bestPoint.landingPosition.y = TerrainManager.GetHeightAt(bestPoint.landingPosition);
                    }

                    point = point.Next;
                }

                return bestPoint;
            }
            else
            {
                Trajectory.TrajectoryPoint edgePoint = trajectory.GetPointAtHeight(builder.GetHeight()).Value;

                Vector3 position = edgePoint.position;
                position.y = TerrainManager.GetHeightAt(position);

                return new(position, trajectory, edgePoint);
            }
        }        

        public void UpdateLineRenderer()
        {
            // draw a line between the current line end point and the point where the mouse is pointing
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, builder.PairedTakeoff.GetEndPoint());
            lineRenderer.SetPosition(1, builder.GetStartPoint());
        }        

        public void UpdateMeasureText()
        {
            Vector3 toLanding = Vector3.ProjectOnPlane(builder.GetTransform().position - lastLineElement.GetTransform().position, Vector3.up);
            Vector3 rideDirProjected = Vector3.ProjectOnPlane(lastLineElement.GetRideDirection(), Vector3.up);
            textMesh.GetComponent<TextMeshPro>().text = $"Jump length: {builder.GetDistanceFromPreviousLineElement():F2}m" +
                $"\nAngle: {(int)Vector3.SignedAngle(rideDirProjected, toLanding, Vector3.up):F2}°"
                + $"\nExit speed: {PhysicsManager.MsToKmh(builder.GetExitSpeed()):F2}km/h";

            // make the text go along the line and lay flat on the terrain
            float camDistance = CameraManager.Instance.GetTDCamDistance();

            textMesh.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 6 * 5, camDistance)),
                Quaternion.LookRotation(-Vector3.up, Vector3.Cross(lastLineElement.GetRideDirection(), Vector3.up)));
        }

        public bool TrySetRotation(float angle)
        {
            // the distances are calculated on the XZ plane to avoid the influence of the height of the terrain
            Vector3 lastElementEnd = lastLineElement.GetEndPoint();
            lastElementEnd.y = 0;

            float angleDiff = angle - builder.GetRotation();
            Vector3 rideDir = Quaternion.AngleAxis(angleDiff, Vector3.up) * transform.forward;

            ObstacleBounds newBounds = builder.GetBoundsForObstaclePosition(transform.position, rideDir);

            float distanceToStartPoint = Vector3.Distance(newBounds.startPoint, lastElementEnd);

            if (distanceToStartPoint > maxBuildDistance)
            {
                UIManager.Instance.ShowMessage($"The new obstacle position is too far from the last line element. The maximum distance is {maxBuildDistance}m", 2f);
                return false;
            }
            else if (distanceToStartPoint < minBuildDistance)
            {
                UIManager.Instance.ShowMessage($"The new obstacle position is too close to the last line element. The minimal distance is {minBuildDistance}m", 2f);
                return false;
            }

            bool newPositionCollides = !TerrainManager.Instance.IsAreaFree(newBounds.startPoint, newBounds.endPoint, builder.GetBottomWidth());
            if (newPositionCollides)
            {
                UIManager.Instance.ShowMessage("The new obstacle position is colliding with a terrain change or another obstacle.", 2f);
                return false;
            }

            // TODO make the width here parametrized by global setting
            bool newRideoutAreaDoesNotCollide = TerrainManager.Instance.IsAreaFree(newBounds.endPoint
                , newBounds.endPoint + newBounds.RideDirection * landingClearanceDistance, 1.5f);

            if (!newRideoutAreaDoesNotCollide)
            {
                UIManager.Instance.ShowMessage($"The rideout area after the landing is occupied by another obstacle or a terrain change.", 2f);
                return false;
            }

            UIManager.Instance.HideMessage();

            // place the highlight at the hit point
            builder.SetRotation(angle);

            // make the text go along the line and lay flat on the terrain
            float camDistance = CameraManager.Instance.GetTDCamDistance();


            textMesh.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 8, camDistance)),
                Quaternion.LookRotation(Vector3.down, Vector3.Cross(lastLineElement.GetRideDirection(), Vector3.up)));

            UpdateMeasureText();

            UpdateLineRenderer();

            return true;

        }

        private void MatchLandingToTrajectoryPoint(LandingPositionTrajectoryInfo trajectoryInfo)
        {
            Vector3 velocityForward = Vector3.ProjectOnPlane(trajectoryInfo.edgePoint.velocity, transform.up);
            builder.SetSlope(Vector3.Angle(trajectoryInfo.edgePoint.velocity, velocityForward) * Mathf.Deg2Rad);
            builder.SetPosition(trajectoryInfo.landingPosition);
            
            builder.SetMatchingTrajectory(trajectoryInfo.trajectory);
        }

        private bool ValidatePosition(Vector3 newPosition)
        {
            // the distances are calculated on the XZ plane to avoid the influence of the height of the terrain
            Vector3 lastElementEnd = lastLineElement.GetEndPoint();
            lastElementEnd.y = 0;

            Vector3 lastElemRideDir = Vector3.ProjectOnPlane(lastLineElement.GetRideDirection(), Vector3.up);

            Vector3 toHit = Vector3.ProjectOnPlane(newPosition - lastElementEnd, Vector3.up);

            ObstacleBounds newBounds = builder.GetBoundsForObstaclePosition(newPosition, builder.GetRideDirection());

            float edgeToEdgeDistance = Vector3.Distance(builder.PairedTakeoff.GetTransitionEnd(), newBounds.contactPoint);

            // prevent the landing from being placed behind the takeoff
            float projection = Vector3.Dot(toHit, lastElemRideDir);
            if (projection < 0)
            {
                UIManager.Instance.ShowMessage("Cannot place the landing at an angle larger than 90 degrees with respect to its takeoff.", 2f);
                return false;
            }
            else if (edgeToEdgeDistance > maxBuildDistance)
            {
                UIManager.Instance.ShowMessage($"The new position is too far from the last line element. The maximum distance is {maxBuildDistance}m", 2f);
                return false;
            }
            else if (edgeToEdgeDistance < minBuildDistance)
            {
                UIManager.Instance.ShowMessage($"The new position is too close to the last line element. The minimal distance is {minBuildDistance}m", 2f);
                return false;
            }

            bool newPositionCollides = !TerrainManager.Instance.IsAreaFree(newBounds.startPoint, newBounds.endPoint, builder.GetBottomWidth(), builder.PairedTakeoff);
            if (newPositionCollides)
            {
                UIManager.Instance.ShowMessage("The new obstacle position is colliding with a terrain change or another obstacle.", 2f);
                return false;
            }

            bool newRideoutAreaDoesNotCollide = TerrainManager.Instance.IsAreaFree(newBounds.endPoint
                , newBounds.endPoint + newBounds.RideDirection * landingClearanceDistance, clearanceWidth);

            if (!newRideoutAreaDoesNotCollide)
            {
                UIManager.Instance.ShowMessage($"The rideout area after the landing is occupied by another obstacle or a terrain change.", 2f);
                return false;
            }            

            return true;
        }

        public override bool TrySetPosition(Vector3 hit)
        {
            if (!ValidatePosition(hit))
            {
                return false;
            }

            UIManager.Instance.HideMessage();

            // place the highlight at the hit point
            builder.SetPosition(hit);

            return true;            
        }
    }
}
