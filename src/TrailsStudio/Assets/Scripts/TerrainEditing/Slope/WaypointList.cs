using System.Collections;
using System.Collections.Generic;
using LineSystem;
using Managers;

namespace TerrainEditing.Slope
{
    public partial class SlopeChange
    {
        /// <summary>
        /// A collection that manages obstacles positioned as waypoints along a generated slope.
        /// Tracks entries coupled with historical layout states for undo actions.
        /// </summary>
        public class WaypointList : IEnumerable<(ILineElement, SlopeSnapshot, TerrainManager.HeightmapCoordinates)>, ISaveable<WaypointListData>
        {            

            public readonly SlopeChange Owner;

            /// <summary>
            /// Internal registry containing the line elements bound as waypoints, with snapshot contexts and their terrain occupancy data.
            /// </summary>
            private readonly List<(ILineElement element, SlopeSnapshot snapshot, TerrainManager.HeightmapCoordinates affectedCoords)> waypoints = new();

            /// <summary>
            /// Registers a newly confirmed line element as a waypoint connected to this slope logic.
            /// Records the prior shape state inside a new snapshot context.
            /// </summary>
            public void AddWaypoint(ILineElement waypoint, TerrainManager.HeightmapCoordinates affectedCoords)
            {
                // only when the first waypoint is added, mark the flat to start point as occupied to avoid placement issues
                // with the first waypoint (the flat would be marked as occupied and the waypoint couldn't be placed there)
                if (waypoints.Count == 0)
                {
                    Owner.FlatToStartPoint.MarkAs(new HeightSetCoordinateState());
                }

                StudioUIManager.Instance.GetSidebar().DeleteButtonEnabled = true;
                StudioUIManager.Instance.GetSidebar().DeleteSlopeButtonEnabled = false;


                SlopeSnapshot snapshot = Owner.lastConfirmedSnapshot;
                waypoints.Add((waypoint, snapshot, affectedCoords));
                waypoint.SetSlopeChange(Owner);
            }

            public (ILineElement element, SlopeSnapshot snapshot, TerrainManager.HeightmapCoordinates affectedCoords) this[int index] => waypoints[index];

            public bool TryFindByElement(ILineElement element, out SlopeSnapshot snapshot, out TerrainManager.HeightmapCoordinates affectedCoords)
            {
                foreach (var waypoint in waypoints)
                {
                    if (waypoint.element == element)
                    {
                        snapshot = waypoint.snapshot;
                        affectedCoords = waypoint.affectedCoords;
                        return true;
                    }
                }
                affectedCoords = null;
                snapshot = null;
                return false;
            }

            /// <summary>
            /// Excises the selected inline element and reverts affected parts of the slope to the state before said element was placed.
            /// </summary>
            public bool RemoveWaypoint(ILineElement item)
            {
                if (TryFindByElement(item, out var snapshot, out var affectedCoords))
                {
                    item.SetSlopeChange(null);
                    snapshot.Revert(); // revert the slope to the state before the waypoint was added
                    affectedCoords?.MarkAs(new FreeCoordinateState()); // unmark the heightmap coordinates of the waypoint

                    TerrainManager.Instance.SetHeight(snapshot.EndPoint.y); // set the height of the terrain to the height at the waypoint

                    Owner.lastConfirmedSnapshot = snapshot;

                    waypoints.Remove((item, snapshot, affectedCoords));
                    return true;
                }

                return false;
            }
            
            
            public void Clear()
            {
                foreach ((ILineElement element, var _, var affectedCoordinates) in waypoints)
                {
                    element.SetSlopeChange(null);
                    affectedCoordinates?.MarkAs(new FreeCoordinateState());
                }
                waypoints.Clear();
            }

            public int Count => waypoints.Count;          


            public IEnumerator<(ILineElement, SlopeSnapshot, TerrainManager.HeightmapCoordinates)> GetEnumerator()
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
                    var affectedCoords = data.affectedCoords[i];

                    if (element == null)
                    {
                        InternalDebug.LogWarning($"Waypoint with index {data.waypointIndices[i]} not found in the line. Skipping loading.");
                        continue;
                    }

                    element.SetSlopeChange(Owner);

                    waypoints.Add((element, snapshot.ToSlopeSnapshot(), new TerrainManager.HeightmapCoordinates(affectedCoords)));
                }
                
            }

            public WaypointList(SlopeChange owner)
            {
                this.Owner = owner;
            }
        }
    }
}