using Assets.Scripts.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line : Singleton<Line>
{
    public List<GameObject> line = new List<GameObject>();
    public Vector3 currentLineEndPoint;
    public Vector3 currentRideDirection; // a vector that points in the direction of rideout of the last obstacle on line

    public Terrain terrain;

    public void AddToLine(GameObject obstacle, Vector3 endPoint, Vector3 rideDirection)
    {
        line.Add(obstacle);
        currentLineEndPoint = endPoint;
        currentRideDirection = rideDirection;
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
