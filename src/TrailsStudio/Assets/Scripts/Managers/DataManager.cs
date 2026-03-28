using System;
using System.Collections.Generic;
using System.IO;
using LineSystem;
using Misc;
using Obstacles;
using Obstacles.Landing;
using Obstacles.TakeOff;
using States;
using TerrainEditing;
using TerrainEditing.Slope;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Managers
{   
    [Serializable]
    public class SerializableHeightmapCoordinates
    {
        [Serializable]
        public class SerializablePatch
        {
            public int2 terrainIndex;
            public int minX;
            public int minY;
            public int maxX;
            public int maxY;
            public List<int2> coordinates;
        }
        
        public List<SerializablePatch> patches;

        public SerializableHeightmapCoordinates(TerrainManager.HeightmapCoordinates coords)
        {
            patches = coords.ToSerializable();
        }

        public TerrainManager.HeightmapCoordinates ToHeightmapCoordinates() => new(this);
    }

    [Serializable]
    public class SerializableTrajectory
    {
        public List<Trajectory.TrajectoryPoint> points = new List<Trajectory.TrajectoryPoint>();
        public SerializableTrajectory(Trajectory trajectory)
        {
            if (trajectory == null)
                return;

            foreach (var point in trajectory)
            {
                points.Add(point);
            }            
        }

        public Trajectory ToTrajectory()
        {
            var trajectory = new Trajectory();
            trajectory.Clear();

            foreach (var point in points)
            {
                trajectory.Add(point.position, point.velocity);
            }

            return trajectory;
        }
    }

    [Serializable]
    public class LineElementData
    {
        public Vector3 position;
        public Quaternion rotation;
        public int lineIndex;
    }

    [Serializable]
    public class TakeoffData : LineElementData
    {
        public float height;
        public float width;
        public float radius;
        public float thickness;
        public float entrySpeed;

        public SerializableTrajectory trajectory;

        public TakeoffData(Takeoff takeoff)
        {
            position = takeoff.transform.position;
            rotation = takeoff.transform.rotation;
            lineIndex = takeoff.GetIndex();
           
            height = takeoff.GetHeight();
            width = takeoff.GetWidth();
            radius = takeoff.GetRadius();
            thickness = takeoff.GetThickness();
            entrySpeed = takeoff.EntrySpeed;
            
            trajectory = new(takeoff.MatchingTrajectory);
        }
    }

    [Serializable]
    public class LandingData : LineElementData
    {
        public float height;
        public float width;
        public float slopeAngle;
        public float thickness;
        public float exitSpeed;
        public LandingData(Landing landing)
        {
            position = landing.transform.position;
            rotation = landing.transform.rotation;
            lineIndex = landing.GetIndex();
            
            height = landing.GetHeight();
            width = landing.GetWidth();
            slopeAngle = landing.GetSlopeAngle();
            thickness = landing.GetThickness();
            exitSpeed = landing.ExitSpeed;
        }
    }

    [Serializable]
    public class  RollInData : LineElementData
    {
        public float height;
        public int angle;

        public float topSize;
        public float flatThickness;

        public RollInData(RollIn rollIn)
        {
            position = rollIn.transform.position;
            rotation = rollIn.transform.rotation;
            lineIndex = 0;
            
            height = rollIn.GetHeight();
            angle = rollIn.Angle;
            topSize = rollIn.TopSize;
            flatThickness = rollIn.FlatThickness;
        }
    }

    [Serializable]
    public class SerializablePlacementResult
    {
        public float remaininglength;

        public Vector3 newEndPoint;

        public bool isWaypoint;

        public SerializableHeightmapCoordinates changedHeightmapCoords;

        public SerializablePlacementResult(SlopeChange.PlacementResult placementResult)
        {
            remaininglength = placementResult.RemainingLength;
            newEndPoint = placementResult.NewEndPoint;
            isWaypoint = placementResult.IsWaypoint;

            if (placementResult.ChangedHeightmapCoords != null)
            {
                changedHeightmapCoords = new(placementResult.ChangedHeightmapCoords);
            }
            else
            {
                changedHeightmapCoords = null;
            }
        }

        public SlopeChange.PlacementResult ToPlacementResult()
        {
            return new SlopeChange.PlacementResult(remaininglength, newEndPoint, isWaypoint, changedHeightmapCoords.ToHeightmapCoordinates());
        }
    }

    [Serializable]
    public class SlopeSnapshotData
    {
        public int slopeId;
        public bool finished;
        public float remainingLength;
        public float width;
        public Vector3 endPoint;
        public Vector3 lastRideDir;

        public SlopeSnapshotData(SlopeChange.SlopeSnapshot snapshot)
        {
            slopeId = TerrainManager.Instance.SlopeChanges.IndexOf(snapshot.Slope);
            finished = snapshot.Finished;
            remainingLength = snapshot.RemainingLength;
            width = snapshot.Width;
            endPoint = snapshot.EndPoint;
            lastRideDir = snapshot.LastRideDir;
        }

        public SlopeChange.SlopeSnapshot ToSlopeSnapshot()
        {
            SlopeChange slope = TerrainManager.Instance.SlopeChanges[slopeId];
            return new SlopeChange.SlopeSnapshot(slope, finished, remainingLength, width, endPoint, lastRideDir);
        }
    }

    [Serializable]
    public class WaypointListData
    {
        public List<int> waypointIndices = new();
        public List<SlopeSnapshotData> snapshots = new();
        public List<SerializableHeightmapCoordinates> affectedCoords = new();

        public WaypointListData(SlopeChange.WaypointList list)
        {
            foreach (var item in list)
            {
                waypointIndices.Add(Line.Instance.GetLineElementIndex(item.Item1));
                snapshots.Add(new SlopeSnapshotData(item.Item2));
                affectedCoords.Add(new SerializableHeightmapCoordinates(item.Item3));
            }
        }        
    }

    [Serializable]
    public class SlopeData
    {
        public Vector3 start;
        public Vector3 end;
        public Vector3 lastRideDirection;
        public float startHeight;
        public float endHeight;
        public float length;
        public float remainingLength;
        public float width;
        public bool finished;
        public int previousLineElementIndex;
        public WaypointListData waypoints;
        public SlopeSnapshotData lastConfirmedSnapshot;
        public SerializablePlacementResult lastPlacementResult;

        public SlopeData(SlopeChange slope)
        {
            start = slope.Start;
            end = slope.EndPoint;
            lastRideDirection = slope.LastRideDirection;
            startHeight = slope.Start.y;
            endHeight = startHeight + slope.HeightDifference;
            length = slope.Length;
            remainingLength = slope.RemainingLength;
            width = slope.Width;

            waypoints = new WaypointListData(slope.Waypoints);

            lastConfirmedSnapshot = slope.LastConfirmedSnapshot != null ? new SlopeSnapshotData(slope.LastConfirmedSnapshot) : null;

            lastPlacementResult = new(slope.LastPlacementResult);

            previousLineElementIndex = slope.PreviousLineElement.GetIndex();
        }
    }

    [Serializable]
    public class LineData
    {
        public string name;

        public RollInData rollIn;

        public List<TakeoffData> takeoffs = new List<TakeoffData>();

        public List<LandingData> landings = new List<LandingData>();

        public LineData(Line line)
        {
            name = line.Name;

            rollIn = new(line.GetRollIn());
                
            for (int i = 1; i < line.Count; i++)
            {
                if (i % 2 == 1) // odd indices are takeoffs
                {
                    if (line[i] is Takeoff takeoff)
                    {
                        takeoffs.Add(new TakeoffData(takeoff));
                    }
                }
                else // even indices are landings
                {
                    if (line[i] is Landing landing)
                    {
                        landings.Add(new LandingData(landing));
                    }
                }
            }            
        }
    }

    [Serializable]
    public class MultiTerrainMapData
    {
        // Wrapper class for a single terrain's data
        [Serializable]
        public class TerrainDataWrapper
        {
            public int2 terrainIndex; // The grid coordinates of this terrain
            public List<SerializableMultiTerrainMapCoordinate> coordinates = new List<SerializableMultiTerrainMapCoordinate>();
        }
        
        [Serializable]
        public class SerializableMultiTerrainMapCoordinate
        {
            public int2 heightmapCoord;
            public float normalizedHeight;
            public CoordinateState state;
            public int occupyingElementIndex = -1; // -1 means no element occupies this coordinate

            public void Deconstruct(out int2 heightmapCoord, out float normalizedHeight,
                out CoordinateState state, out int occupyingElementIndex)
            {
                heightmapCoord = this.heightmapCoord;
                normalizedHeight = this.normalizedHeight;
                state = this.state;
                occupyingElementIndex = this.occupyingElementIndex;
            }
        }
        
        // Store coordinates that are not in Free state
        public List<TerrainDataWrapper> multiTerrainData = new();
        
        public int heightmapResolution;
        
        public MultiTerrainMapData(TerrainManager.MultiTerrainMap multiTerrainMap)
        {
            heightmapResolution = multiTerrainMap.HeightmapResolution;

            foreach (var (terrain, coordStates) in multiTerrainMap.Values)
            {
                int2 index = multiTerrainMap.GetIndex(terrain);
                
                TerrainDataWrapper terrainWrapper = new TerrainDataWrapper();
                terrainWrapper.terrainIndex = index;
                
                float[,] heightmap = terrain.terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
                
                // Only store non-default states
                for (int y = 0; y < heightmapResolution; y++)
                {
                    for (int x = 0; x < heightmapResolution; x++)
                    {
                        CoordinateStateHolder stateHolder = coordStates[y, x];

                        CoordinateState state = stateHolder.GetState();
                        // Skip Free states as they're the default
                        if (state == CoordinateState.Free)
                            continue;

                        var coord = new SerializableMultiTerrainMapCoordinate
                        {
                            heightmapCoord = new int2(x,y),
                            state = state,
                            normalizedHeight = heightmap[y, x]
                        };
                        
                        if (state == CoordinateState.HeightSet)
                        {
                            coord.occupyingElementIndex = -1; // No occupying element
                        }
                        else if (stateHolder is OccupiedCoordinateState occupiedState)
                        {
                            // Find the index of the occupying element in the line
                            int elementIndex = Line.Instance.GetLineElementIndex(occupiedState.OccupyingElement);
                            coord.occupyingElementIndex = elementIndex;
                        }
                        
                        terrainWrapper.coordinates.Add(coord);
                    }
                }
                
                multiTerrainData.Add(terrainWrapper);
            }
        }
        
        public TerrainManager.MultiTerrainMap ToTerrainMap() => new TerrainManager.MultiTerrainMap(this);
    }

    [Serializable]
    public class TerrainManagerData
    {
        public List<SlopeData> slopes = new();

        public MultiTerrainMapData multiTerrainMapData;
        
        public float globalHeightLevel;

        public TerrainManagerData (TerrainManager terrainManager, TerrainManager.MultiTerrainMap multiTerrainMap)
        {
            globalHeightLevel = terrainManager.GlobalHeightLevel;
            
            foreach (var slope in terrainManager.SlopeChanges)
            {
                slopes.Add(new SlopeData(slope));
            }

            multiTerrainMapData = new MultiTerrainMapData(multiTerrainMap);
        }        
    }

    [Serializable]
    public class SaveData
    {
        public string saveName;
        public string saveDate;
        public LineData line;
        public TerrainManagerData terrain;
    }

    public interface ISaveable<T>
    {
        /// <summary>
        /// Saves the current state of the object.
        /// </summary>
        T GetSerializableData();

        /// <summary>
        /// Loads the saved state of the object.
        /// </summary>
        void LoadFromData(T data);
    }

    public class DataManager : Singleton<DataManager>
    {
        public GameObject takeoffPrefab;

        public GameObject landingPrefab;

        public GameObject slopeChangePrefab;

        private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");

        private const string SAVE_FILE_EXT = ".dirt";

        private void Awake()
        {
            //DontDestroyOnLoad(gameObject);

            // Create save directory if it doesn't exist
            Directory.CreateDirectory(SaveDirectory);
        }

        public void SaveLine(string saveName)
        {
            if (StateController.Instance.CurrentState is not DefaultState)
            {
                Debug.LogError("Cannot save line in non-default state.");
                return;
            }

            SaveData saveData = new SaveData
            {
                saveName = saveName,
                saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                line = Line.Instance.GetSerializableData(),
                terrain = TerrainManager.Instance.GetSerializableData()
            };

            string json = JsonUtility.ToJson(saveData, true);
            string path = Path.Combine(SaveDirectory, saveName + SAVE_FILE_EXT);
            File.WriteAllText(path, json);

            Debug.Log($"Game saved to: {path}");
            StudioUIManager.Instance.ShowMessage($"Line saved as '{saveName}'", 2f);
        }

        public bool LoadLine(string saveName)
        {
            string path = Path.Combine(SaveDirectory, saveName + SAVE_FILE_EXT);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Save file not found: {path}");
                return false;
            }
            
            try
            {
                ClearCurrentState();
                string json = File.ReadAllText(path);
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);
            
                Line.Instance.LoadFromData(saveData.line);
            
                TerrainManager.Instance.LoadFromData(saveData.terrain);
            
                Debug.Log($"Game loaded from: {path}");
                StudioUIManager.Instance.ShowMessage($"Line '{saveName}' loaded successfully", 2f);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load save file: {ex.Message}");
                StudioUIManager.Instance.ShowMessage($"Failed to load line '{saveName}': {ex.Message}", 5f);
                return false;
            }
        }

        public void DeleteSave(string saveName)
        {
            string path = Path.Combine(SaveDirectory, saveName + SAVE_FILE_EXT);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"Save file deleted: {path}");
                StudioUIManager.Instance.ShowMessage($"Line '{saveName}' deleted successfully", 2f);
            }
            else
            {
                Debug.LogWarning($"Save file not found: {path}");
                StudioUIManager.Instance.ShowMessage($"Line '{saveName}' not found", 2f);
            }
        }

        private void ClearCurrentState()
        {
            // Reset the application to initial state
            StateController.Instance.ChangeState(new DefaultState());

            // Destroy existing elements
            while (Line.Instance.Count > 1) // Keep RollIn
            {
                ILineElement element = Line.Instance.GetLastLineElement();
                Line.Instance.DestroyLineElementsFromIndex(element.GetIndex());
            }

            // Clear slopes
            foreach (var slope in new List<SlopeChange>(TerrainManager.Instance.SlopeChanges))
            {
                slope.Delete();
            }

            // Reset terrain height
            TerrainManager.Instance.SetHeight(0);
        }

        public string[] GetSaveFiles()
        {
            if (!Directory.Exists(SaveDirectory))
                return Array.Empty<string>();

            string[] files = Directory.GetFiles(SaveDirectory, "*" + SAVE_FILE_EXT);
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
            return files;
        }
    }
}