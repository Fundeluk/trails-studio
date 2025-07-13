using Assets.Scripts.Builders;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.Managers
{   
    [Serializable]
    public class SerializableHeightmapCoordinates
    {
        public int startX;
        public int startY;
        /// <summary>
        /// X axis
        /// </summary>
        public int arrayWidth;
        /// <summary>
        /// Z axis
        /// </summary>
        public int arrayHeight;

        public List<int2> coordinates = new();        

        public SerializableHeightmapCoordinates(HeightmapCoordinates coords)
        {
            startX = coords.startX;
            startY = coords.startY;
            arrayWidth = coords.arrayWidth;
            arrayHeight = coords.arrayHeight;
            foreach (var coord in coords.coordinates)
            {
                coordinates.Add(coord);
            }
        }

        public HeightmapCoordinates ToHeightmapCoordinates()
        {
            
            HeightmapCoordinates coords = new(startX, startY, arrayWidth, arrayHeight, coordinates);
           
            return coords;
        }
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
        
        public SerializableHeightmapCoordinates slopeHeightmapCoordinates;
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

            var slopeCoords = takeoff.GetUnderlyingSlopeHeightmapCoordinates();
            if (slopeCoords == null)
            {
                slopeHeightmapCoordinates = null;
            }
            else
            {
                // If slope coordinates are not null, serialize them
                slopeHeightmapCoordinates = new SerializableHeightmapCoordinates(slopeCoords);
            }

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

            var slopeCoords = landing.GetUnderlyingSlopeHeightmapCoordinates();
            if (slopeCoords == null)
            {
                slopeHeightmapCoordinates = null;
            }
            else
            {
                // If slope coordinates are not null, serialize them
                slopeHeightmapCoordinates = new SerializableHeightmapCoordinates(slopeCoords);
            }
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
            slopeHeightmapCoordinates = null;
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
            remaininglength = placementResult.Remaininglength;
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
            slopeId = TerrainManager.Instance.slopeChanges.IndexOf(snapshot.slope);
            finished = snapshot.finished;
            remainingLength = snapshot.remainingLength;
            width = snapshot.width;
            endPoint = snapshot.endPoint;
            lastRideDir = snapshot.lastRideDir;
        }

        public SlopeChange.SlopeSnapshot ToSlopeSnapshot()
        {
            SlopeChange slope = TerrainManager.Instance.slopeChanges[slopeId];
            return new SlopeChange.SlopeSnapshot(slope, finished, remainingLength, width, endPoint, lastRideDir);
        }
    }

    [Serializable]
    public class WaypointListData
    {
        public List<int> waypointIndices = new();
        public List<SlopeSnapshotData> snapshots = new();

        public WaypointListData(SlopeChange.WaypointList list)
        {
            foreach (var item in list)
            {
                waypointIndices.Add(Line.Instance.GetLineElementIndex(item.Item1));
                snapshots.Add(new SlopeSnapshotData(item.Item2));
            }
        }        
    }

    [Serializable]
    public class SerializableHeightmap
    {
        public int resolution;
        public float globalHeightLevelNormalized;

        [Serializable]
        public class SerializableHeightmapCoordinate
        {
            public int2 coord;
            public float value;
        }

        public List<SerializableHeightmapCoordinate> heightValues = new List<SerializableHeightmapCoordinate>();

        public SerializableHeightmap(TerrainManager terrainManager)
        {
            globalHeightLevelNormalized = TerrainManager.WorldUnitsToHeightmapUnits(terrainManager.GlobalHeightLevel);

            TerrainData terrainData = TerrainManager.Floor.terrainData;
            resolution = terrainData.heightmapResolution;

            CoordinateStateHolder[,] untouchedTerrainMap = terrainManager.UntouchedTerrainMap;

            // Get the full heightmap
            float[,] heights = terrainData.GetHeights(0, 0, resolution, resolution);

            // Save just the changed values
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    if (untouchedTerrainMap[y, x].GetState() != CoordinateState.Free)
                    {
                        heightValues.Add(new SerializableHeightmapCoordinate
                        {
                            coord = new int2(x, y),
                            value = heights[y, x]
                        });
                    }
                }
            }
        }

        public float[,] ToHeightmap()
        {
            float[,] heights = new float[resolution, resolution];

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    heights[y,x] = globalHeightLevelNormalized; // Initialize with global height level
                }
            }

            foreach (var heightValue in heightValues)
            {
                // Set the height value at the specified coordinate
                heights[heightValue.coord.y, heightValue.coord.x] = heightValue.value;
            }

            return heights;
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

            if (slope.LastConfirmedSnapshot != null)
            {
                lastConfirmedSnapshot = new SlopeSnapshotData(slope.LastConfirmedSnapshot);
            }
            else
            {
                lastConfirmedSnapshot = null;
            }

            lastPlacementResult = new(slope.LastPlacementResult);

            previousLineElementIndex = slope.PreviousLineElement.GetIndex();
        }
    }

    [Serializable]
    public class LineData
    {
        public RollInData rollIn;

        public List<TakeoffData> takeoffs = new List<TakeoffData>();

        public List<LandingData> landings = new List<LandingData>();

        public LineData(Line line)
        {
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
    public class UntouchedTerrainMapData
    {
        [Serializable]
        public class UntouchedTerrainMapCoordinate
        {
            public int2 coord;
            public CoordinateState state;
            public int occupyingElementIndex = -1; // -1 means no element occupies this coordinate
        }

        // Store terrain resolution for reconstruction
        public int heightmapResolution;

        // Store coordinates that are not in Free state
        public List<UntouchedTerrainMapCoordinate> coords = new();
        
        // Constructor that creates a serialized version of the terrain map
        public UntouchedTerrainMapData(CoordinateStateHolder[,] untouchedTerrainMap)
        {
            heightmapResolution = untouchedTerrainMap.GetLength(0);

            // Only store non-default states
            for (int y = 0; y < heightmapResolution; y++)
            {
                for (int x = 0; x < heightmapResolution; x++)
                {
                    CoordinateStateHolder stateHolder = untouchedTerrainMap[y, x];

                    CoordinateState state = stateHolder.GetState();
                    // Skip Free states as they're the default
                    if (state == CoordinateState.Free)
                        continue;

                    var coord = new UntouchedTerrainMapCoordinate
                    {
                        coord = new int2(x, y),
                        state = state,

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
                }
            }
        }

        // Method to reconstruct the terrain map
        public CoordinateStateHolder[,] ToTerrainMap()
        {
            CoordinateStateHolder[,] map = new CoordinateStateHolder[heightmapResolution, heightmapResolution];

            // Initialize all to Free state
            for (int y = 0; y < heightmapResolution; y++)
            {
                for (int x = 0; x < heightmapResolution; x++)
                {
                    map[y, x] = new FreeCoordinateState();
                }
            }

            // Apply the stored states
            for (int i = 0; i < coords.Count; i++)
            {
                var coord = coords[i];

                if (coord.state == CoordinateState.HeightSet)
                {
                    map[coord.coord.y, coord.coord.x] = new HeightSetCoordinateState();
                }
                else if (coord.state == CoordinateState.Occupied && coord.occupyingElementIndex >= 0)
                {
                    // Get the element from the line using the stored index
                    ILineElement occupyingElement = Line.Instance[coord.occupyingElementIndex];
                    map[coord.coord.y, coord.coord.x] = new OccupiedCoordinateState(occupyingElement);
                }
            }

            return map;
        }
    }

    [Serializable]
    public class TerrainManagerData
    {
        public float globalHeight;
        public List<SlopeData> slopes = new List<SlopeData>();

        public SerializableHeightmap heightmap;

        public TerrainManagerData (TerrainManager terrainManager)
        {
            globalHeight = terrainManager.GlobalHeightLevel;

            foreach (var slope in terrainManager.slopeChanges)
            {
                slopes.Add(new SlopeData(slope));
            }

            heightmap = new(terrainManager);
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

        public const string saveFileExt = ".dirt";

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
                line = new LineData(Line.Instance),
                terrain = new TerrainManagerData(TerrainManager.Instance)
            };

            string json = JsonUtility.ToJson(saveData, true);
            string path = Path.Combine(SaveDirectory, saveName + saveFileExt);
            File.WriteAllText(path, json);

            Debug.Log($"Game saved to: {path}");
            StudioUIManager.Instance.ShowMessage($"Line saved as '{saveName}'", 2f);
        }

        public bool LoadLine(string saveName)
        {
            string path = Path.Combine(SaveDirectory, saveName + saveFileExt);
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
            string path = Path.Combine(SaveDirectory, saveName + saveFileExt);
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
            foreach (var slope in new List<SlopeChange>(TerrainManager.Instance.slopeChanges))
            {
                slope.Delete();
            }

            // Reset terrain height
            TerrainManager.Instance.SetHeight(0);
        }

        public string[] GetSaveFiles()
        {
            if (!Directory.Exists(SaveDirectory))
                return new string[0];

            string[] files = Directory.GetFiles(SaveDirectory, "*" + saveFileExt);
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
            return files;
        }
    }
}