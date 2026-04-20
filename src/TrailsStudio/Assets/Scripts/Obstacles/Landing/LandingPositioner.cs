using System.Collections.Generic;
using LineSystem;
using Managers;
using Misc;
using PhysicsManager;
using TerrainEditing;
using TerrainEditing.Slope;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Obstacles.Landing
{    
    /// <summary>
    /// Takes care of positioning the landing builder on valid landing positions based on the flight trajectory from its paired takeoff.
    /// </summary>
    public partial class LandingPositioner : Positioner
    {        
        [Header("Position highlight settings")]
        [SerializeField]
        private GameObject positionHighlightPrefab;

        [SerializeField]
        private float positionHighlightWidth = 0.1f;

        [SerializeField] 
        private float positionHighlightLength = 0.7f;

        [SerializeField] 
        private float positionHighlightHeightOffset = 0.3f;

        private LandingBuilder builder;

        private LandingBuilder invisibleBuilder;

        private Button buildButton;

        /// <summary>
        /// Represents a list of allowed landing positions with their corresponding highlights according to the possible paired takeoff trajectories.
        /// </summary>
        private List<(LandingPositionCarrier info, MeshCollider highlight)> allowedTrajectoryPositions = new();

        public bool ValidPositionsExist => allowedTrajectoryPositions.Count > 0;

        public override void OnEnable()
        {
            builder = GetComponent<LandingBuilder>();
            baseBuilder = builder;

            invisibleBuilder = builder.InvisibleClone;

            base.OnEnable();

            builder.PositionChanged += OnParamChanged;
            builder.RotationChanged += OnParamChanged;
            builder.SlopeChanged += OnParamChanged;
            builder.HeightChanged += OnParamChanged;
            builder.ExitSpeedChanged += OnParamChanged;

            builder.SetRideDirection(lastLineElement.GetRideDirection());

            buildButton = StudioUIManager.Instance.landingBuildUI.GetComponent<LandingBuildUI>().BuildButton;

            CanMoveHighlight = false;

            UpdateValidPositionList();            
        }


        private void OnParamChanged<T>(object sender, ParamChangeEventArgs<T> e) => UpdateMeasureText();        


        protected override void OnDisable()
        {
            ClearPositionHighlights();
            builder.PositionChanged -= OnParamChanged;
            builder.RotationChanged -= OnParamChanged;
            builder.SlopeChanged -= OnParamChanged;
            builder.HeightChanged -= OnParamChanged;
            builder.ExitSpeedChanged -= OnParamChanged;
            base.OnDisable();
        }

        protected override void Awake()
        {
            raycastTargetLayerMask = LayerMask.GetMask("Position Highlight");
        }

        protected override void Update()
        {
            if (!CanMoveHighlight || allowedTrajectoryPositions.Count <= 0) return;
            
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, raycastTargetLayerMask)) return;
            
            if (hit.collider.TryGetComponent<PositionHolder>(out var holder))
            {
                holder.Select();
            }
        }

        private MeshCollider CreatePositionHighlight(Vector3[] vertices, int[] triangles, LandingPositionCarrier positionTrajectoryInfo)
        {
            // avoid placing the highlight under terrain
            Vector3 position = positionTrajectoryInfo.landingPosition;           

            GameObject posHighlight = Instantiate(positionHighlightPrefab, position, Quaternion.identity, Line.Instance.transform);
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

            positionHolder.Init(positionTrajectoryInfo.MatchBuilder);

            return meshCollider;
        }

        private List<MeshCollider> CreatePositionHighlights(List<LandingPositionCarrier> positions)
        {
            int pointCount = positions.Count;

            if (pointCount == 0)
            {
                return new List<MeshCollider>();
            }            

            List<MeshCollider> highlights = new(pointCount);           

            // Create vertices for the ribbon
            Vector3[] vertices = new Vector3[4];
            int[] triangles = {0, 2, 1, 1, 2, 3 };

            Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up);
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

            Vector3 toNext = right * positionHighlightWidth / 2;
            Vector3 toForward = forward * positionHighlightLength / 2;

            float highlightHeight;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                SlopeChange slope = TerrainManager.Instance.ActiveSlope;

                // if the slope goes upwards, put the highlights on its end height level.
                highlightHeight = slope.HeightDifference > 0 ? slope.GetFinishedEndPoint(slope.LastRideDirection).y :
                    // if it goes downwards, put them at the current end point height
                    slope.EndPoint.y;
            }
            else
            {
                highlightHeight = TerrainManager.Instance.GetHeightAt(positions[0].landingPosition);
            }

            highlightHeight += positionHighlightHeightOffset;


            for (int i = 0; i < pointCount; i++)
            {
                Vector3 position = positions[i].landingPosition;
                position.y = highlightHeight; // set the height of the highlight above terrain at the landing position


                vertices[0] = position - toNext + toForward;
                vertices[1] = position - toNext - toForward;
                vertices[2] = position + toNext + toForward;
                vertices[3] = position + toNext - toForward;

                highlights.Add(CreatePositionHighlight(vertices, triangles, positions[i]));                
            }            

            return highlights;
        }

        private void ClearPositionHighlights()
        {
            if (allowedTrajectoryPositions == null || allowedTrajectoryPositions.Count == 0)
            {
                return;
            }

            foreach (var highlight in allowedTrajectoryPositions)
            {
                if (highlight.highlight == null)
                {
                    continue; // skip if the highlight is already destroyed
                }

                Destroy(highlight.highlight.gameObject);
            }
            allowedTrajectoryPositions.Clear();
        }
                       
        private List<LandingPositionCarrier> CalculateValidLandingPositions(float normalizedAngleStep = 0.05f)
        {
            if (TerrainManager.Instance.ActiveSlope != null)
            {
                InternalDebug.Log("Calculating valid landing positions with slope");
            }
            else
            {
                InternalDebug.Log("Calculating valid landing positions without slope");
            }
            
            var trajectoryInfos = new List<LandingPositionCarrier>();
            for (float normalizedAngle = -1; normalizedAngle <= 1; normalizedAngle += normalizedAngleStep)
            {                
                Trajectory trajectory = PhysicsManager.PhysicsManager.GetFlightTrajectory(builder.PairedTakeoff, normalizedAngle);
                InternalDebug.DrawLine(trajectory.Apex.Value.position, trajectory.Apex.Value.position + Vector3.up * 5f, Color.red, 5f);
                LandingPositionCarrier trajectoryInfo = GetValidPointFromTrajectory(trajectory);                                

                if (trajectoryInfo != null)
                {
                    trajectoryInfos.Add(trajectoryInfo);
                }

            }   
            
            return trajectoryInfos;
        }

        private OnSlopeLandingPositionCarrier GetBestMatchingLandingPositionOnSlope(SlopeChange slope, Trajectory trajectory)
        {
            LinkedListNode<Trajectory.TrajectoryPoint> trajectoryPoint = trajectory.Apex;
            float minEdgeToTrajectoryDistance = float.MaxValue;
            OnSlopeLandingPositionCarrier bestPosition = null;
            LinkedListNode<Trajectory.TrajectoryPoint> bestNode = null;

            Vector3 flightDirectionXZ = Vector3.ProjectOnPlane(trajectoryPoint.Value.velocity, Vector3.up).normalized;
            SlopeBoundaryPoints boundaryPoints = GetSlopeBoundaryPoints(slope, invisibleBuilder, flightDirectionXZ);

            while (trajectoryPoint != null)
            {
                LandingCandidate? matchingLandingPosition = GetLandingInfoForDesiredTrajectoryPoint(slope, invisibleBuilder, trajectoryPoint.Value.position, boundaryPoints);

                if (!matchingLandingPosition.HasValue)
                {
                    trajectoryPoint = trajectoryPoint.Next;
                    continue;
                }

                (Vector3 landingPosition, Vector3 edgePosition, bool isWaypoint, bool isTilted) = matchingLandingPosition.Value;
                
                float edgeToTrajectoryDistance = Vector3.Distance(edgePosition, trajectoryPoint.Value.position);

                // skip supposed landing points that are below the edge (results in colliding with the back of the landing)
                if (edgePosition.y > trajectoryPoint.Value.position.y)
                {
                    InternalDebug.Log("Skipped landing position because it is below the edge, which would result in colliding with the back of the landing");
                    trajectoryPoint = trajectoryPoint.Next;
                    continue;
                }

                if (edgeToTrajectoryDistance < minEdgeToTrajectoryDistance)
                {
                    minEdgeToTrajectoryDistance = edgeToTrajectoryDistance;

                    bestPosition = new(landingPosition, trajectory, edgePosition, trajectoryPoint.Value.velocity, this, slope, isWaypoint, isTilted);
                    bestNode = trajectoryPoint;
                }

                trajectoryPoint = trajectoryPoint.Next;
            }

            if (bestPosition != null && bestPosition.IsValid())
            {
                trajectory.RemoveTrajectoryPointsAfter(bestNode);
                return bestPosition;
            }
            else
            {
                return null;
            }
        }

        private LandingPositionCarrier GetValidPointFromTrajectory(Trajectory trajectory)
        {
            if (TerrainManager.Instance.ActiveSlope != null)
            {
                return GetBestMatchingLandingPositionOnSlope(TerrainManager.Instance.ActiveSlope, trajectory);
            }
            else
            {
                LinkedListNode<Trajectory.TrajectoryPoint> bestNode = trajectory.GetPointAtHeight(TerrainManager.Instance.GlobalHeightLevel + invisibleBuilder.GetHeight());

                if (bestNode == null)
                {
                    return null;
                }

               
                // on flat ground, the best position for the trajectory point is one that places the landings edge directly below it
                Vector3 position = bestNode.Value.position;
                position.y = TerrainManager.Instance.GlobalHeightLevel;

                LandingPositionCarrier bestPosition = new(position, trajectory, bestNode.Value.position, bestNode.Value.velocity, this);

                if (!bestPosition.IsValid())
                {
                    return null; // no valid position found
                }

                trajectory.RemoveTrajectoryPointsAfter(bestNode);                
                
                return bestPosition;
            }
        }
        
        public void UpdateValidPositionList()
        {
            ClearPositionHighlights();

            List<LandingPositionCarrier> trajectoryInfos = CalculateValidLandingPositions();
            allowedTrajectoryPositions = new List<(LandingPositionCarrier info, MeshCollider highlight)>(trajectoryInfos.Count);

            List<MeshCollider> highlights = CreatePositionHighlights(trajectoryInfos);

            for (int i = 0; i < trajectoryInfos.Count; i++)
            {
                allowedTrajectoryPositions.Add((trajectoryInfos[i], highlights[i]));
            }

            if (allowedTrajectoryPositions.Count != 0)
            {
                allowedTrajectoryPositions[allowedTrajectoryPositions.Count / 2].info.MatchBuilder();
                buildButton.Toggle(true);
                GetComponent<MeshRenderer>().enabled = true;
            }
            else
            {
                StudioUIManager.Instance.ShowMessage("No valid positions available. Try lowering the height or changing the takeoff parameters.", 3f, MessagePriority.Medium);
                buildButton.Toggle(false);
            }
        }
        
        private SlopeBoundaryPoints GetSlopeBoundaryPoints(SlopeChange slope, LandingBase landing, Vector3 flightDirectionXZ)
        {
            Vector3 rideDirXZ = Vector3.ProjectOnPlane(landing.GetRideDirection(), Vector3.up).normalized;
            Vector3 slopeNormal = slope.GetNormal(rideDirXZ);

            Vector3? lastBeforeSlopeEdge = null;
            Vector3? firstOnSlopeEdge = null;
            Vector3? lastOnSlopeEdge = null;

            // slope is not yet started; landing can be placed on the flat before it or on the border of the slope start
            if (Mathf.Approximately(slope.RemainingLength, slope.Length))
            {
                Vector3 lastBeforeSlopePosition = slope.Start - landing.GetLandingAreaLengthXZ() * rideDirXZ;
                lastBeforeSlopePosition.y = slope.StartHeight; // set the height to the slope's end height
                lastBeforeSlopeEdge = lastBeforeSlopePosition + landing.GetHeight() * Vector3.up;

                // a landing placed on the border of the slope start has its position heightened because the slope starts earlier to support the landing from its start point
                if (landing.GetLength() < slope.RemainingLength)
                {
                    Vector3 firstOnSlopePosition = slope.Start + 0.01f * flightDirectionXZ - landing.GetLandingAreaLengthXZ() * rideDirXZ;
                    firstOnSlopePosition.y = slope.EndPoint.y - slope.GetHeightDifferenceForXZDistance(Vector3.Distance(slope.Start, firstOnSlopePosition));
                    firstOnSlopeEdge = firstOnSlopePosition + landing.GetHeight() * slopeNormal;
                }
            }

            // slope's remaining length is longer than the landing length; the whole landing can be placed on the slope
            if (landing.GetLength() < slope.RemainingLength)
            {
                if (!Mathf.Approximately(slope.RemainingLength, slope.Length))
                {
                    Vector3 firstOnSlopePosition = slope.EndPoint;
                    firstOnSlopeEdge = firstOnSlopePosition + landing.GetHeight() * slopeNormal;
                }

                Vector3 lastOnSlopePosition = slope.EndPoint + flightDirectionXZ * (slope.RemainingLength - landing.GetLandingAreaLengthXZ() - 0.01f);
                lastOnSlopePosition.y = slope.EndPoint.y + slope.GetHeightDifferenceForXZDistance(Vector3.Distance(slope.EndPoint, lastOnSlopePosition));
                lastOnSlopeEdge = lastOnSlopePosition + landing.GetHeight() * slopeNormal;
            }       
            
            // calculate the first position after the slope (landing is on flat ground)
            Vector3 firstAfterSlopePosition = slope.EndPoint + flightDirectionXZ * (slope.RemainingLength - landing.GetLandingAreaLengthXZ() - 0.01f);
            firstAfterSlopePosition.y = slope.EndHeight;
            Vector3 firstAfterSlopeEdge = firstAfterSlopePosition + landing.GetHeight() * Vector3.up;

            if (lastOnSlopeEdge.HasValue)
            {
                InternalDebug.DrawLine(lastOnSlopeEdge.Value, lastOnSlopeEdge.Value - slopeNormal * 5f, Color.red, 5f);
                InternalDebug.DrawLine(firstAfterSlopeEdge, firstAfterSlopeEdge - Vector3.up * 5f, Color.green, 5f);
            }

            return new SlopeBoundaryPoints(lastBeforeSlopeEdge, firstOnSlopeEdge, lastOnSlopeEdge, firstAfterSlopeEdge);
        }

        private LandingCandidate? GetLandingInfoForDesiredTrajectoryPoint(SlopeChange slope, LandingBase landing, Vector3 supposedLandingPoint, SlopeBoundaryPoints boundaryPoints)
        {
            Vector3 rideDirXZ = Vector3.ProjectOnPlane(landing.GetRideDirection(), Vector3.up).normalized;

            Vector3 slopeNormalLandingDir = slope.GetNormal(rideDirXZ);

            bool IsBeforeOrMatchesTrajectoryPoint(Vector3 edgePosition) => Vector3.Dot(supposedLandingPoint - edgePosition, rideDirXZ) >= 0;
            bool IsAfterTrajectoryPoint(Vector3 edgePosition) => Vector3.Dot(supposedLandingPoint - edgePosition, rideDirXZ) < 0;

            (Vector3? lastBeforeSlopeEdge, Vector3? firstOnSlopeEdge, Vector3? lastOnSlopeEdge, Vector3 firstAfterSlopeEdge) = boundaryPoints;

            Vector3 landingPosition;
            Vector3 edgePosition;
            bool isWaypoint = false;
            bool isTilted;

            if (lastBeforeSlopeEdge.HasValue && IsAfterTrajectoryPoint(lastBeforeSlopeEdge.Value))
            {
                landingPosition = supposedLandingPoint;
                landingPosition.y = slope.StartHeight;
                edgePosition = landingPosition + landing.GetHeight() * Vector3.up;
                isTilted = false;
            }
            else if (lastBeforeSlopeEdge.HasValue && IsBeforeOrMatchesTrajectoryPoint(lastBeforeSlopeEdge.Value) && firstOnSlopeEdge.HasValue && IsAfterTrajectoryPoint(firstOnSlopeEdge.Value))
            {
                landingPosition = lastBeforeSlopeEdge.Value - landing.GetHeight() * Vector3.up;
                edgePosition = lastBeforeSlopeEdge.Value;
                isTilted = false;
            }
            else if (firstOnSlopeEdge.HasValue && IsBeforeOrMatchesTrajectoryPoint(firstOnSlopeEdge.Value) && lastOnSlopeEdge.HasValue && IsAfterTrajectoryPoint(lastOnSlopeEdge.Value))
            {
                Vector3 slopeDirXZ = Vector3.ProjectOnPlane(supposedLandingPoint - slope.EndPoint, Vector3.up).normalized;
                Vector3 slopeDir = slope.TiltVectorBySlopeAngle(slopeDirXZ);
               
                Vector3 normalWithSlopeIntersection = slope.EndPoint + Vector3.Project(supposedLandingPoint - slope.EndPoint, slopeDir);
                float trajectoryToIntersectionDistance = Vector3.Distance(supposedLandingPoint, normalWithSlopeIntersection);

                float landingHeight = landing.GetHeight();
                float shiftToLandingPosition = Mathf.Tan(-slope.Angle) * (trajectoryToIntersectionDistance - landingHeight);

                landingPosition = normalWithSlopeIntersection + shiftToLandingPosition * slope.TiltVectorBySlopeAngle(rideDirXZ);
                edgePosition = landingPosition + landing.GetHeight() * slopeNormalLandingDir;

                isWaypoint = true;
                isTilted = true;
            }
            else if (lastOnSlopeEdge.HasValue && IsBeforeOrMatchesTrajectoryPoint(lastOnSlopeEdge.Value) && IsAfterTrajectoryPoint(firstAfterSlopeEdge))
            {
                landingPosition = lastOnSlopeEdge.Value - landing.GetHeight() * slopeNormalLandingDir;
                edgePosition = lastOnSlopeEdge.Value;
                isWaypoint = true;
                isTilted = true;
            }           
            else if (IsBeforeOrMatchesTrajectoryPoint(firstAfterSlopeEdge))
            {
                landingPosition = supposedLandingPoint;
                landingPosition.y = slope.EndHeight;
                edgePosition = landingPosition + landing.GetHeight() * Vector3.up;     
                isWaypoint = true;
                isTilted = false;
            }
            else
            {
                return null;
            }

            return new LandingCandidate(landingPosition, edgePosition, isWaypoint, isTilted);
        }

        public void UpdateMeasureText()
        {
            Vector3 toLanding = Vector3.ProjectOnPlane(builder.GetTransform().position - lastLineElement.GetTransform().position, Vector3.up);
            Vector3 rideDirProjected = Vector3.ProjectOnPlane(lastLineElement.GetRideDirection(), Vector3.up);
            textMesh.GetComponent<TextMeshPro>().text = $"Jump length: {builder.GetDistanceFromPreviousLineElement():F2}m" +
                $"\nAngle: {(int)Vector3.SignedAngle(rideDirProjected, toLanding, Vector3.up):F2}°"
                + $"\nExit speed: {PhysicsManager.PhysicsManager.MsToKmh(builder.GetExitSpeed()):F2}km/h";

            // make the text go along the line and lay flat on the terrain
            float camDistance = CameraManager.Instance.GetTDCamDistance();

            textMesh.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 6 * 5, camDistance)),
                Quaternion.LookRotation(-Vector3.up, Vector3.Cross(lastLineElement.GetRideDirection(), Vector3.up)));
        }

        public bool TrySetRotation(float angle)
        {            
            builder.SetRotation(angle);

            UpdateValidPositionList();
            
            if (allowedTrajectoryPositions.Count == 0)
            {
                buildButton.Toggle(false);
                StudioUIManager.Instance.ShowMessage("No valid landing positions available for this rotation. Either change it or adjust the line before this landing.", 3f);
                builder.CanBuild(false);
                return false;
            }

            buildButton.Toggle(true);

            builder.CanBuild(true);

            StudioUIManager.Instance.HideMessage();
            
            return true;
        }

        /// <summary>
        /// Calculates the slope angle to match the landing velocity direction.
        /// </summary>
        /// <param name="velocity">Velocity vector.</param>
        /// <param name="upDirection">Up direction of the landing to calculate the slope for.</param>
        /// <returns>The slope angle in radians.</returns>
        public static float GetSlopeFromLandingVelocity(Vector3 velocity, Vector3 upDirection)
        {
            Vector3 velocityForward = Vector3.ProjectOnPlane(velocity, upDirection);
            return Vector3.Angle(velocity, velocityForward) * Mathf.Deg2Rad;
        }

        public static float GetAngleBetweenRideDirAndVelocity(Vector3 rideDir, Vector3 velocity)
        {
            Vector3 rideDirXZ = Vector3.ProjectOnPlane(rideDir, Vector3.up);
            velocity = Vector3.ProjectOnPlane(velocity, Vector3.up);

            return Vector3.Angle(rideDirXZ, velocity);
        }
        
        private bool ValidatePosition(LandingBuilder invisibleBuilder)
        {
            // the distances are calculated on the XZ plane to avoid the influence of the height of the terrain
            Vector3 lastElementEnd = lastLineElement.GetEndPoint();
            lastElementEnd.y = 0;

            Vector3 lastElemRideDir = Vector3.ProjectOnPlane(lastLineElement.GetRideDirection(), Vector3.up);

            Vector3 toHit = Vector3.ProjectOnPlane(invisibleBuilder.GetTransform().position - lastElementEnd, Vector3.up);

            float edgeToEdgeDistance = Vector3.Distance(builder.PairedTakeoff.GetTransitionEnd(), invisibleBuilder.GetLandingPoint());

            // prevent the landing from being placed behind the takeoff
            float projection = Vector3.Dot(toHit, lastElemRideDir);
            if (projection < 0)
            {
                StudioUIManager.Instance.ShowMessage("Cannot place the landing at an angle larger than 90 degrees with respect to its takeoff.", 2f);
                return false;
            }
            
            if (edgeToEdgeDistance > LandingSettings.MaxDistanceFromTakeoff)
            {
                StudioUIManager.Instance.ShowMessage($"The new position is too far from the last line element. The maximum distance is {LandingSettings.MaxDistanceFromTakeoff}", 2f);
                return false;
            }
            
            if (edgeToEdgeDistance < LandingSettings.MinDistanceFromTakeoff)
            {
                StudioUIManager.Instance.ShowMessage($"The new position is too close to the last line element. The minimal distance is {LandingSettings.MaxDistanceFromTakeoff}", 2f);
                return false;
            }

            bool newPositionCollides = !TerrainManager.Instance.IsAreaFree(invisibleBuilder.GetStartPoint(), invisibleBuilder.GetEndPoint(), invisibleBuilder.GetBottomWidth(), builder.PairedTakeoff);
            if (newPositionCollides)
            {
                StudioUIManager.Instance.ShowMessage("The new obstacle position is colliding with a terrain change or another obstacle.", 2f);
                return false;
            }

            bool newRideoutAreaDoesNotCollide = TerrainManager.Instance.IsAreaFree(invisibleBuilder.GetEndPoint()
                , invisibleBuilder.GetEndPoint() + invisibleBuilder.GetRideDirection() * LandingSettings.RideoutClearanceDistance, clearanceWidth);

            if (!newRideoutAreaDoesNotCollide)
            {
                StudioUIManager.Instance.ShowMessage($"The rideout area after the landing is occupied by another obstacle or a terrain change.", 2f);
                return false;
            }            

            return true;
        }

        protected override bool TrySetPosition(Vector3 hit)
        {
            invisibleBuilder.SetPosition(hit);

            if (!ValidatePosition(invisibleBuilder))
            {
                invisibleBuilder.SetPosition(builder.GetTransform().position);
                buildButton.Toggle(false);
                return false;
            }

            buildButton.Toggle(true);

            StudioUIManager.Instance.HideMessage();

            // place the highlight at the hit point
            builder.SetPosition(hit);

            return true;            
        }

        
    }

    
}
