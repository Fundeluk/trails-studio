using Assets.Scripts.Builders;
using Assets.Scripts.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILineElement
{
    public Transform GetTransform();

    public GameObject GetCameraTarget();

    public void SetHeight(float height);
    public float GetHeight();

    public void SetLength(float length);
    public float GetLength();

    public Vector3 GetEndPoint();

    public void SetRideDirection(Vector3 rideDirection);
    public Vector3 GetRideDirection();

    public void DestroyUnderlyingGameObject();
}


public class Line : Singleton<Line>
{
    // TODO handle coupling of takeoff and landing

    public List<ILineElement> line = new();

    public GameObject takeoffPrefab;
    public GameObject landingPrefab;

    /// <summary>
    /// Adds an already created LineElement to the line.
    /// </summary>
    /// <param name="element">The LineElement to add</param>
    public void AddLineElement(ILineElement element)
    {
        line.Add(element);
    }

    /// <summary>
    /// Instantiates a takeoff prefab at the given position, rotates it in ride direction and adds its LineElement to the line.
    /// </summary>
    /// <param name="position">Position where the takeoff should be placed</param>
    /// <returns>The instantiated takeoff</returns>
    public TakeoffMeshGenerator.Takeoff AddTakeOff(Vector3 position)
    {
        GameObject takeoff = Instantiate(takeoffPrefab, position, Quaternion.LookRotation(line[^1].GetRideDirection(), Vector3.up), transform);

        var meshBuilder = takeoff.GetComponent<TakeoffMeshGenerator>();

        // create the line element representing the takeoff and add it to the line
        TakeoffMeshGenerator.Takeoff element = new(meshBuilder);
        line.Add(element);

        return element;
    }

    public LandingMeshGenerator.Landing AddLanding(Vector3 position, Vector3 rideDirection)
    {
        // check if last line element is a takeoff
        if (line[^1] is not TakeoffMeshGenerator.Takeoff)
        {
            throw new InvalidOperationException("Cannot add a landing without a takeoff before it.");
        }

        GameObject landing = Instantiate(landingPrefab, position, Quaternion.LookRotation(rideDirection, Vector3.up), transform);

        var meshBuilder = landing.GetComponent<LandingMeshGenerator>();

        TakeoffMeshGenerator.Takeoff takeoff = (TakeoffMeshGenerator.Takeoff)line[^1];

        LandingMeshGenerator.Landing element = new(meshBuilder, takeoff);

        takeoff.SetLanding(element);

        line.Add(element);

        return element;
    }

    public void DestroyLastLineElement()
    {
        ILineElement lastElement = line[^1];

        line.RemoveAt(line.Count - 1);

        lastElement.DestroyUnderlyingGameObject();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
