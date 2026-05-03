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

        private WaypointList waypoints;

        /// <summary>
        /// The end point of the slope. If the slope is not finished, this is an end point of the realized portion of the slope.
        /// </summary>
        public Vector3 EndPoint { get; private set; }

        public Vector3 LastRideDirection { get; private set; }


        /// <summary>
        /// The most recent saved state of the slope, used to revert unconfirmed changes.
        /// </summary>
        private SlopeSnapshot lastConfirmedSnapshot;

        public override bool Finished => RemainingLength <= 0;

        public bool IsBuiltOn => waypoints.Count > 0;

        /// <summary>
        /// Creates a snapshot of the current state of the slope.
        /// </summary>
        private SlopeSnapshot GetSlopeSnapshot() => new(this);

        /// <summary>
        /// Tracks the result of the last attempted obstacle placement on the slope.
        /// </summary>
        private PlacementResult lastPlacementResult;

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
            waypoints = new WaypointList(this);
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

            lastPlacementResult = null;
            lastConfirmedSnapshot = GetSlopeSnapshot();
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
        public bool IsBeforeStart(Vector3 position, Vector3 referenceRideDirection)
        {
            if (waypoints.Count > 0)
            {
                // if waypoints are added, the slope can go in any direction, even before its start,
                // so we can't check if the position is before the slope's start
                return false;
            }

            Vector3 slopeStartToPosition = Vector3.ProjectOnPlane(position - Start, Vector3.up);
            float projection = Vector3.Dot(slopeStartToPosition, referenceRideDirection);

            return projection < 0; // if the position is before the slope start, the projection is negative
        }

        /// <summary>
        /// Checks if a position lies within the currently active, unbuilt portion of the slope.
        /// </summary>
        /// <returns>True if the slope is not yet finished 
        /// and the position is on the part from current endpoint to the potential finished endpoint of the slope.</returns>
        public bool IsOnActivePartOfSlope(Vector3 position, Vector3 referenceRideDirection)
        {
            if (Finished)
            {
                return false;
            }

            Vector3 endPointToPosition = Vector3.ProjectOnPlane(position - EndPoint, Vector3.up).normalized;
            float projection = Vector3.Dot(endPointToPosition, referenceRideDirection);

            Vector3 endPointXZ = new(EndPoint.x, 0, EndPoint.z);
            Vector3 positionXZ = new(position.x, 0, position.z);

            return Vector3.Distance(endPointXZ, positionXZ) <= RemainingLength && projection >= 0;
        }

        /// <summary>
        /// Checks if a position is located beyond the determined end of the slope.
        /// </summary>
        public bool IsAfterSlope(Vector3 position, Vector3 referenceRideDirection)
        {
            Vector3 endPointToPosition = Vector3.ProjectOnPlane(position - EndPoint, Vector3.up).normalized;
            float projection = Vector3.Dot(endPointToPosition, referenceRideDirection);

            Vector3 endPointXZ = new(EndPoint.x, 0, EndPoint.z);
            Vector3 positionXZ = new(position.x, 0, position.z);

            return projection > 0 && Vector3.Distance(endPointXZ, positionXZ) > RemainingLength;
        }

        /// <summary>
        /// Returns the end point of the slope if finished, otherwise where the slope will end in the referenceRideDirection>
        /// </summary>
        public Vector3 GetFinishedEndPoint(Vector3 referenceRideDirection)
        {
            if (Finished)
            {
                return EndPoint;
            }

            Vector3 result = EndPoint + referenceRideDirection * RemainingLength;
            result.y = EndHeight;
            return result;
        }

        /// <summary>
        /// Calculates the height difference for a given XZ distance using the slope's Angle.
        /// </summary>
        /// <remarks>Is not bounded by the slope's <see cref="RemainingLength"/></remarks>
        public float GetHeightDifferenceForXZDistance(float distance)
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
        public void FitObstacleOnSlope(IObstacleBuilder builder)
        {
            Vector3 rideDir = Vector3.ProjectOnPlane(builder.GetRideDirection(), Vector3.up).normalized;
            Vector3 angledRideDir = TiltVectorBySlopeAngle(rideDir);

            builder.GetTransform().forward = angledRideDir;
            float newHeight = TerrainManager.Instance.GetHeightAt(builder.GetTransform().position);
            builder.GetTransform().position = new(builder.GetTransform().position.x, newHeight,
                builder.GetTransform().position.z);
        }
        
        /// <summary>
        /// Returns the line indices of the first waypoint and last on-slope line element of this slope change.
        /// </summary>
        public (int firstLineIndex, int? lastLineIndex)? GetFirstAndLastWaypointLineIndices()
        {
            if (waypoints.Count == 0)
                return null;

            int first = waypoints[0].element.GetIndex();
            
            if (LastElementOnSlope == null)
                return (first, null);
            
            int last = LastElementOnSlope.GetIndex();
            return  (first, last);
        }
        
        public void DiscardTentativePlacement()
        {
            lastPlacementResult?.Discard(EndPoint.y);
        }
        
        public void PlaceObstacle(IObstacleBuilder builder, Vector3 rawPosition, bool isTilted = false) 
        {
            lastPlacementResult = builder.PlaceOnSlope(this, rawPosition, isTilted);

            if (lastPlacementResult.IsWaypoint)
            {
                UpdateHighlight(lastPlacementResult.RemainingLength, builder.GetEndPoint(), lastPlacementResult.RideDirection);
            }
        }

        /// <summary>
        /// Applies the last calculated placement result, confirming the obstacle on the slope and establishing new bounds.
        /// </summary>
        public void ConfirmChanges<T>(ObstacleBase<T> element) where T : MeshGeneratorBase
        {
            // nothing to confirm if the placement did not influence the slope at all
            if (lastPlacementResult == null)
            {
                return;
            }
            
            var heightSetState = new HeightSetCoordinateState();
            lastPlacementResult.ChangedHeightmapCoords?.MarkAs(heightSetState);
            if (lastPlacementResult.IsWaypoint && element.TryGetComponent<ILineElement>(out var lineElement))
            {
                waypoints.AddWaypoint(lineElement, lastPlacementResult.ChangedHeightmapCoords);

                // if the element is actually built on top of the slope and not after its end, mark it as the current
                // last on slope element
                if (Vector3.Angle(Vector3.ProjectOnPlane(element.GetRideDirection(), Vector3.up),
                        element.GetRideDirection()) > float.Epsilon)
                {
                    LastElementOnSlope = lineElement;
                }

            }

            LastRideDirection = lastPlacementResult.RideDirection;
            Width = lastPlacementResult.Width;
            RemainingLength = lastPlacementResult.RemainingLength;
            EndPoint = lastPlacementResult.EndPoint;

            if (RemainingLength <= 0)
            {
                TerrainManager.Instance.ActiveSlope = null;
            }

            TerrainManager.Instance.SetHeight(EndPoint.y);
            lastPlacementResult = null;

            TerrainManager.ConfirmChanges();

            lastConfirmedSnapshot = GetSlopeSnapshot();

            UpdateHighlight();
        }
        
        /// <summary>
        /// Cancels the current unconfirmed obstacle placement, discarding terrain modifications 
        /// and restoring mutated slope state.
        /// </summary>
        public void CancelPlacement()
        {
            // discard terrain modifications
            lastPlacementResult?.Discard(EndPoint.y);
            lastPlacementResult = null;

            // restore base fields that were modified during placement
            lastConfirmedSnapshot.Revert();
        }

        /// <summary>
        /// Removes a placed waypoint from the slope and reconstructs the slope state to what it was before placement.
        /// </summary>
        public void RemoveWaypoint(ILineElement element)
        {
            waypoints.RemoveWaypoint(element);

            if (waypoints.Count == 0)
            {
                StudioUIManager.Instance.GetSidebar().DeleteSlopeButtonEnabled = true;
                LastElementOnSlope = null;
            }
            else
            {
                LastElementOnSlope = waypoints[^1].element;
            }
        }

        public void Delete()
        {
            if (waypoints.Count > 0)
            {
                InternalDebug.LogError("Deleting a slope change with waypoints. This should not happen.");
                waypoints.Clear();
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
            Gizmos.DrawSphere(GetFinishedEndPoint(LastRideDirection), 0.5f);
        }

        public SlopeData GetSerializableData() => new SlopeData(this, waypoints, lastConfirmedSnapshot);

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

            waypoints = new WaypointList(this);
            waypoints.LoadFromData(data.waypoints);
            
            if (IsBuiltOn)
            {
                FlatToStartPoint.MarkAs(new HeightSetCoordinateState());
            }

            UpdateHighlight();
        }
    }
}


