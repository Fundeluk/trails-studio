using Assets.Scripts.Builders;
using Assets.Scripts.Managers;
using Assets.Scripts.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;


public interface ILineElement
{
    public int GetIndex();
    public Transform GetTransform();

    public Vector3 GetStartPoint();

    public GameObject GetCameraTarget();

    public Terrain GetTerrain();

    public HeightmapBounds GetHeightmapBounds();

    public void SetHeight(float height);
    public float GetHeight();

    public void SetLength(float length);
    public float GetLength();

    public Vector3 GetEndPoint();

    public void SetRideDirection(Vector3 rideDirection);
    public Vector3 GetRideDirection();

    public float GetWidth();

    /// <summary>
    /// Returns the width of the line element at its bottom level.
    /// </summary>
    public float GetBottomWidth();

    public void DestroyUnderlyingGameObject();
}


public class Line : Singleton<Line>
{
    // TODO handle coupling of takeoff and landing

    public List<ILineElement> line = new();

    /// <summary>
    /// If a slope change is built, but there is nothing built farther from its start than its length, it is active
    /// </summary>
    public SlopeChange activeSlopeChange = null;

    public const int baseHeight = 50; // to reflect height of terrain, this is the height that signifies the ground level

    //public Spline spline;

    public GameObject takeoffPrefab;
    public GameObject landingPrefab;
    public GameObject pathProjectorPrefab;

    //private void OnDrawGizmos()
    //{
    //    if (TerrainManager.Instance.slopeModifiers.Count != 0)
    //    {
    //        TerrainManager.Instance.slopeModifiers[^1].OnDrawGizmos();
    //    }
    //    else if (activeSlopeChange != null)
    //    {
    //        activeSlopeChange.OnDrawGizmos();
    //    }
    //}


    /// <summary>
    /// Adds an already created LineElement to the line.
    /// </summary>
    /// <param name="element">The LineElement to add</param>
    public void AddLineElement(ILineElement element)
    {
        line.Add(element);
        if (activeSlopeChange != null )
        {
            bool finished = activeSlopeChange.AddWaypoint(element);
            if (finished)
            {
                activeSlopeChange = null;
            }
        }
        else
        {
            TerrainManager.Instance.MarkTerrainAsOccupied(element.GetHeightmapBounds());
        }

        //var splineContainer = GetComponent<SplineContainer>();
        //spline.Add(splineContainer.transform.InverseTransformPoint(element.GetTransform().position));
    }

    /// <summary>
    /// Instantiates a takeoff prefab at the given position, rotates it in ride direction and adds its LineElement to the line.
    /// </summary>
    /// <param name="position">Position where the takeoff should be placed</param>
    /// <returns>The instantiated takeoff</returns>
    public TakeoffMeshGenerator.Takeoff AddTakeOff(Vector3 position)
    {
        GameObject takeoff = Instantiate(takeoffPrefab, position, Quaternion.LookRotation(GetCurrentRideDirection(), Vector3.up), transform);

        var meshBuilder = takeoff.GetComponent<TakeoffMeshGenerator>();

        // create the line element representing the takeoff and add it to the line
        TakeoffMeshGenerator.Takeoff element = new(meshBuilder, line.Count, TerrainManager.GetTerrainForPosition(takeoff.transform.position));
        AddLineElement(element);

        return element;
    }

    public LandingMeshGenerator.Landing AddLanding(Vector3 position, Vector3 rideDirection)
    {
        // check if last line element is a takeoff
        if (GetLastLineElement() is not TakeoffMeshGenerator.Takeoff)
        {
            throw new InvalidOperationException("Cannot add a landing without a takeoff before it.");
        }

        GameObject landing = Instantiate(landingPrefab, position, Quaternion.LookRotation(rideDirection, Vector3.up), transform);

        var meshBuilder = landing.GetComponent<LandingMeshGenerator>();

        TakeoffMeshGenerator.Takeoff takeoff = (TakeoffMeshGenerator.Takeoff)GetLastLineElement();

        LandingMeshGenerator.Landing element = new(meshBuilder, takeoff, line.Count, TerrainManager.GetTerrainForPosition(landing.transform.position));

        takeoff.SetLanding(element);

        AddLineElement(element);

        return element;
    }

    public Vector3 GetCurrentRideDirection()
    {
        if (line.Count == 0)
        {
            Debug.LogError("Ride directon request error: no line elements in the line.");
            return Vector3.forward;
        }

        return GetLastLineElement().GetRideDirection();
    }


    public void DestroyLastLineElement()
    {
        ILineElement lastElement = GetLastLineElement();

        line.RemoveAt(line.Count - 1);

        lastElement.DestroyUnderlyingGameObject();

        //spline.RemoveAt(line.Count - 1);
    }

    /// <summary>
    /// Destroys all line elements from index onwards. 
    /// </summary>
    /// <param name="index">Index where the deletion starts</param>
    public void DestroyLineElementAt(int index)
    {
        if (index >= line.Count || index <= 0)
        {
            throw new IndexOutOfRangeException("Index out of bounds.");
        }

        int count = line.Count - index;

        for (int i = 0; i < count; i++)
        {
            DestroyLastLineElement();
        }
    }

    public ILineElement GetLastLineElement()
    {
        if (line.Count == 0)
        {
            Debug.LogError("No line elements in the line.");
            return null;
        }

        return line[^1];
    }

    private void Start()
    {
        //spline = GetComponent<SplineContainer>().AddSpline();
        //List<BezierKnot> knots = new();
        //spline.Knots = knots;
    }
}
