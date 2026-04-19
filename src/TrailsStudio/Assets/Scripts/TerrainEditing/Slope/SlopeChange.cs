using System.Collections.Generic;
using LineSystem;
using Managers;
using Misc;
using Obstacles;
using Obstacles.Landing;
using Obstacles.TakeOff;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TerrainEditing.Slope
{


    public partial class SlopeChange : SlopeChangeBase, ISaveable<SlopeData>
    {
        /// <summary>
        /// Prefab used to instantiate highlight markers at the start and end points of the slope.
        /// </summary>
        [SerializeField] GameObject endPointHighlightPrefab;

        /// <summary>
        /// List of instantiated highlight markers for the slope's endpoints.
        /// </summary>
        readonly List<GameObject> endPointHighlights = new();

        /// <summary>
        /// The UI object displaying information text about the slope.
        /// </summary>
        GameObject infoText;

        /// <summary>
        /// The portion of the terrain that goes from the last line element before the slope's start to the slope's start.
        /// </summary>
        public TerrainManager.HeightmapCoordinates FlatToStartPoint => TerrainManager.Instance.GetCoordinatesForArea(
            PreviousLineElement.GetEndPoint(), Start, PreviousLineElement.GetBottomWidth());

        /// <summary>
        /// Retrieves the last line element that has been placed on the slope.
        /// </summary>
        public ILineElement LastElementOnSlope { get; private set; }

        public float RemainingLength { get; private set; }

        public WaypointList Waypoints { get; private set; }

        /// <summary>
        /// The end point of the slope. If the slope is not finished, this is an end point of the realized portion of the slope.
        /// </summary>
        public Vector3 EndPoint { get; private set; }

        public Vector3 LastRideDirection { get; private set; }


        /// <summary>
        /// The most recent saved state of the slope, used to revert unconfirmed changes.
        /// </summary>
        public SlopeSnapshot LastConfirmedSnapshot { get; private set; }

        public override bool Finished => RemainingLength <= 0;

        public bool IsBuiltOn => Waypoints.Count > 0;

        /// <summary>
        /// Creates a snapshot of the current state of the slope.
        /// </summary>
        public SlopeSnapshot GetSlopeSnapshot() => new(this);

        /// <summary>
        /// Tracks the result of the last attempted obstacle placement on the slope.
        /// </summary>
        public PlacementResult LastPlacementResult { get; private set; }

        /// <summary>
        /// Updates the visual decal highlight to represent the remaining available portion of the slope.
        /// </summary>
        protected override void UpdateHighlight()
        {
            if (Finished || Length == 0 || RemainingLength == 0)
            {
                Highlight.enabled = false;
                return;
            }

            Highlight.enabled = true;

            Vector3 rideDirNormal = Vector3.Cross(LastRideDirection, Vector3.up).normalized;

            Vector3 position = Vector3.Lerp(EndPoint, EndPoint + RemainingLength * LastRideDirection, 0.5f);
            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, rideDirNormal);

            transform.SetPositionAndRotation(position, rotation);

            DecalProjector decalProjector = Highlight.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(RemainingLength, Width, 20);
        }

        private void UpdateHighlight(float remainingLength, Vector3 highlightStart, Vector3 direction)
        {
            if (Finished || Length == 0 || remainingLength == 0)
            {
                Highlight.enabled = false;
                return;
            }

            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;

            Highlight.enabled = true;

            Vector3 rideDirNormal = Vector3.Cross(direction, Vector3.up).normalized;

            Vector3 position = Vector3.Lerp(highlightStart, highlightStart + remainingLength * direction, 0.5f);

            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, rideDirNormal);

            transform.SetPositionAndRotation(position, rotation);

            DecalProjector decalProjector = Highlight.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(remainingLength, Width, 20);
        }

        /// <summary>
        /// Initializes a slope with a given start point, end height, and length.
        /// </summary>
        public void Initialize(Vector3 start, float endHeight, float length)
        {
            Waypoints = new WaypointList(this);
            StartHeight = start.y;
            this.EndHeight = endHeight;

            Start = start;
            EndPoint = start;

            Length = length;
            RemainingLength = length;

            UpdateAngle();

            PreviousLineElement = Line.Instance.GetLastLineElement();
            LastRideDirection = PreviousLineElement.GetRideDirection();
            Width = PreviousLineElement.GetBottomWidth();

            Highlight = GetComponent<DecalProjector>();
            Highlight.material.color = Color.green;

            UpdateHighlight();

            LastPlacementResult = new(this);
            LastConfirmedSnapshot = GetSlopeSnapshot();
        }

        /// <summary>
        /// Generates the text details to be displayed when showing information about the slope.
        /// </summary>
        private List<(string name, string value)> GetInfoText()
        {
            List<(string name, string value)> info = new()
            {
                ("Length", $"{Length:0.##}m"),
                ("Angle", $"{Angle * Mathf.Rad2Deg:0}°"),
                ("Height difference", $"{EndHeight - StartHeight:0.##}m"),
            };
            return info;
        }

        /// <summary>
        /// Displays the informational UI and endpoint highlights for this slope.
        /// </summary>
        public void ShowInfo()
        {
            // offset the info text to the side of the slope
            Vector3 infoTextPos = Start + Vector3.Cross(Camera.main.transform.forward, Vector3.up).normalized * 5f +
                                  Vector3.up * 4f;
            infoText = StudioUIManager.Instance.ShowSlopeInfo(GetInfoText(), infoTextPos, transform, Start);

            endPointHighlights.Add(Instantiate(endPointHighlightPrefab, Start, Quaternion.identity));
            endPointHighlights[0].transform.parent = transform;
            endPointHighlights.Add(Instantiate(endPointHighlightPrefab, EndPoint, Quaternion.identity));
            endPointHighlights[1].transform.parent = transform;
        }

        /// <summary>
        /// Hides the informational UI and removes the endpoint highlights for this slope.
        /// </summary>
        public void HideInfo()
        {
            foreach (var highlight in endPointHighlights)
            {
                Destroy(highlight);
            }

            endPointHighlights.Clear();

            if (infoText != null)
            {
                Destroy(infoText);
            }
        }

        /// <summary>
        /// Returns whether a position is before the Start of this slope. 
        /// </summary>        
        /// <returns>True if the slope has no waypoints and the position is before the slope's start, false if otherwise.</returns>
        public bool IsBeforeStart(Vector3 position)
        {
            if (Waypoints.Count > 0)
            {
                // if waypoints are added, the slope can go in any direction, even before its start,
                // so we can't check if the position is before the slope's start
                return false;
            }

            Vector3 slopeStartToPosition = Vector3.ProjectOnPlane(position - Start, Vector3.up);
            float projection = Vector3.Dot(slopeStartToPosition, LastRideDirection);

            return projection < 0; // if the position is before the slope start, the projection is negative
        }

        /// <summary>
        /// Checks if a position lies within the currently active, unbuilt portion of the slope.
        /// </summary>
        /// <returns>True if the slope is not yet finished 
        /// and the position is on the part from current endpoint to the potential finished endpoint of the slope.</returns>
        public bool IsOnActivePartOfSlope(Vector3 position)
        {
            if (Finished)
            {
                return false;
            }

            Vector3 endPointToPosition = Vector3.ProjectOnPlane(position - EndPoint, Vector3.up).normalized;
            float projection = Vector3.Dot(endPointToPosition, LastRideDirection);

            Vector3 endPointXZ = new(EndPoint.x, 0, EndPoint.z);
            Vector3 positionXZ = new(position.x, 0, position.z);

            return Vector3.Distance(endPointXZ, positionXZ) <= RemainingLength && projection >= 0;
        }

        /// <summary>
        /// Checks if a position is located beyond the determined end of the slope.
        /// </summary>
        public bool IsAfterSlope(Vector3 position)
        {
            Vector3 endPointToPosition = Vector3.ProjectOnPlane(position - EndPoint, Vector3.up).normalized;
            float projection = Vector3.Dot(endPointToPosition, LastRideDirection);

            Vector3 endPointXZ = new(EndPoint.x, 0, EndPoint.z);
            Vector3 positionXZ = new(position.x, 0, position.z);

            return projection > 0 && Vector3.Distance(endPointXZ, positionXZ) > RemainingLength;
        }

        /// <summary>
        /// Returns the end point of the slope if finished, otherwise where the slope will end in the current <see cref="LastRideDirection"/>
        /// </summary>
        public Vector3 GetFinishedEndPoint()
        {
            if (Finished)
            {
                return EndPoint;
            }

            Vector3 result = EndPoint + LastRideDirection * RemainingLength;
            result.y = EndHeight;
            return result;
        }

        /// <summary>
        /// Calculates the height difference for a given XZ distance using the slope's Angle.
        /// </summary>
        /// <remarks>Is not bounded by the slope's <see cref="RemainingLength"/></remarks>
        private float GetHeightDifferenceForXZDistance(float distance)
        {
            float heightDif = distance * Mathf.Tan(Angle);
            return heightDif;
        }

        /// <summary>
        /// Converts a horizontal (XZ plane) distance to the actual distance along the slope.
        /// </summary>
        /// <param name="xzDistance">The distance on the XZ plane.</param>
        /// <returns>The slope Length that corresponds to the given XZ distance.</returns>
        public float GetSlopeLengthFromXZDistance(float xzDistance)
        {
            return xzDistance / Mathf.Cos(Mathf.Abs(Angle));
        }

        /// <summary>
        /// Converts a distance along the slope to its horizontal (XZ plane) equivalent distance.
        /// </summary>
        /// <param name="slopeLength">The distance on the slope</param>
        /// <returns>The XZ plane distance that corresponds to the given slope distance</returns>
        public float GetXZDistanceFromSlopeLength(float slopeLength)
        {
            return slopeLength * Mathf.Cos(Mathf.Abs(Angle));
        }

        /// <summary>
        /// Computes the upward normal vector for the slope based on a given horizontal ride direction.
        /// </summary>
        public Vector3 GetNormal(Vector3 rideDir)
        {
            Quaternion tiltToAngle =
                Quaternion.AngleAxis(Angle * Mathf.Rad2Deg, -Vector3.Cross(Vector3.up, rideDir).normalized);
            Vector3 normal = tiltToAngle * Vector3.up;
            return normal.normalized;
        }

        /// <summary>
        /// Tilts a given horizontal vector so that it aligns with the slope's angle.
        /// </summary>
        public Vector3 TiltVectorBySlopeAngle(Vector3 vector)
        {
            Vector3 flatDirection = Vector3.ProjectOnPlane(vector, Vector3.up).normalized;
            Quaternion tiltToAngle = Quaternion.AngleAxis(Angle * Mathf.Rad2Deg,
                -Vector3.Cross(Vector3.up, flatDirection).normalized);
            return tiltToAngle * vector;
        }

        /// <summary>
        /// Rotates the obstacle so that its forward direction is aligned with the slope's angled ride direction
        /// and places it on the terrain at the correct height.
        /// </summary>
        private void FitObstacleOnSlope(IObstacleBuilder builder)
        {
            Vector3 rideDir = Vector3.ProjectOnPlane(builder.GetRideDirection(), Vector3.up).normalized;
            Vector3 angledRideDir = TiltVectorBySlopeAngle(rideDir);

            builder.GetTransform().forward = angledRideDir;
            float newHeight = TerrainManager.Instance.GetHeightAt(builder.GetTransform().position);
            builder.GetTransform().position = new(builder.GetTransform().position.x, newHeight,
                builder.GetTransform().position.z);
        }

        /// <summary>
        /// Determines the boundary placement points for an obstacle on the slope based on its trajectory.
        /// </summary>
        public SlopeBoundaryPoints GetBoundaryPoints(LandingBase landing, Vector3 flightDirectionXZ)
        {
            Vector3 rideDirXZ = Vector3.ProjectOnPlane(landing.GetRideDirection(), Vector3.up).normalized;
            Vector3 slopeNormal = GetNormal(rideDirXZ);

            Vector3? lastBeforeSlopeEdge = null;
            Vector3? firstOnSlopeEdge = null;
            Vector3? lastOnSlopeEdge = null;

            // slope is not yet started; landing can be placed on the flat before it or on the border of the slope start
            if (Mathf.Approximately(RemainingLength, Length))
            {
                Vector3 lastBeforeSlopePosition = Start - landing.GetLandingAreaLengthXZ() * rideDirXZ;
                lastBeforeSlopePosition.y = StartHeight; // set the height to the slope's end height
                lastBeforeSlopeEdge = lastBeforeSlopePosition + landing.GetHeight() * Vector3.up;

                // a landing placed on the border of the slope start has its position heightened because the slope starts earlier to support the landing from its start point
                if (landing.GetLength() < RemainingLength)
                {
                    Vector3 firstOnSlopePosition =
                        Start + 0.01f * flightDirectionXZ - landing.GetLandingAreaLengthXZ() * rideDirXZ;
                    firstOnSlopePosition.y = EndPoint.y -
                                             GetHeightDifferenceForXZDistance(Vector3.Distance(Start,
                                                 firstOnSlopePosition));
                    firstOnSlopeEdge = firstOnSlopePosition + landing.GetHeight() * slopeNormal;
                }
            }


            // slope's remaining length is longer than the landing length; the whole landing can be placed on the slope
            if (landing.GetLength() < RemainingLength)
            {
                if (!Mathf.Approximately(RemainingLength, Length))
                {
                    Vector3 firstOnSlopePosition = EndPoint;
                    firstOnSlopeEdge = firstOnSlopePosition + landing.GetHeight() * slopeNormal;
                }

                Vector3 lastOnSlopePosition = EndPoint +
                                              flightDirectionXZ * (RemainingLength - landing.GetLandingAreaLengthXZ() -
                                                                   0.01f);
                lastOnSlopePosition.y = EndPoint.y +
                                        GetHeightDifferenceForXZDistance(
                                            Vector3.Distance(EndPoint, lastOnSlopePosition));
                lastOnSlopeEdge = lastOnSlopePosition + landing.GetHeight() * slopeNormal;
            }

            // calculate the first position after the slope (landing is on flat ground)
            Vector3 firstAfterSlopePosition =
                EndPoint + flightDirectionXZ * (RemainingLength - landing.GetLandingAreaLengthXZ() - 0.01f);
            firstAfterSlopePosition.y = EndHeight;
            Vector3 firstAfterSlopeEdge = firstAfterSlopePosition + landing.GetHeight() * Vector3.up;

            if (lastOnSlopeEdge.HasValue)
            {
                InternalDebug.DrawLine(lastOnSlopeEdge.Value, lastOnSlopeEdge.Value - slopeNormal * 5f, Color.red, 5f);
                InternalDebug.DrawLine(firstAfterSlopeEdge, firstAfterSlopeEdge - Vector3.up * 5f, Color.green, 5f);
            }

            return new SlopeBoundaryPoints(lastBeforeSlopeEdge, firstOnSlopeEdge, lastOnSlopeEdge, firstAfterSlopeEdge);
        }

        /// <summary>
        /// For a potential landing position in XZ coordinates, calculates that landings position, where its edge will be and what its landing direction will be with respect to the slope.
        /// </summary>        
        public LandingCandidate? GetLandingInfoForDesiredTrajectoryPoint(LandingBase landing,
            Vector3 supposedLandingPoint, SlopeBoundaryPoints boundaryPoints)
        {
            Vector3 rideDirXZ = Vector3.ProjectOnPlane(landing.GetRideDirection(), Vector3.up).normalized;

            Vector3 slopeNormalLandingDir = GetNormal(rideDirXZ);

            bool IsBeforeOrMatchesTrajectoryPoint(Vector3 edgePosition) =>
                Vector3.Dot(supposedLandingPoint - edgePosition, rideDirXZ) >= 0;

            bool IsAfterTrajectoryPoint(Vector3 edgePosition) =>
                Vector3.Dot(supposedLandingPoint - edgePosition, rideDirXZ) < 0;

            (Vector3? lastBeforeSlopeEdge, Vector3? firstOnSlopeEdge, Vector3? lastOnSlopeEdge,
                Vector3 firstAfterSlopeEdge) = boundaryPoints;

            Vector3 landingPosition;
            Vector3 edgePosition;
            bool isWaypoint = false;
            bool isTilted;

            // the landings ideal edge position is somewhere before the slope start
            if (lastBeforeSlopeEdge.HasValue && IsAfterTrajectoryPoint(lastBeforeSlopeEdge.Value))
            {
                landingPosition = supposedLandingPoint;
                landingPosition.y = StartHeight;
                edgePosition = landingPosition + landing.GetHeight() * Vector3.up;
                isTilted = false;

            }
            // the landings ideal edge position is between the last before slope start position and first after start position
            else if (lastBeforeSlopeEdge.HasValue && IsBeforeOrMatchesTrajectoryPoint(lastBeforeSlopeEdge.Value) &&
                     firstOnSlopeEdge.HasValue && IsAfterTrajectoryPoint(firstOnSlopeEdge.Value))
            {
                landingPosition = lastBeforeSlopeEdge.Value - landing.GetHeight() * Vector3.up;
                edgePosition = lastBeforeSlopeEdge.Value;
                isTilted = false;
            }
            // the landings ideal edge position is between the first on slope position and last on slope position
            else if (firstOnSlopeEdge.HasValue && IsBeforeOrMatchesTrajectoryPoint(firstOnSlopeEdge.Value) &&
                     lastOnSlopeEdge.HasValue && IsAfterTrajectoryPoint(lastOnSlopeEdge.Value))
            {
                Vector3 slopeDirXZ = Vector3.ProjectOnPlane(supposedLandingPoint - EndPoint, Vector3.up).normalized;
                Vector3 slopeDir = TiltVectorBySlopeAngle(slopeDirXZ);

                Vector3 normalWithSlopeIntersection =
                    EndPoint + Vector3.Project(supposedLandingPoint - EndPoint, slopeDir);
                float trajectoryToIntersectionDistance =
                    Vector3.Distance(supposedLandingPoint, normalWithSlopeIntersection);

                float landingHeight = landing.GetHeight();

                float shiftToLandingPosition = Mathf.Tan(-Angle) * (trajectoryToIntersectionDistance - landingHeight);

                landingPosition = normalWithSlopeIntersection +
                                  shiftToLandingPosition * TiltVectorBySlopeAngle(rideDirXZ);

                edgePosition = landingPosition + landing.GetHeight() * slopeNormalLandingDir;

                isWaypoint = true;
                isTilted = true;
            }
            // the landings ideal edge position is between the last on slope position and first after slope position
            else if (lastOnSlopeEdge.HasValue && IsBeforeOrMatchesTrajectoryPoint(lastOnSlopeEdge.Value) &&
                     IsAfterTrajectoryPoint(firstAfterSlopeEdge))
            {
                landingPosition = lastOnSlopeEdge.Value - landing.GetHeight() * slopeNormalLandingDir;
                edgePosition = lastOnSlopeEdge.Value;
                isWaypoint = true;
                isTilted = true;
            }
            // the landings ideal edge position is after the slope end            
            else if (IsBeforeOrMatchesTrajectoryPoint(firstAfterSlopeEdge))
            {
                landingPosition = supposedLandingPoint;
                landingPosition.y = EndHeight;
                edgePosition = landingPosition + landing.GetHeight() * Vector3.up;
                isWaypoint = true;
                isTilted = false;
            }
            else
            {
                InternalDebug.Log(
                    $"No valid landing position found for supposed landing point {supposedLandingPoint}.");
                return null;
            }


            return new(landingPosition, edgePosition, isWaypoint, isTilted);
        }

        /// <summary>
        /// Calculates position and height changes for placing a takeoff obstacle and previews the adjusted slope geometry.
        /// </summary>
        public void PlaceTakeoff(TakeoffBuilder takeoff)
        {
            LastConfirmedSnapshot.Revert();

            Vector3 waypointStartXZ = takeoff.GetStartPoint();
            waypointStartXZ.y = 0; // ignore the takeoff's height for the XZ calculations
            Vector3 waypointEndXZ = takeoff.GetEndPoint();
            waypointEndXZ.y = 0; // ignore the takeoff's height for the XZ calculations

            LastRideDirection = Vector3.ProjectOnPlane(takeoff.GetRideDirection(), Vector3.up);

            // check if entire takeoff is before the slope
            if (IsBeforeStart(waypointStartXZ) && IsBeforeStart(waypointEndXZ))
            {
                TerrainManager.Instance.FitObstacleOnFlat(takeoff);
                UpdateHighlight();
                return;
            }

            float newRemainingLength = RemainingLength;
            Vector3 newEndPoint;
            TerrainManager.HeightmapCoordinates coords = new();

            Width = Mathf.Max(Width, takeoff.GetBottomWidth());

            float startHeight = EndPoint.y;


            // obstacle is on the border of slope start
            if (IsBeforeStart(waypointStartXZ) && IsOnActivePartOfSlope(waypointEndXZ))
            {
                float heightDiff = GetHeightDifferenceForXZDistance(Vector3.Distance(waypointStartXZ, waypointEndXZ));
                var rampCoords = TerrainManager.Instance.DrawRamp(waypointStartXZ, waypointEndXZ,
                    heightDiff, Width, startHeight);
                coords.Add(rampCoords);

                var flatCoords = TerrainManager.Instance.DrawFlat(waypointStartXZ, waypointEndXZ,
                    startHeight, Width);
                coords.Add(flatCoords);

                TerrainManager.Instance.FitObstacleOnFlat(takeoff);
                float distanceTaken = Vector3.Distance(EndPoint, takeoff.GetEndPoint());
                newRemainingLength -= distanceTaken;

                newEndPoint = takeoff.GetEndPoint();
                float newEndPointHeight = EndPoint.y + GetHeightDifferenceForXZDistance(distanceTaken);
                newEndPoint.y = newEndPointHeight;

            }
            // whole obstacle is on slope
            else if (IsOnActivePartOfSlope(waypointStartXZ) && IsOnActivePartOfSlope(waypointEndXZ))
            {
                float heightDiff = GetHeightDifferenceForXZDistance(Vector3.Distance(EndPoint, waypointEndXZ));

                var rampCoords = TerrainManager.Instance.DrawRamp(EndPoint, waypointEndXZ, heightDiff,
                    Width, startHeight);
                coords.Add(rampCoords);
                FitObstacleOnSlope(takeoff);
                newRemainingLength -= GetXZDistanceFromSlopeLength(Vector3.Distance(EndPoint, takeoff.GetEndPoint()));
                newEndPoint = takeoff.GetEndPoint();
            }
            // obstacle is on border of slope end
            else if (IsOnActivePartOfSlope(waypointStartXZ) && IsAfterSlope(waypointEndXZ))
            {
                float heightDiff = GetHeightDifferenceForXZDistance(Vector3.Distance(EndPoint, waypointEndXZ));
                var rampCoords = TerrainManager.Instance.DrawRamp(EndPoint, waypointEndXZ, heightDiff,
                    Width, startHeight);
                coords.Add(rampCoords);

                FitObstacleOnSlope(takeoff);
                newEndPoint = GetFinishedEndPoint();

                newRemainingLength = 0;
            }
            // whole obstacle is after the slope
            else if (IsAfterSlope(waypointStartXZ) && IsAfterSlope(waypointEndXZ))
            {
                newEndPoint = GetFinishedEndPoint();
                float heightDiff = GetHeightDifferenceForXZDistance(Vector3.Distance(EndPoint, newEndPoint));
                var rampCoords = TerrainManager.Instance.DrawRamp(EndPoint, newEndPoint, heightDiff,
                    Width, startHeight);
                coords.Add(rampCoords);

                var flatCoords = TerrainManager.Instance.DrawFlat(newEndPoint, waypointEndXZ,
                    EndHeight, Width);
                coords.Add(flatCoords);

                TerrainManager.Instance.FitObstacleOnFlat(takeoff);
                newRemainingLength = 0;
            }
            // slope is so short that the obstacle starts before it but ends after it.
            else
            {
                newEndPoint = GetFinishedEndPoint();
                float heightDiff = GetHeightDifferenceForXZDistance(Vector3.Distance(waypointStartXZ, newEndPoint));
                var rampCoords = TerrainManager.Instance.DrawRamp(waypointStartXZ, newEndPoint, heightDiff,
                    Width, startHeight);
                coords.Add(rampCoords);

                FitObstacleOnSlope(takeoff);
                newRemainingLength = 0;
            }

            UpdateHighlight(newRemainingLength, takeoff.GetEndPoint(), takeoff.GetRideDirection());

            LastPlacementResult = new PlacementResult(newRemainingLength, newEndPoint, true, coords);
        }

        /// <summary>
        /// Calculates position and height changes for placing a landing obstacle and previews the adjusted slope geometry.
        /// </summary>
        public void PlaceLanding(Vector3 landingPosition, bool isTilted, LandingBuilder landing)
        {
            LastConfirmedSnapshot.Revert();

            Vector3 rideDirXZ = Vector3.ProjectOnPlane(landing.GetTransform().forward, Vector3.up);

            if (isTilted)
            {
                Vector3 angledRideDir = TiltVectorBySlopeAngle(rideDirXZ);
                landing.GetTransform().forward = angledRideDir;
            }
            else
            {
                landing.GetTransform().forward = rideDirXZ;
            }

            landing.SetPosition(landingPosition);

            float newRemainingLength = RemainingLength;
            Vector3 newEndPoint;
            TerrainManager.HeightmapCoordinates coords = new();

            Vector3 landingStartXZ = landing.GetStartPoint();
            landingStartXZ.y = EndPoint.y;

            float distanceToStartXZ = Vector3.Distance(EndPoint, landingStartXZ);

            Width = Mathf.Max(Width, landing.GetBottomWidth() + 1f);

            if (isTilted)
            {
                Vector3 landingPositionXZ = landing.GetTransform().position;
                landingPositionXZ.y = EndPoint.y;
                float startHeight = landing.GetStartPoint().y + 0.05f;

                float heightDiff = GetHeightDifferenceForXZDistance(Vector3.Distance(landing.GetStartPoint(),
                    landing.GetEndPoint()));
                var rampCoords = TerrainManager.Instance.DrawRamp(landing.GetStartPoint(),
                    landing.GetEndPoint(), heightDiff, Width, startHeight);
                coords.Add(rampCoords);

                newEndPoint = landing.GetEndPoint();
                newRemainingLength -= Vector3.Distance(EndPoint, landingPositionXZ) + landing.GetLandingAreaLengthXZ();
            }
            else
            {
                var flatCoords = TerrainManager.Instance.DrawFlat(landing.GetStartPoint(),
                    landing.GetEndPoint(), landingPosition.y, Width);
                coords.Add(flatCoords);

                newRemainingLength = 0;
                newEndPoint = EndPoint + (landing.GetStartPoint() - EndPoint).normalized * distanceToStartXZ +
                              landing.GetRideDirection() * (newRemainingLength - distanceToStartXZ);
                newEndPoint.y = EndHeight;
            }

            UpdateHighlight(newRemainingLength, landing.GetEndPoint(), landing.GetRideDirection());

            LastRideDirection = Vector3.ProjectOnPlane(rideDirXZ, Vector3.up).normalized;

            LastPlacementResult = new(newRemainingLength, newEndPoint, true, coords);
        }

        /// <summary>
        /// Applies the last calculated placement result, confirming the obstacle on the slope and establishing new bounds.
        /// </summary>
        public void ConfirmChanges<T>(ObstacleBase<T> element) where T : MeshGeneratorBase
        {

            if (LastPlacementResult.IsWaypoint && element.TryGetComponent<ILineElement>(out var lineElement))
            {
                Waypoints.AddWaypoint(lineElement, LastPlacementResult.ChangedHeightmapCoords);

                // if the element is actually built on top of the slope and not after its end, mark it as the current
                // last on slope element
                if (Vector3.Angle(Vector3.ProjectOnPlane(element.GetRideDirection(), Vector3.up),
                        element.GetRideDirection()) > float.Epsilon)
                {
                    LastElementOnSlope = lineElement;
                }

            }

            RemainingLength = LastPlacementResult.RemainingLength;
            EndPoint = LastPlacementResult.NewEndPoint;

            if (RemainingLength <= 0)
            {
                TerrainManager.Instance.ActiveSlope = null;
            }

            TerrainManager.Instance.SetHeight(EndPoint.y);
            LastPlacementResult = new(this); // reset last change

            TerrainManager.ConfirmChanges();

            LastConfirmedSnapshot = GetSlopeSnapshot();

            UpdateHighlight();
        }

        /// <summary>
        /// Removes a placed waypoint from the slope and reconstructs the slope state to what it was before placement.
        /// </summary>
        public void RemoveWaypoint(ILineElement element)
        {
            Waypoints.RemoveWaypoint(element);

            if (Waypoints.Count == 0)
            {
                StudioUIManager.Instance.GetSidebar().DeleteSlopeButtonEnabled = true;
                LastElementOnSlope = null;
            }
            else
            {
                LastElementOnSlope = Waypoints[^1].element;
            }
        }

        public void Delete()
        {
            if (Waypoints.Count > 0)
            {
                InternalDebug.LogError("Deleting a slope change with waypoints. This should not happen.");
                Waypoints.Clear();
            }

            TerrainManager.Instance.RemoveSlope(this);

            FlatToStartPoint.MarkAs(new FreeCoordinateState());

            Destroy(gameObject);
        }

        /// <summary>
        /// Draws gizmos in the editor representing slope start, current end, and predicted finished end point.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Start, 0.5f);
            Gizmos.DrawSphere(EndPoint, 0.5f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(GetFinishedEndPoint(), 0.5f);
        }

        public SlopeData GetSerializableData() => new SlopeData(this);

        public void LoadFromData(SlopeData data)
        {
            Start = data.start;
            EndPoint = data.end;
            RemainingLength = data.remainingLength;
            StartHeight = data.startHeight;
            EndHeight = data.endHeight;
            Angle = GetSlopeAngle(data.length, HeightDifference);
            Width = data.width;
            LastRideDirection = data.lastRideDirection;
            Length = data.length;

            PreviousLineElement = Line.Instance[data.previousLineElementIndex];

            Waypoints = new WaypointList(this);
            Waypoints.LoadFromData(data.waypoints);

            LastPlacementResult = data.lastPlacementResult.ToPlacementResult();

            if (IsBuiltOn)
            {
                FlatToStartPoint.MarkAs(new HeightSetCoordinateState());
            }

            UpdateHighlight();
        }
    }
}


