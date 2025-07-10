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
    public class LineElementData
    {
        public Vector3 position;
        public Quaternion rotation;
        public int lineIndex;
        
        public SerializableHeightmapCoordinates slopeHeightmapCoordinates;
        public SerializableHeightmapCoordinates obstacleHeightmapCoordinates;        
    }

    [Serializable]
    public class TakeoffData : LineElementData
    {
        public float height;
        public float width;
        public float radius;
        public float thickness;
        public float entrySpeed;

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

            obstacleHeightmapCoordinates = new SerializableHeightmapCoordinates(takeoff.GetObstacleHeightmapCoordinates());
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

            obstacleHeightmapCoordinates = new SerializableHeightmapCoordinates(landing.GetObstacleHeightmapCoordinates());
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
            obstacleHeightmapCoordinates = new SerializableHeightmapCoordinates(rollIn.GetObstacleHeightmapCoordinates());
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
        public int ownerId;
        public List<int> waypointIndices = new();
        public List<SlopeSnapshotData> snapshots = new();

        public WaypointListData(SlopeChange.WaypointList list)
        {
            ownerId = TerrainManager.Instance.slopeChanges.IndexOf(list.owner);
            foreach (var item in list)
            {
                waypointIndices.Add(Line.Instance.GetLineElementIndex(item.Item1));
                snapshots.Add(new SlopeSnapshotData(item.Item2));
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
        public WaypointListData waypoints;
        public SlopeSnapshotData lastConfirmedSnapshot;
        public SerializableHeightmapCoordinates flatToStartCoords;

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
            finished = slope.Finished;

            waypoints = new WaypointListData(slope.Waypoints);

            if (slope.LastConfirmedSnapshot != null)
            {
                lastConfirmedSnapshot = new SlopeSnapshotData(slope.LastConfirmedSnapshot);
            }
            else
            {
                lastConfirmedSnapshot = null;
            }

            flatToStartCoords = new SerializableHeightmapCoordinates(slope.FlatToStartPoint);
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
    public class TerrainMapData
    {
        // Store terrain resolution for reconstruction
        public int heightmapResolution;

        // Store coordinates that are not in Free state
        public List<int2> coords = new();

        // Store the state of each coordinate (0=HeightSet, 1=Occupied)
        public List<byte> stateTypes = new List<byte>();

        // For occupied states, store which line element occupies it
        public List<int> occupyingElementIndices = new List<int>();
        
        // Constructor that creates a serialized version of the terrain map
        public TerrainMapData(CoordinateStateHolder[,] untouchedTerrainMap)
        {
            heightmapResolution = untouchedTerrainMap.GetLength(0);

            // Only store non-default states
            for (int y = 0; y < heightmapResolution; y++)
            {
                for (int x = 0; x < heightmapResolution; x++)
                {
                    CoordinateStateHolder state = untouchedTerrainMap[y, x];

                    // Skip Free states as they're the default
                    if (state.GetState() == CoordinateState.Free)
                        continue;

                    // Store the coordinates
                    coords.Add(new int2(x, y));

                    if (state.GetState() == CoordinateState.HeightSet)
                    {
                        stateTypes.Add(0);
                        occupyingElementIndices.Add(-1); // No occupying element
                    }
                    else if (state is OccupiedCoordinateState occupiedState)
                    {
                        stateTypes.Add(1);

                        // Find the index of the occupying element in the line
                        int elementIndex = Line.Instance.GetLineElementIndex(occupiedState.OccupyingElement);
                        occupyingElementIndices.Add(elementIndex);
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
                int2 coord = coords[i];
                byte stateType = stateTypes[i];
                int elementIndex = occupyingElementIndices[i];

                if (stateType == 0) // HeightSet
                {
                    map[coord.y, coord.x] = new HeightSetCoordinateState();
                }
                else if (stateType == 1 && elementIndex >= 0) // Occupied
                {
                    
                    map[coord.y, coord.x] = new OccupiedCoordinateState(Line.Instance[elementIndex]);                    
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

        public TerrainMapData terrainMap;

        public TerrainManagerData (TerrainManager terrainManager)
        {
            globalHeight = terrainManager.GlobalHeightLevel;

            foreach (var slope in terrainManager.slopeChanges)
            {
                slopes.Add(new SlopeData(slope));
            }

            terrainMap = new TerrainMapData(terrainManager.UntouchedTerrainMap);
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
            UIManager.Instance.ShowMessage($"Line saved as '{saveName}'", 2f);
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
                UIManager.Instance.ShowMessage($"Line '{saveName}' loaded successfully", 2f);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading save: {e.Message}");
                return false;
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