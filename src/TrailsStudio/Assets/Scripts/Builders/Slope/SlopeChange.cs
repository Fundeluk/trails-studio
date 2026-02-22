using Assets.Scripts.Managers;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders
{
    public readonly struct SlopeBoundaryPoints
    {
        public readonly Vector3? lastBeforeSlopeEdge;
        public readonly Vector3? firstOnSlopeEdge;
        public readonly Vector3? lastOnSlopeEdge;
        public readonly Vector3 firstAfterSlopeEdge;
        public SlopeBoundaryPoints(Vector3? lastBeforeSlopeEdge, Vector3? firstOnSlopeEdge, Vector3? lastOnSlopeEdge, Vector3 firstAfterSlopeEdge)
        {
            this.lastBeforeSlopeEdge = lastBeforeSlopeEdge;
            this.firstOnSlopeEdge = firstOnSlopeEdge;
            this.lastOnSlopeEdge = lastOnSlopeEdge;
            this.firstAfterSlopeEdge = firstAfterSlopeEdge;
        }

        internal void Deconstruct(out Vector3? lastBeforeSlopeEdge, out Vector3? firstOnSlopeEdge, out Vector3? lastOnSlopeEdge, out Vector3 firstAfterSlopeEdge)
        {
            lastBeforeSlopeEdge = this.lastBeforeSlopeEdge;
             firstOnSlopeEdge = this.firstOnSlopeEdge;
            lastOnSlopeEdge = this.lastOnSlopeEdge;
            firstAfterSlopeEdge = this.firstAfterSlopeEdge;
        }
    }

    public class SlopeChange : SlopeChangeBase, ISaveable<SlopeData>
    {
        public record SlopeSnapshot
        {
            public readonly SlopeChange slope;
            public bool finished;
            public float remainingLength;
            public float width;
            public Vector3 endPoint;
            public Vector3 lastRideDir;

            public SlopeSnapshot(SlopeChange slope)
            {
                this.slope = slope;
                finished = slope.Finished;
                remainingLength = slope.RemainingLength;
                width = slope.Width;
                endPoint = slope.EndPoint;
                lastRideDir = slope.LastRideDirection;
            }

            public SlopeSnapshot(SlopeChange slope, bool finished, float remainingLength, float width, Vector3 endPoint, Vector3 lastRideDir)
            {
                this.slope = slope;
                this.finished = finished;
                this.remainingLength = remainingLength;
                this.width = width;
                this.endPoint = endPoint;
                this.lastRideDir = lastRideDir;
            }

            public void Revert()
            {
                if (!finished)
                {
                    TerrainManager.Instance.ActiveSlope = slope;
                }
                
                slope.LastPlacementResult.ChangedHeightmapCoords?.SetHeight(endPoint.y);

                slope.RemainingLength = remainingLength;
                slope.EndPoint = endPoint;
                slope.Width = width;
                slope.LastRideDirection = lastRideDir;


                slope.UpdateHighlight();
            }
        }

        /// <summary>
        /// Represents the result of placing a line element (takeoff or landing) on or near a slope.
        /// This record captures the state changes that occur when an obstacle is positioned, including
        /// the updated slope geometry and affected terrain heightmap coordinates.
        /// </summary>
        /// <remarks>
        /// <para>
        /// PlacementResult is used to track both tentative and confirmed placements of obstacles on slopes.
        /// It serves as an intermediate state holder that can be used to preview changes before they are
        /// committed to the slope's state.
        /// </para>
        /// <para>
        /// The placement result is stored in <see cref="SlopeChange.LastPlacementResult"/> and is later
        /// confirmed via <see cref="SlopeChange.ConfirmChanges{T}(ObstacleBase{T})"/> which applies the
        /// changes permanently to the slope.
        /// </para>
        /// </remarks>
        public record PlacementResult
        {
            public float Remaininglength { get; private set; } = 0f;
            public Vector3 NewEndPoint { get; private set; } = Vector3.zero;
            public bool IsWaypoint { get; private set; } = false;

            public HeightmapCoordinates ChangedHeightmapCoords { get; private set; } = null;

            public PlacementResult(float remainingLength, Vector3 newEndPoint, bool isWaypoint, HeightmapCoordinates changedHeightmapCoords)
            {
                Remaininglength = remainingLength;
                NewEndPoint = newEndPoint;
                IsWaypoint = isWaypoint;
                ChangedHeightmapCoords = changedHeightmapCoords;
            }     
            
            public PlacementResult(SlopeChange slopeChange)
            {
                Remaininglength = slopeChange.RemainingLength;
                NewEndPoint = slopeChange.EndPoint;                
            }
        }

        public class WaypointList : IEnumerable<(ILineElement, SlopeSnapshot)>, ISaveable<WaypointListData>
        {            

            public readonly SlopeChange owner;
            public List<(ILineElement element, SlopeSnapshot snapshot)> waypoints = new();

            public void AddWaypoint(ILineElement waypoint)
            {
                // only when the first waypoint is added, mark the flat to start point as occupied to avoid placement issues
                // with the first waypoint (the flat would be marked as occupied and the waypoint couldn't be placed there)
                if (waypoints.Count == 0)
                {
                    owner.FlatToStartPoint.MarkAs(new HeightSetCoordinateState());
                }

                StudioUIManager.Instance.GetSidebar().DeleteButtonEnabled = true;
                StudioUIManager.Instance.GetSidebar().DeleteSlopeButtonEnabled = false;


                SlopeSnapshot snapshot = owner.LastConfirmedSnapshot;
                waypoints.Add((waypoint, snapshot));
                waypoint.SetSlopeChange(owner);
            }

            public (ILineElement element, SlopeSnapshot snapshot) this[int index] => waypoints[index];

            public bool TryFindByElement(ILineElement element, out SlopeSnapshot snapshot)
            {
                foreach (var waypoint in waypoints)
                {
                    if (waypoint.element == element)
                    {
                        snapshot = waypoint.snapshot;
                        return true;
                    }
                }
                snapshot = default;
                return false;
            }

            public bool RemoveWaypoint(ILineElement item)
            {
                if (TryFindByElement(item, out var snapshot))
                {
                    item.SetSlopeChange(null);
                    snapshot.Revert(); // revert the slope to the state before the waypoint was added
                    item.GetUnderlyingSlopeHeightmapCoordinates()?.MarkAs(new FreeCoordinateState()); // unmark the heightmap coordinates of the waypoint

                    TerrainManager.Instance.SetHeight(snapshot.endPoint.y); // set the height of the terrain to the height at the waypoint

                    owner.LastConfirmedSnapshot = snapshot;

                    return waypoints.Remove((item, snapshot));
                }
                else
                {
                    return false;
                }

            } 
            
            public void Clear()
            {
                foreach ((ILineElement element, var _) in waypoints)
                {
                    element.SetSlopeChange(null);
                    element.GetUnderlyingSlopeHeightmapCoordinates()?.MarkAs(new FreeCoordinateState());
                }
                waypoints.Clear();
            }

            public int Count => waypoints.Count;          

            
            public IEnumerator<(ILineElement, SlopeSnapshot)> GetEnumerator()
            {
                return waypoints.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)waypoints).GetEnumerator();
            }

            public WaypointListData GetSerializableData() => new(this);

            public void LoadFromData(WaypointListData data)
            {
                Clear();
                for (int i = 0; i < data.snapshots.Count; i++)
                {
                    var snapshot = data.snapshots[i];
                    var element = Line.Instance[data.waypointIndices[i]];

                    if (element == null)
                    {
                        Debug.LogWarning($"Waypoint with index {data.waypointIndices[i]} not found in the line. Skipping loading.");
                        continue;
                    }

                    element.SetSlopeChange(owner);

                    waypoints.Add((element, snapshot.ToSlopeSnapshot()));
                }
                
            }

            public WaypointList(SlopeChange owner)
            {
                this.owner = owner;
            }
        }

        [SerializeField]
        GameObject endPointHighlightPrefab;

        readonly List<GameObject> endPointHighlights = new();

        GameObject infoText;               

        /// <summary>
        /// The portion of the terrain that goes from the last line element before the slope's start to the slope's start.
        /// </summary>
        public HeightmapCoordinates FlatToStartPoint => new HeightmapCoordinates(PreviousLineElement.GetEndPoint(), Start, PreviousLineElement.GetBottomWidth());

        public ILineElement LastElementOnSlope { get; private set; }

        public float RemainingLength { get; private set; }       

        public WaypointList Waypoints { get; private set; }

        /// <summary>
        /// The end point of the slope. If the slope is not finished, this is an end point of the realized portion of the slope.
        /// </summary>
        public Vector3 EndPoint { get; private set; }

        public Vector3 LastRideDirection { get; private set; }

        public SlopeSnapshot LastConfirmedSnapshot { get; private set; }

        public override bool Finished => RemainingLength <= 0;

        public bool IsBuiltOn => Waypoints.Count > 0;

        protected override void UpdateHighlight()
        {
            if (Finished || Length == 0 || RemainingLength == 0)
            {
                highlight.enabled = false;
                return;
            }

            highlight.enabled = true;

            Vector3 rideDirNormal = Vector3.Cross(LastRideDirection, Vector3.up).normalized;

            Vector3 position = Vector3.Lerp(EndPoint, EndPoint + RemainingLength * LastRideDirection, 0.5f);
            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, rideDirNormal);

            transform.SetPositionAndRotation(position, rotation);

            DecalProjector decalProjector = highlight.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(RemainingLength, Width, 20);
        }

        protected void UpdateHighlight(float remainingLength, Vector3 highlightStart, Vector3 direction)
        {
            if (Finished || Length == 0 || remainingLength == 0)
            {
                highlight.enabled = false;
                return;
            }

            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;

            highlight.enabled = true;

            Vector3 rideDirNormal = Vector3.Cross(direction, Vector3.up).normalized;

            Vector3 position = Vector3.Lerp(highlightStart, highlightStart + remainingLength * direction, 0.5f);

            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, rideDirNormal);

            transform.SetPositionAndRotation(position, rotation);

            DecalProjector decalProjector = highlight.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(remainingLength, Width, 20);
        }

        /// <summary>
        /// Initializes a slope with a given Start point, Length and end height.
        /// </summary>
        public void Initialize(Vector3 start, float endHeight, float length)
        {
            Waypoints = new WaypointList(this);
            startHeight = start.y;
            this.endHeight = endHeight;

            Start = start;
            EndPoint = start; 
            
            Length = length;
            RemainingLength = length;

            UpdateAngle();

            PreviousLineElement = Line.Instance.GetLastLineElement();
            LastRideDirection = PreviousLineElement.GetRideDirection();
            Width = PreviousLineElement.GetBottomWidth();

            highlight = GetComponent<DecalProjector>();
            highlight.material.color = Color.green;

            UpdateHighlight();

            LastPlacementResult = new(this);
            LastConfirmedSnapshot = GetSlopeSnapshot();
        }

        public List<(string name, string value)> GetInfoText()
        {
            List<(string name, string value)> info = new()
            {
                ("Length", $"{Length:0.##}m"),
                ("Angle", $"{Angle * Mathf.Rad2Deg:0}°"),
                ("Height difference", $"{endHeight - startHeight:0.##}m"),
            };
            return info;
        }

        public void ShowInfo()
        {
            // offset the info text to the side of the slope
            Vector3 infoTextPos = Start + Vector3.Cross(Camera.main.transform.forward, Vector3.up).normalized * 5f + Vector3.up * 4f;
            infoText = StudioUIManager.Instance.ShowSlopeInfo(GetInfoText(), infoTextPos, transform, Start);

            endPointHighlights.Add(Instantiate(endPointHighlightPrefab, Start, Quaternion.identity));
            endPointHighlights[0].transform.parent = transform;
            endPointHighlights.Add(Instantiate(endPointHighlightPrefab, EndPoint, Quaternion.identity));
            endPointHighlights[1].transform.parent = transform;            
        }

        public void HideInfo()
        {
            foreach (var highlight in  endPointHighlights)
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
            else
            {
                Vector3 result = EndPoint + LastRideDirection * RemainingLength;
                result.y = endHeight;
                return result;
            }
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


        /// <param name="XZDistance">The distance on the XZ plane.</param>
        /// <rerns>The slope Length that corresponds to the given XZ distance.</returns>
        public float GetSlopeLengthFromXZDistance(float XZDistance)
        {           
            return XZDistance / Mathf.Cos(Mathf.Abs(Angle));
        }
        
        /// <param name="slopeLength">The distance on the slope</param>
        /// <returns>The XZ plane distance that corresponds to the given slope distance</returns>
        public float GetXZDistanceFromSlopeLength(float slopeLength)
        {
            return slopeLength * Mathf.Cos(Mathf.Abs(Angle));
        }

        public Vector3 GetNormal(Vector3 rideDir)
        {
            Quaternion tiltToAngle = Quaternion.AngleAxis(Angle * Mathf.Rad2Deg, -Vector3.Cross(Vector3.up, rideDir).normalized);
            Vector3 normal = tiltToAngle * Vector3.up;
            return normal.normalized;
        }

        public Vector3 TiltVectorBySlopeAngle(Vector3 vector)
        {
            Vector3 flatDirection = Vector3.ProjectOnPlane(vector, Vector3.up).normalized;
            Quaternion tiltToAngle = Quaternion.AngleAxis(Angle * Mathf.Rad2Deg, -Vector3.Cross(Vector3.up, flatDirection).normalized);
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
            float newHeight = TerrainManager.GetHeightAt(builder.GetTransform().position);
            builder.GetTransform().position = new(builder.GetTransform().position.x, newHeight, builder.GetTransform().position.z);
        }

        public SlopeBoundaryPoints GetBoundaryPoints(LandingBase landing, Vector3 flightDirectionXZ)
        {
            Vector3 rideDirXZ = Vector3.ProjectOnPlane(landing.GetRideDirection(), Vector3.up).normalized;
            Vector3 slopeNormal = GetNormal(rideDirXZ);

            Vector3? lastBeforeSlopeEdge = null;
            Vector3? firstOnSlopeEdge = null;
            Vector3? lastOnSlopeEdge = null;

            // slope is not yet started; landing can be placed on the flat before it or on the border of the slope start
            if (RemainingLength == Length)
            {
                Vector3 lastBeforeSlopePosition = Start - landing.GetLandingAreaLengthXZ() * rideDirXZ;
                lastBeforeSlopePosition.y = startHeight; // set the height to the slope's end height
                lastBeforeSlopeEdge = lastBeforeSlopePosition + landing.GetHeight() * Vector3.up;

                // a landing placed on the border of the slope start has its position heightened because the slope starts earlier to support the landing from its start point
                if (landing.GetLength() < RemainingLength)
                {
                    Vector3 firstOnSlopePosition = Start + 0.01f * flightDirectionXZ - landing.GetLandingAreaLengthXZ() * rideDirXZ;
                    firstOnSlopePosition.y = EndPoint.y - GetHeightDifferenceForXZDistance(Vector3.Distance(Start, firstOnSlopePosition));
                    firstOnSlopeEdge = firstOnSlopePosition + landing.GetHeight() * slopeNormal;
                }
            }


            // slope's remaining length is longer than the landing length; the whole landing can be placed on the slope
            if (landing.GetLength() < RemainingLength)
            {
                if (RemainingLength != Length)
                {
                    Vector3 firstOnSlopePosition = EndPoint;
                    firstOnSlopeEdge = firstOnSlopePosition + landing.GetHeight() * slopeNormal;
                }

                Vector3 lastOnSlopePosition = EndPoint + flightDirectionXZ * (RemainingLength - landing.GetLandingAreaLengthXZ() - 0.01f);
                lastOnSlopePosition.y = EndPoint.y + GetHeightDifferenceForXZDistance(Vector3.Distance(EndPoint, lastOnSlopePosition));
                lastOnSlopeEdge = lastOnSlopePosition + landing.GetHeight() * slopeNormal;
            }       
            
            // calculate the first position after the slope (landing is on flat ground)
            Vector3 firstAfterSlopePosition = EndPoint + flightDirectionXZ * (RemainingLength - landing.GetLandingAreaLengthXZ() - 0.01f);
            firstAfterSlopePosition.y = endHeight;
            Vector3 firstAfterSlopeEdge = firstAfterSlopePosition + landing.GetHeight() * Vector3.up;

            if (lastOnSlopeEdge.HasValue)
            {
                Debug.DrawLine(lastOnSlopeEdge.Value, lastOnSlopeEdge.Value - slopeNormal * 5f, Color.red, 5f);
                Debug.DrawLine(firstAfterSlopeEdge, firstAfterSlopeEdge - Vector3.up * 5f, Color.green, 5f);
            }

            return new(lastBeforeSlopeEdge, firstOnSlopeEdge, lastOnSlopeEdge, firstAfterSlopeEdge);
        }

        public readonly struct MatchingLandingPosition
        {
            public readonly Vector3 position;
            public readonly Vector3 edgePosition;
            public readonly bool isWaypoint;
            public readonly bool isTilted;

            public MatchingLandingPosition(Vector3 position, Vector3 edgePosition, bool isWaypoint, bool isTilted)
            {
                this.position = position;
                this.edgePosition = edgePosition;
                this.isWaypoint = isWaypoint;
                this.isTilted = isTilted;
            }

            internal void Deconstruct(out Vector3 landingPosition, out Vector3 edgePosition)
            {
                landingPosition = position;
                edgePosition = this.edgePosition;
            }

            internal void Deconstruct(out Vector3 landingPosition, out Vector3 edgePosition, out bool isWaypoint, out bool isTilted)
            {
                landingPosition = position;
                edgePosition = this.edgePosition;
                isWaypoint = this.isWaypoint;
                isTilted = this.isTilted;
            }
        }


        /// <summary>
        /// For a potential landing position in XZ coordinates, calculates that landings position, where its edge will be and what its landing direction will be with respect to the slope.
        /// </summary>        
        public MatchingLandingPosition? GetLandingInfoForDesiredTrajectoryPoint(LandingBase landing, Vector3 supposedLandingPoint, SlopeBoundaryPoints boundaryPoints)
        {
            Vector3 rideDirXZ = Vector3.ProjectOnPlane(landing.GetRideDirection(), Vector3.up).normalized;

            Vector3 slopeNormalLandingDir = GetNormal(rideDirXZ);

            bool IsBeforeOrMatchesTrajectoryPoint(Vector3 edgePosition) => Vector3.Dot(supposedLandingPoint - edgePosition, rideDirXZ) >= 0;

            bool IsAfterTrajectoryPoint(Vector3 edgePosition) => Vector3.Dot(supposedLandingPoint - edgePosition, rideDirXZ) < 0;

            (Vector3? lastBeforeSlopeEdge, Vector3? firstOnSlopeEdge, Vector3? lastOnSlopeEdge, Vector3 firstAfterSlopeEdge) = boundaryPoints;

            Vector3 landingPosition;
            Vector3 edgePosition;
            bool isWaypoint = false;
            bool isTilted;

            // the landings ideal edge position is somewhere before the slope start
            if (lastBeforeSlopeEdge.HasValue &&  IsAfterTrajectoryPoint(lastBeforeSlopeEdge.Value))
            {
                landingPosition = supposedLandingPoint;
                landingPosition.y = startHeight;
                edgePosition = landingPosition + landing.GetHeight() * Vector3.up;
                isTilted = false;

            }
            // the landings ideal edge position is between the last before slope start position and first after start position
            else if (lastBeforeSlopeEdge.HasValue && IsBeforeOrMatchesTrajectoryPoint(lastBeforeSlopeEdge.Value) && firstOnSlopeEdge.HasValue && IsAfterTrajectoryPoint(firstOnSlopeEdge.Value))
            {
                landingPosition = lastBeforeSlopeEdge.Value - landing.GetHeight() * Vector3.up;
                edgePosition = lastBeforeSlopeEdge.Value;
                isTilted = false;
            }
            // the landings ideal edge position is between the first on slope position and last on slope position
            else if (firstOnSlopeEdge.HasValue && IsBeforeOrMatchesTrajectoryPoint(firstOnSlopeEdge.Value) && lastOnSlopeEdge.HasValue && IsAfterTrajectoryPoint(lastOnSlopeEdge.Value))
            {
                Vector3 slopeDirXZ = Vector3.ProjectOnPlane(supposedLandingPoint - EndPoint, Vector3.up).normalized;
                Vector3 slopeDir = TiltVectorBySlopeAngle(slopeDirXZ);
               
                Vector3 normalWithSlopeIntersection = MathHelper.GetNearestPointOnLine(EndPoint, slopeDir, supposedLandingPoint);

                float trajectoryToIntersectionDistance = Vector3.Distance(supposedLandingPoint, normalWithSlopeIntersection);

                float landingHeight = landing.GetHeight();

                float shiftToLandingPosition = Mathf.Tan(-Angle) * (trajectoryToIntersectionDistance - landingHeight);

                landingPosition = normalWithSlopeIntersection + shiftToLandingPosition * TiltVectorBySlopeAngle(rideDirXZ);

                edgePosition = landingPosition + landing.GetHeight() * slopeNormalLandingDir;

                isWaypoint = true;
                isTilted = true;
                Debug.DrawLine(landingPosition, edgePosition, Color.blue, 5f);
            }
            // the landings ideal edge position is between the last on slope position and first after slope position
            else if (lastOnSlopeEdge.HasValue && IsBeforeOrMatchesTrajectoryPoint(lastOnSlopeEdge.Value) && IsAfterTrajectoryPoint(firstAfterSlopeEdge))
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
                landingPosition.y = endHeight;
                edgePosition = landingPosition + landing.GetHeight() * Vector3.up;     
                isWaypoint = true;
                isTilted = false;
            }
            else
            {
                return null;
            }


            return new(landingPosition, edgePosition, isWaypoint, isTilted);
        }


        private HeightmapCoordinates DrawFlat(Vector3 start, Vector3 end, float height)
        {
            start.y = 0;
            end.y = 0;

            float distanceToModify = Vector3.Distance(start, end);

            if (distanceToModify == 0)
            {
                return new HeightmapCoordinates();
            }

            Vector3 rideDir = Vector3.ProjectOnPlane(end - start, Vector3.up).normalized;

            Vector3 rideDirNormal = Vector3.Cross(rideDir, Vector3.up).normalized;

            Vector3 leftStartCorner = start - 0.5f * Width * rideDirNormal;

            float heightmapSpacing = TerrainManager.GetHeightmapSpacing();
            int widthSteps = Mathf.CeilToInt(Width / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(distanceToModify / heightmapSpacing);

            int2 leftSCorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner);
            int2 rightSCorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + widthSteps * rideDirNormal);
            int2 leftECorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + lengthSteps * rideDir);
            int2 rightECorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + lengthSteps * rideDir + widthSteps * rideDirNormal);

            int minX = Mathf.Min(leftSCorner.x, rightSCorner.x, leftECorner.x, rightECorner.x);
            int maxX = Mathf.Max(leftSCorner.x, rightSCorner.x, leftECorner.x, rightECorner.x);
            int minY = Mathf.Min(leftSCorner.y, rightSCorner.y, leftECorner.y, rightECorner.y);
            int maxY = Mathf.Max(leftSCorner.y, rightSCorner.y, leftECorner.y, rightECorner.y);
            int hMapWidth = maxX - minX + 1;
            int hMapHeight = maxY - minY + 1;

            HashSet<int2> coordinates = new();

            float[,] heights = TerrainManager.Floor.terrainData.GetHeights(minX, minY, hMapWidth, hMapHeight);

            height = TerrainManager.WorldUnitsToHeightmapUnits(height); // heightmap units

            if (height < -TerrainManager.maxHeight || height > TerrainManager.maxHeight)
            {
                StudioUIManager.Instance.ShowMessage($"Trying to set height that is out of bounds: {height}m. It must be between {-TerrainManager.maxHeight}m and {TerrainManager.maxHeight}m.", 5f, MessagePriority.High);
                height = Mathf.Clamp(height, -TerrainManager.maxHeight, TerrainManager.maxHeight);
            }

            for (int i = 0; i <= lengthSteps; i++)
            {
                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 position = leftStartCorner + j * heightmapSpacing * rideDirNormal + i * heightmapSpacing * rideDir;

                    int2 heightmapPosition = TerrainManager.WorldToHeightmapCoordinates(position);

                    if (TerrainManager.Instance.GetStateHolder(heightmapPosition) is OccupiedCoordinateState)
                    {
                        // if the heightmap position is occupied, skip it
                        continue;
                    }

                    coordinates.Add(heightmapPosition);

                    int x = heightmapPosition.x - minX;
                    int y = heightmapPosition.y - minY;

                    heights[y, x] = height;
                }
            }

            TerrainManager.Floor.terrainData.SetHeightsDelayLOD(minX, minY, heights);

            var result = new HeightmapCoordinates(coordinates);
            return result;
        }
       
        private HeightmapCoordinates DrawRamp(Vector3 start, Vector3 end, float startHeight)
        {
            start.y = 0;
            end.y = 0;

            float distanceToModify = Vector3.Distance(start, end);

            if (distanceToModify == 0)
            {
                return new HeightmapCoordinates();
            }

            Vector3 rideDir = Vector3.ProjectOnPlane(end - start, Vector3.up).normalized;

            Vector3 rideDirNormal = Vector3.Cross(rideDir, Vector3.up).normalized;

            Vector3 leftStartCorner = start - 0.5f * Width * rideDirNormal;
            
            float heightmapSpacing = TerrainManager.GetHeightmapSpacing();
            int widthSteps = Mathf.CeilToInt(Width / heightmapSpacing);
            int lengthSteps = Mathf.CeilToInt(distanceToModify / heightmapSpacing);

            int2 leftSCorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner);
            int2 rightSCorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + widthSteps * rideDirNormal);
            int2 leftECorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + lengthSteps * rideDir);
            int2 rightECorner = TerrainManager.WorldToHeightmapCoordinates(leftStartCorner + lengthSteps * rideDir + widthSteps * rideDirNormal);

            int minX = Mathf.Min(leftSCorner.x, rightSCorner.x, leftECorner.x, rightECorner.x);
            int maxX = Mathf.Max(leftSCorner.x, rightSCorner.x, leftECorner.x, rightECorner.x);
            int minY = Mathf.Min(leftSCorner.y, rightSCorner.y, leftECorner.y, rightECorner.y);
            int maxY = Mathf.Max(leftSCorner.y, rightSCorner.y, leftECorner.y, rightECorner.y);
            int hMapWidth = maxX - minX + 1;
            int hMapHeight = maxY - minY + 1;

            HashSet<int2> coordinates = new();

            float[,] heights = TerrainManager.Floor.terrainData.GetHeights(minX, minY, hMapWidth, hMapHeight);

            float endHeight = startHeight + GetHeightDifferenceForXZDistance(distanceToModify); // world units

            if (endHeight < -TerrainManager.maxHeight || endHeight > TerrainManager.maxHeight)
            {
                StudioUIManager.Instance.ShowMessage($"Trying to draw a ramp with endHeight that is out of bounds: {endHeight}m. It must be between {-TerrainManager.maxHeight}m and {TerrainManager.maxHeight}m. Clamping it to the closest allowed value..",
                    5f, MessagePriority.High);

                endHeight = Mathf.Clamp(endHeight, -TerrainManager.maxHeight, TerrainManager.maxHeight);
            }

            for (int i = 0; i <= lengthSteps; i++)
            {
                float heightAtLength = startHeight + (endHeight - startHeight) * (i / (float)lengthSteps); // world units                
                heightAtLength = TerrainManager.WorldUnitsToHeightmapUnits(heightAtLength); // heightmap units                

                for (int j = 0; j <= widthSteps; j++)
                {
                    Vector3 position = leftStartCorner + j * heightmapSpacing * rideDirNormal + i * heightmapSpacing * rideDir;

                    int2 heightmapPosition = TerrainManager.WorldToHeightmapCoordinates(position);

                    if (TerrainManager.Instance.GetStateHolder(heightmapPosition) is OccupiedCoordinateState)
                    {
                        // if the heightmap position is occupied, skip it
                        continue;
                    }

                    coordinates.Add(heightmapPosition);

                    int x = heightmapPosition.x - minX;
                    int y = heightmapPosition.y - minY;

                    heights[y, x] = heightAtLength;
                }
            }

            TerrainManager.Floor.terrainData.SetHeightsDelayLOD(minX, minY, heights);

            var result = new HeightmapCoordinates(coordinates);            
            return result;
        }
        
        public SlopeSnapshot GetSlopeSnapshot() => new(this);


        public PlacementResult LastPlacementResult { get; private set; }

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
                TerrainManager.FitObstacleOnFlat(takeoff);
                UpdateHighlight();
                return;
            }

            bool isWaypoint;
            float newRemainingLength = RemainingLength;
            Vector3 newEndPoint;
            HeightmapCoordinates coords = new();
           
            Width = Mathf.Max(Width, takeoff.GetBottomWidth());

            float startHeight = EndPoint.y;


            // obstacle is on the border of slope start
            if (IsBeforeStart(waypointStartXZ) && IsOnActivePartOfSlope(waypointEndXZ))
            {
                coords.Add(DrawFlat(waypointStartXZ, waypointEndXZ, startHeight));
                TerrainManager.FitObstacleOnFlat(takeoff);
                float distanceTaken = Vector3.Distance(EndPoint, takeoff.GetEndPoint());
                newRemainingLength -= distanceTaken;

                newEndPoint = takeoff.GetEndPoint();
                float newEndPointHeight = EndPoint.y + GetHeightDifferenceForXZDistance(distanceTaken);
                newEndPoint.y = newEndPointHeight;

                isWaypoint = true;
            }
            // whole obstacle is on slope
            else if (IsOnActivePartOfSlope(waypointStartXZ) && IsOnActivePartOfSlope(waypointEndXZ))
            {                
                coords.Add(DrawRamp(EndPoint, waypointEndXZ, startHeight));
                FitObstacleOnSlope(takeoff);
                newRemainingLength -= GetXZDistanceFromSlopeLength(Vector3.Distance(EndPoint, takeoff.GetEndPoint()));
                newEndPoint = takeoff.GetEndPoint();
                isWaypoint = true;
            }
            // obstacle is on border of slope end
            else if (IsOnActivePartOfSlope(waypointStartXZ) && IsAfterSlope(waypointEndXZ))
            {
                coords.Add(DrawRamp(EndPoint, waypointEndXZ, startHeight));
                FitObstacleOnSlope(takeoff);
                newEndPoint = GetFinishedEndPoint();

                newRemainingLength = 0;
                isWaypoint = true;
            }
            // whole obstacle is after the slope
            else if (IsAfterSlope(waypointStartXZ) && IsAfterSlope(waypointEndXZ))
            {
                newEndPoint = GetFinishedEndPoint();
                coords.Add(DrawRamp(EndPoint, newEndPoint, startHeight));
                coords.Add(DrawFlat(newEndPoint, waypointEndXZ, endHeight));
                TerrainManager.FitObstacleOnFlat(takeoff);
                newRemainingLength = 0;
                isWaypoint = true;
            }
            // slope is so short that the obstacle starts before it but ends after it.
            else
            {
                newEndPoint = GetFinishedEndPoint();
                coords.Add(DrawRamp(waypointStartXZ, newEndPoint, startHeight));                
                FitObstacleOnSlope(takeoff); // place the landing on the flat terrain
                newRemainingLength = 0;
                isWaypoint = true;
            }                   

            UpdateHighlight(newRemainingLength, takeoff.GetEndPoint(), takeoff.GetRideDirection());

            LastPlacementResult = new PlacementResult(newRemainingLength, newEndPoint, isWaypoint, coords);

            return;
        }     
        
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
            HeightmapCoordinates coords = new();

            Vector3 landingStartXZ = landing.GetStartPoint();
            landingStartXZ.y = EndPoint.y;

            Vector3 landingEndXZ = landing.GetEndPoint();
            landingEndXZ.y = EndPoint.y;


            float distanceToStartXZ = Vector3.Distance(EndPoint, landingStartXZ);

            Width = Mathf.Max(Width, landing.GetBottomWidth() + 1f);

            if (isTilted)
            {
                Vector3 landingPositionXZ = landing.GetTransform().position;
                landingPositionXZ.y = EndPoint.y;
                float startHeight = landing.GetStartPoint().y + 0.05f;
                coords.Add(DrawRamp(landing.GetStartPoint(), landing.GetEndPoint(), startHeight));
                newEndPoint = landing.GetEndPoint();
                newRemainingLength -= Vector3.Distance(EndPoint, landingPositionXZ) + landing.GetLandingAreaLengthXZ();
            }
            else
            {
                coords.Add(DrawFlat(landing.GetStartPoint(), landing.GetEndPoint(), landingPosition.y));

                newRemainingLength = 0;
                newEndPoint = EndPoint + (landing.GetStartPoint() - EndPoint).normalized * distanceToStartXZ + landing.GetRideDirection() * (newRemainingLength - distanceToStartXZ);
                newEndPoint.y = endHeight;
            }

            UpdateHighlight(newRemainingLength, landing.GetEndPoint(), landing.GetRideDirection());

            LastRideDirection = Vector3.ProjectOnPlane(rideDirXZ, Vector3.up).normalized;

            LastPlacementResult = new(newRemainingLength, newEndPoint, true, coords);
        }
                
        public void ConfirmChanges<T>(ObstacleBase<T> element) where T : MeshGeneratorBase
        {

            if (LastPlacementResult.IsWaypoint && element.TryGetComponent<ILineElement>(out var lineElement))
            {
                Waypoints.AddWaypoint(lineElement);
                element.AddSlopeHeightmapCoords(LastPlacementResult.ChangedHeightmapCoords);
                
                // if the element is actually built on top of the slope and not after its end, mark it as the current
                // last on slope element
                if (Vector3.Angle(Vector3.ProjectOnPlane(element.GetRideDirection(), Vector3.up),
                        element.GetRideDirection()) > float.Epsilon)
                {
                    LastElementOnSlope = lineElement;
                }
                
            }

            RemainingLength = LastPlacementResult.Remaininglength;
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
                Debug.LogError("Deleting a slope change with waypoints. This should not happen.");
                Waypoints.Clear();
            }

            TerrainManager.Instance.RemoveSlope(this);
            
            FlatToStartPoint.MarkAs(new FreeCoordinateState());

            Destroy(gameObject);
        }

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
            startHeight = data.startHeight;
            endHeight = data.endHeight;
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