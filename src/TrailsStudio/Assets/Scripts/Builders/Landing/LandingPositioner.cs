using Assets.Scripts;
using Assets.Scripts.Managers;
using Assets.Scripts.States;
using Assets.Scripts.UI;
using Assets.Scripts.Utilities;
using System;
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
    

    /// <summary>
    /// Moves a highlight object anywhere after the last line element based on user input. <br/>
    /// Positions the highlight based on where the mouse is pointing on the terrain. Draws a line from the last line element to the highlight<br/>
    /// and shows distance from the line endpoint to the highlight + the Angle between the last line elements ride direction and the line.<br/>
    /// </summary>
    /// <remarks>Here, the highlight is the LandingBuilder mesh which is <b>attached to the same GameObject</b> as this highlighter.</remarks>
    public class LandingPositioner : Positioner
    {        
        [Header("Position highlight settings")]
        [SerializeField]
        GameObject positionHighlightPrefab;

        [SerializeField]
        float positionHighlightWidth = 0.1f;

        [SerializeField]
        float positionHighlightLength = 0.7f;

        [SerializeField]
        float positionHighlightHeightOffset = 0.3f;

        LandingBuilder builder;

        LandingBuilder invisibleBuilder;

        private Button buildButton;

        public class LandingPositionCarrier
        {
            public readonly Vector3 landingPosition;
            public readonly Trajectory trajectory;
            public readonly Vector3 edgePosition;
            public readonly Vector3 landingVelocityDirection;

            public readonly LandingPositioner positioner;

            public LandingPositionCarrier(Vector3 position, Trajectory trajectory, Vector3 edgePosition, Vector3 landingVelocityDirection, LandingPositioner positioner)
            {
                landingPosition = position;
                this.trajectory = trajectory;
                this.landingVelocityDirection = landingVelocityDirection.normalized;
                this.edgePosition = edgePosition;
                this.positioner = positioner;
            }            

            public virtual void MatchBuilder()
            {
                LandingBuilder landing = positioner.builder;
                landing.SetPosition(landingPosition);
                float slopeAngle = GetSlopeFromLandingVelocity(landingVelocityDirection, landing.GetTransform().up);
                landing.SetSlope(slopeAngle);

                landing.SetMatchingTrajectory(trajectory);
            }

            public virtual void MatchInvisibleBuilder()
            {
                LandingBuilder invisibleBuilder = positioner.invisibleBuilder;
                invisibleBuilder.SetPosition(landingPosition);
                float slopeAngle = GetSlopeFromLandingVelocity(landingVelocityDirection, invisibleBuilder.GetTransform().up);
                invisibleBuilder.SetSlope(slopeAngle);
            }            

            public bool IsValid()
            {
                float potentialSlopeDeg = GetSlopeFromLandingVelocity(landingVelocityDirection, positioner.invisibleBuilder.GetTransform().up) * Mathf.Rad2Deg;
                bool isSlopeAngleValid = potentialSlopeDeg >= LandingConstants.MIN_SLOPE_DEG && potentialSlopeDeg <= LandingConstants.MAX_SLOPE_DEG;

                if (!isSlopeAngleValid)
                {
                    return false;
                }

                float angleBetweenRideDirAndVelocity = GetAngleBetweenRideDirAndVelocity(positioner.builder.GetRideDirection(), landingVelocityDirection);

                if (angleBetweenRideDirAndVelocity > LandingConstants.MAX_ANGLE_BETWEEN_TRAJECTORY_AND_LANDING_DEG)
                {
                    return false;
                }


                MatchInvisibleBuilder();                

                if (!positioner.ValidatePosition(positioner.invisibleBuilder))
                {
                    return false;
                }

                return true;
            }
        }

        public class OnSlopeLandingPositionCarrier : LandingPositionCarrier
        {
            public readonly SlopeChange slope;
            public readonly bool isTilted;
            public readonly bool isWaypoint;

            public OnSlopeLandingPositionCarrier(Vector3 position, Trajectory trajectory, Vector3 edgePosition, Vector3 landingVelocityDirection, LandingPositioner positioner, SlopeChange slope, bool isWaypoint, bool isTilted)
                : base(position, trajectory, edgePosition, landingVelocityDirection, positioner)
            {
                this.slope = slope;
                this.isWaypoint = isWaypoint;

                this.isTilted = isTilted;
            }

            public override void MatchBuilder()
            {
                LandingBuilder landing = positioner.builder;

                if (isWaypoint)
                {
                    slope.PlaceLanding(landingPosition, isTilted, landing);
                }
                else
                {
                    landing.SetPosition(landingPosition);
                }

                float slopeAngle = GetSlopeFromLandingVelocity(landingVelocityDirection, landing.GetTransform().up);
                landing.SetSlope(slopeAngle);

                landing.SetMatchingTrajectory(trajectory);
            }

            public override void MatchInvisibleBuilder()
            {
                LandingBuilder invisibleBuilder = positioner.invisibleBuilder;

                if (isWaypoint)
                {
                    slope.PlaceLanding(landingPosition, isTilted, invisibleBuilder);
                }
                else
                {
                    invisibleBuilder.SetPosition(landingPosition);
                }

                float slopeAngle = GetSlopeFromLandingVelocity(landingVelocityDirection, invisibleBuilder.GetTransform().up);
                invisibleBuilder.SetSlope(slopeAngle);
            }
        }
 
        public List<(LandingPositionCarrier info, MeshCollider highlight)> AllowedTrajectoryPositions { get; private set; } = new();

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

            buildButton = UIManager.Instance.landingBuildUI.GetComponent<LandingBuildUI>().BuildButton;

            CanMoveHighlight = false;

            UpdateValidPositionList();            
        }       
        

        void OnParamChanged<T>(object sender, ParamChangeEventArgs<T> e) => UpdateMeasureText();        


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
            terrainLayerMask = LayerMask.GetMask("Position Highlight");
        }

        protected override void FixedUpdate()
        {
            if (CanMoveHighlight && AllowedTrajectoryPositions.Count > 0)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayerMask))
                {
                    hit.collider.GetComponent<PositionHolder>().TrajectoryPositionInfo.MatchBuilder();
                }
            }
        }

        MeshCollider CreatePositionHighlight(Vector3[] vertices, int[] triangles, LandingPositionCarrier positionTrajectoryInfo)
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

            positionHolder.Init(positionTrajectoryInfo);

            return meshCollider;
        }

        List<MeshCollider> CreatePositionHighlights(List<LandingPositionCarrier> positions)
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

            float highlightHeight;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                SlopeChange slope = TerrainManager.Instance.ActiveSlope;

                // if the slope goes upwards, put the highlights on its end height level.
                if (slope.HeightDifference > 0)
                {
                    highlightHeight = slope.GetFinishedEndPoint().y;
                }
                // if it goes downwards, put them at the current end point height
                else
                {
                    highlightHeight = slope.EndPoint.y;
                }                
            }
            else
            {
                highlightHeight = TerrainManager.GetHeightAt(positions[0].landingPosition);
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

        void ClearPositionHighlights()
        {
            if (AllowedTrajectoryPositions == null || AllowedTrajectoryPositions.Count == 0)
            {
                return;
            }

            foreach (var highlight in AllowedTrajectoryPositions)
            {
                if (highlight.highlight == null)
                {
                    continue; // skip if the highlight is already destroyed
                }

                Destroy(highlight.highlight.gameObject);
            }
            AllowedTrajectoryPositions.Clear();
        }
                       
        public List<LandingPositionCarrier> CalculateValidLandingPositions(float normalizedAngleStep = 0.05f)
        {
            var trajectoryInfos = new List<LandingPositionCarrier>();
            for (float normalizedAngle = -1; normalizedAngle <= 1; normalizedAngle += normalizedAngleStep)
            {                
                Trajectory trajectory = PhysicsManager.GetFlightTrajectory(builder.PairedTakeoff, normalizedAngle);                
                LandingPositionCarrier trajectoryInfo = GetValidPointFromTrajectory(trajectory);                                

                if (trajectoryInfo != null)
                {
                    trajectoryInfos.Add(trajectoryInfo);
                }

            }   
            
            return trajectoryInfos;
        }

        OnSlopeLandingPositionCarrier GetBestMatchingLandingPositionOnSlope(SlopeChange slope, Trajectory trajectory)
        {
            LinkedListNode<Trajectory.TrajectoryPoint> trajectoryPoint = trajectory.Apex;
            float minEdgeToTrajectoryDistance = float.MaxValue;
            OnSlopeLandingPositionCarrier bestPosition = null;
            LinkedListNode<Trajectory.TrajectoryPoint> bestNode = null;

            Vector3 flightDirectionXZ = Vector3.ProjectOnPlane(trajectoryPoint.Value.velocity, Vector3.up).normalized;
            SlopeBoundaryPoints boundaryPoints = slope.GetBoundaryPoints(invisibleBuilder, flightDirectionXZ);

            while (trajectoryPoint != null)
            {
                SlopeChange.MatchingLandingPosition? matchingLandingPosition = slope.GetLandingInfoForDesiredTrajectoryPoint(invisibleBuilder, trajectoryPoint.Value.position, boundaryPoints);

                if (!matchingLandingPosition.HasValue)
                {
                    trajectoryPoint = trajectoryPoint.Next;
                    continue;
                }

                (Vector3 landingPosition, Vector3 edgePosition, bool isWaypoint, bool isTilted) = matchingLandingPosition.Value;
                
                float edgeToTrajectoryDistance = Vector3.Distance(edgePosition, trajectoryPoint.Value.position);

                // skip suppposed landing points that are below the edge (results in colliding with the back of the landing)
                if (edgePosition.y > trajectoryPoint.Value.position.y)
                {
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

        LandingPositionCarrier GetValidPointFromTrajectory(Trajectory trajectory)
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
                    Debug.Log("invalid position");
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
            AllowedTrajectoryPositions = new List<(LandingPositionCarrier info, MeshCollider highlight)>(trajectoryInfos.Count);

            List<MeshCollider> highlights = CreatePositionHighlights(trajectoryInfos);

            for (int i = 0; i < trajectoryInfos.Count; i++)
            {
                AllowedTrajectoryPositions.Add((trajectoryInfos[i], highlights[i]));
            }

            if (AllowedTrajectoryPositions.Count != 0)
            {
                AllowedTrajectoryPositions[AllowedTrajectoryPositions.Count / 2].info.MatchBuilder();
                UIManager.ToggleButton(buildButton, true);
                GetComponent<MeshRenderer>().enabled = true;
            }
            else
            {
                UIManager.Instance.ShowMessage("No valid positions available. Try lowering the height or changing the takeoff parameters.", 3f, UIManager.MessagePriority.Medium);
                UIManager.ToggleButton(buildButton, false);
            }
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
            builder.SetRotation(angle);

            UpdateValidPositionList();
            
            if (AllowedTrajectoryPositions.Count == 0)
            {
                UIManager.ToggleButton(buildButton, false);
                UIManager.Instance.ShowMessage("No valid landing positions available for this rotation. Either change it or adjust the line before this landing.", 3f);
                builder.CanBuild(false);
                return false;
            }

            UIManager.ToggleButton(buildButton, true);

            builder.CanBuild(true);

            UIManager.Instance.HideMessage();
            
            return true;
        }

        /// <summary>
        /// Calculates the slope angle to match the landing velocity direction.
        /// </summary>
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
        
        public bool ValidatePosition(LandingBuilder invisibleBuilder)
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
                UIManager.Instance.ShowMessage("Cannot place the landing at an angle larger than 90 degrees with respect to its takeoff.", 2f);
                return false;
            }
            else if (edgeToEdgeDistance > LandingConstants.MAX_DISTANCE_FROM_TAKEOFF)
            {
                UIManager.Instance.ShowMessage($"The new position is too far from the last line element. The maximum distance is {LandingConstants.MAX_DISTANCE_FROM_TAKEOFF}m", 2f);
                return false;
            }
            else if (edgeToEdgeDistance < LandingConstants.MIN_DISTANCE_FROM_TAKEOFF)
            {
                UIManager.Instance.ShowMessage($"The new position is too close to the last line element. The minimal distance is {LandingConstants.MAX_DISTANCE_FROM_TAKEOFF}m", 2f);
                return false;
            }

            bool newPositionCollides = !TerrainManager.Instance.IsAreaFree(invisibleBuilder.GetStartPoint(), invisibleBuilder.GetEndPoint(), invisibleBuilder.GetBottomWidth(), builder.PairedTakeoff);
            if (newPositionCollides)
            {
                UIManager.Instance.ShowMessage("The new obstacle position is colliding with a terrain change or another obstacle.", 2f);
                return false;
            }

            bool newRideoutAreaDoesNotCollide = TerrainManager.Instance.IsAreaFree(invisibleBuilder.GetEndPoint()
                , invisibleBuilder.GetEndPoint() + invisibleBuilder.GetRideDirection() * LandingConstants.RIDEOUT_CLEARANCE_DISTANCE, clearanceWidth);

            if (!newRideoutAreaDoesNotCollide)
            {
                UIManager.Instance.ShowMessage($"The rideout area after the landing is occupied by another obstacle or a terrain change.", 2f);
                return false;
            }            

            return true;
        }

        public override bool TrySetPosition(Vector3 hit)
        {
            invisibleBuilder.SetPosition(hit);

            if (!ValidatePosition(invisibleBuilder))
            {
                invisibleBuilder.SetPosition(builder.GetTransform().position);
                UIManager.ToggleButton(buildButton, false);
                return false;
            }

            UIManager.ToggleButton(buildButton, true);

            UIManager.Instance.HideMessage();

            // place the highlight at the hit point
            builder.SetPosition(hit);

            return true;            
        }

        
    }

    
}
