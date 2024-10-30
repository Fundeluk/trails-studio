using Assets.Scripts.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct LineElement
{
    public readonly Transform obstacle;
    public readonly float height; // how high from terrain the obstacle is
    public readonly float length; // how long in the direction of riding the obstacle is
    public readonly Vector3 endPoint;
    public readonly Vector3 rideDirection; // a vector that points in the direction of rideout of the last obstacle on line

    public LineElement(Transform obstacle, float length, float height, Vector3 endPoint, Vector3 rideDirection)
    {
        this.obstacle = obstacle;
        this.length = length;
        this.height = height;
        this.endPoint = endPoint;
        this.rideDirection = rideDirection;
    }
}

public class Line : Singleton<Line>
{
    public List<LineElement> line = new List<LineElement>();
    public void AddToLine(LineElement element)
    {
        line.Add(element);
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
