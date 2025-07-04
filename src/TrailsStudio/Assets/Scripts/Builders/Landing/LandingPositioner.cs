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
    public readonly struct LandingPositionMatchedToTrajectory
    {
        public readonly Vector3 landingPosition;
        public readonly Trajectory trajectory;
        public readonly Trajectory.TrajectoryPoint supposedLandingPoint;

        public LandingPositionMatchedToTrajectory(Vector3 position, Trajectory trajectory, Trajectory.TrajectoryPoint supposedLandingPoint)
        {
            landingPosition = position;
            this.trajectory = trajectory;
            this.supposedLandingPoint = supposedLandingPoint;
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

        List<(LandingPositionMatchedToTrajectory info, MeshCollider highlight)> allowedTrajectoryPositions = new();

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
            builder.HeightChanged += OnHeightChanged;
            builder.ExitSpeedChanged += OnParamChanged;            

            builder.SetRideDirection(lastLineElement.GetRideDirection());

            UpdateValidPositionList();

            if (allowedTrajectoryPositions.Count > 0)
            {
                CompleteSetup(); // Call the method to finalize setup with valid positions
            }
            else
            {
                //TODO the ui is still kinda wonky, it just blinks and then dissapears
                UIManager.Instance.ShowMessage("No valid landing positions available. Please wait until a landing height with a valid position is found..", priority: UIManager.MessagePriority.Medium);

                StartCoroutine(FindValidLandingHeightCoroutine());                
            }

        }

        // Extracted method to complete setup once valid positions are available
        private void CompleteSetup()
        {
            // select the middle point of the allowed trajectory positions as the initial position
            if (allowedTrajectoryPositions.Count > 0)
            {
                LandingPositionMatchedToTrajectory trajectoryPoint = allowedTrajectoryPositions[allowedTrajectoryPositions.Count / 2].info;
                MatchLandingToTrajectoryPoint(trajectoryPoint);

                CanMoveHighlight = false;
                GetComponent<MeshRenderer>().enabled = true;
                UpdateMeasureText();
            }
        }

        private IEnumerator FindValidLandingHeightCoroutine()
        {
            // Yield once to ensure UI message is displayed
            yield return null;

            // Try different heights
            for (float height = builder.GetHeight(); height >= LandingConstants.MIN_HEIGHT; height -= 0.1f)
            {
                invisibleBuilder.SetHeight(height);
                UpdateValidPositionList();

                // Yield every few iterations to keep UI responsive
                if (height % 0.5f < 0.1f)
                    yield return null;

                if (allowedTrajectoryPositions.Count > 0)
                {
                    UIManager.Instance.HideMessage();
                    builder.SetHeight(height);

                    CompleteSetup(); // Call the method to finalize setup with valid positions
                    yield break; // Exit coroutine
                }
            }

            // Couldn't find any valid position
            UIManager.Instance.ShowMessage("No valid landing positions for any height. Please adjust the takeoff.",
                5f, UIManager.MessagePriority.Medium);
            builder.Cancel();

            if (TerrainManager.Instance.ActiveSlope != null)
                TerrainManager.Instance.ActiveSlope.LastConfirmedSnapshot.Revert();

            TakeoffBuilder takeoffBuilder = (Line.Instance.GetLastLineElement() as Takeoff).Revert();
            StateController.Instance.ChangeState(new TakeOffBuildState(takeoffBuilder.GetComponent<TakeoffPositioner>()));
        }

        void OnParamChanged<T>(object sender, ParamChangeEventArgs<T> e) => UpdateMeasureText();

        void OnHeightChanged(object sender, ParamChangeEventArgs<float> e)
        {            
            // Recalculate the valid positions based on the new height
            UpdateValidPositionList();
            if (allowedTrajectoryPositions.Count == 0)
            {
                UIManager.Instance.ShowMessage("No valid landing positions found. Please adjust the takeoff angle or position.", 10f, UIManager.MessagePriority.Medium);
            }
            else
            {
                MatchLandingToTrajectoryPoint(allowedTrajectoryPositions[allowedTrajectoryPositions.Count / 2].info);
            }
        }


        protected override void OnDisable()
        {
            ClearPositionHighlights();
            builder.PositionChanged -= OnParamChanged;
            builder.RotationChanged -= OnParamChanged;
            builder.SlopeChanged -= OnParamChanged;
            builder.HeightChanged -= OnParamChanged;
            builder.HeightChanged -= OnHeightChanged;
            builder.ExitSpeedChanged -= OnParamChanged;
            base.OnDisable();
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

        MeshCollider CreatePositionHighlight(Vector3[] vertices, int[] triangles, LandingPositionMatchedToTrajectory positionTrajectoryInfo)
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

        List<MeshCollider> CreatePositionHighlights(List<LandingPositionMatchedToTrajectory> positions)
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
                highlightHeight = TerrainManager.Instance.ActiveSlope.EndPoint.y;
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

        public List<LandingPositionMatchedToTrajectory> CalculateValidLandingPositions(float normalizedAngleStep = 0.05f)
        {
            var trajectoryInfos = new List<LandingPositionMatchedToTrajectory>();
            for (float normalizedAngle = -1; normalizedAngle <= 1; normalizedAngle += normalizedAngleStep)
            {
                Trajectory trajectory = PhysicsManager.GetFlightTrajectory(builder.PairedTakeoff, normalizedAngle, 0.025f);                
                LandingPositionMatchedToTrajectory? trajectoryInfo = GetValidPointFromTrajectory(trajectory);

                if (!trajectoryInfo.HasValue)
                {
                    continue;
                }

                invisibleBuilder.SetPosition(trajectoryInfo.Value.landingPosition);

                float potentialSlopeDeg = GetSlopeFromLandingVelocity(trajectoryInfo.Value.supposedLandingPoint.velocity, invisibleBuilder.GetTransform().up) * Mathf.Rad2Deg;
                bool isSlopeAngleValid = potentialSlopeDeg >= LandingConstants.MIN_SLOPE_DEG && potentialSlopeDeg <= LandingConstants.MAX_SLOPE_DEG;

                if (ValidatePosition(invisibleBuilder) && isSlopeAngleValid)
                {
                    trajectoryInfos.Add(trajectoryInfo.Value);
                }                

                //trajectoryInfos.Add(trajectoryInfo.Value);

            }   
            
            invisibleBuilder.SetPosition(builder.GetTransform().position);                

            UIManager.Instance.HideMessage();

            return trajectoryInfos;
        }

        LandingPositionMatchedToTrajectory? GetValidPointFromTrajectory(Trajectory trajectory)
        {
            LinkedListNode<Trajectory.TrajectoryPoint> bestNode = null;

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                LinkedListNode<Trajectory.TrajectoryPoint> trajectoryPoint = trajectory.Apex;
                SlopeChange slopeChange = TerrainManager.Instance.ActiveSlope;
                float minEdgeToTrajectoryDistance = float.MaxValue;
                LandingPositionMatchedToTrajectory? bestPoint = null;

                Vector3 flightDirectionXZ = Vector3.ProjectOnPlane(trajectoryPoint.Value.velocity, Vector3.up);
                SlopeBoundaryPoints boundaryPoints = slopeChange.GetBoundaryPoints(invisibleBuilder, flightDirectionXZ);
                
                while (trajectoryPoint != null)
                {
                    SlopeChange.MatchingLandingPosition? matchingLandingPosition = slopeChange.GetLandingInfoForDesiredTrajectoryPoint(invisibleBuilder, trajectoryPoint.Value.position, boundaryPoints);

                    if (!matchingLandingPosition.HasValue)
                    {
                        //Debug.Log("Skipping trajectory point due to no matching landing position.");
                        trajectoryPoint = trajectoryPoint.Next;
                        continue;
                    }

                    (Vector3 landingPosition, Vector3 edgePosition, Vector3 landingDir) = matchingLandingPosition.Value;

                    float edgeToTrajectoryDistance = Vector3.Distance(edgePosition, trajectoryPoint.Value.position);

                    // skip suppposed landing points that are below the edge (results in colliding with the back of the landing) or too far from the edge
                    if (edgePosition.y > trajectoryPoint.Value.position.y || edgeToTrajectoryDistance > 0.3f)
                    {
                        trajectoryPoint = trajectoryPoint.Next;
                        continue;
                    }                    

                    if (edgeToTrajectoryDistance < minEdgeToTrajectoryDistance)
                    {
                        //Debug.Log("reassigning best point");
                        minEdgeToTrajectoryDistance = edgeToTrajectoryDistance;

                        bestPoint = new(landingPosition, trajectory, new(edgePosition, landingDir));
                        bestNode = trajectoryPoint;
                    }

                    trajectoryPoint = trajectoryPoint.Next;
                }

                // remove all trajectory points after the best one (landing point)
                while (bestNode != null && bestNode.Next != null)
                {
                    trajectory.RemoveLast();
                }

                return bestPoint;
            }
            else
            {
                bestNode = trajectory.GetPointAtHeight(invisibleBuilder.GetHeight());

                if (bestNode == null)
                {
                    return null;
                }

                Vector3 position = bestNode.Value.position;
                position.y = TerrainManager.GetHeightAt(position);

                while (bestNode.Next != null)
                {
                    trajectory.RemoveLast();
                }

                return new(position, trajectory, bestNode.Value);
            }
        }
        
        public void UpdateValidPositionList()
        {
            ClearPositionHighlights();

            List<LandingPositionMatchedToTrajectory> trajectoryInfos = CalculateValidLandingPositions();
            allowedTrajectoryPositions = new List<(LandingPositionMatchedToTrajectory info, MeshCollider highlight)>(trajectoryInfos.Count);

            List<MeshCollider> highlights = CreatePositionHighlights(trajectoryInfos);

            for (int i = 0; i < trajectoryInfos.Count; i++)
            {
                allowedTrajectoryPositions.Add((trajectoryInfos[i], highlights[i]));
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
            // the distances are calculated on the XZ plane to avoid the influence of the height of the terrain
            Vector3 lastElementEnd = lastLineElement.GetEndPoint();
            lastElementEnd.y = 0;            

            invisibleBuilder.SetRotation(angle);
           
            if (!ValidatePosition(invisibleBuilder))
            {
                invisibleBuilder.SetRotation(builder.GetRotation());
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

        //TODO use
        public static float GetAngleBetweenLandingAndVelocity(Vector3 landingDirection, Vector3 velocity)
        {
            landingDirection = Vector3.ProjectOnPlane(landingDirection, Vector3.up);
            velocity = Vector3.ProjectOnPlane(velocity, Vector3.up);

            return Vector3.SignedAngle(landingDirection, velocity, Vector3.up) * Mathf.Deg2Rad;
        }

        private void MatchLandingToTrajectoryPoint(LandingPositionMatchedToTrajectory trajectoryInfo)
        {
            float slopeAngle = GetSlopeFromLandingVelocity(trajectoryInfo.supposedLandingPoint.velocity, transform.up);
            builder.SetSlope(slopeAngle);
            builder.SetPosition(trajectoryInfo.landingPosition);
            
            builder.SetMatchingTrajectory(trajectoryInfo.trajectory);
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
                Debug.Log("placement behind the last line element");
                UIManager.Instance.ShowMessage("Cannot place the landing at an angle larger than 90 degrees with respect to its takeoff.", 2f);
                return false;
            }
            else if (edgeToEdgeDistance > LandingConstants.MAX_DISTANCE_FROM_TAKEOFF)
            {
                Debug.Log("placement too far from the last line element");
                UIManager.Instance.ShowMessage($"The new position is too far from the last line element. The maximum distance is {LandingConstants.MAX_DISTANCE_FROM_TAKEOFF}m", 2f);
                return false;
            }
            else if (edgeToEdgeDistance < LandingConstants.MIN_DISTANCE_FROM_TAKEOFF)
            {
                Debug.Log("placement too close to the last line element");
                UIManager.Instance.ShowMessage($"The new position is too close to the last line element. The minimal distance is {LandingConstants.MAX_DISTANCE_FROM_TAKEOFF}m", 2f);
                return false;
            }

            bool newPositionCollides = !TerrainManager.Instance.IsAreaFree(invisibleBuilder.GetStartPoint(), invisibleBuilder.GetEndPoint(), invisibleBuilder.GetBottomWidth(), builder.PairedTakeoff);
            if (newPositionCollides)
            {
                Debug.Log("placement collides with terrain change or another obstacle");
                UIManager.Instance.ShowMessage("The new obstacle position is colliding with a terrain change or another obstacle.", 2f);
                return false;
            }

            bool newRideoutAreaDoesNotCollide = TerrainManager.Instance.IsAreaFree(invisibleBuilder.GetEndPoint()
                , invisibleBuilder.GetEndPoint() + invisibleBuilder.GetRideDirection() * LandingConstants.RIDEOUT_CLEARANCE_DISTANCE, clearanceWidth);

            if (!newRideoutAreaDoesNotCollide)
            {
                Debug.Log("placement rideout area collides");
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
                return false;
            }

            UIManager.Instance.HideMessage();

            // place the highlight at the hit point
            builder.SetPosition(hit);

            return true;            
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            if (allowedTrajectoryPositions != null && allowedTrajectoryPositions.Count > 0)
            {
                foreach (var highlight in allowedTrajectoryPositions)
                {
                    Gizmos.DrawLine(highlight.info.landingPosition, highlight.info.supposedLandingPoint.position);
                }
            }
        }
    }

    
}
