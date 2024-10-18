using Assets.Scripts.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line : Singleton<Line>
{
    public List<GameObject> line = new List<GameObject>();
    public Vector3 currentLineEndPoint;

    public void AddToLine(GameObject obstacle, Vector3 endPoint)
    {
        line.Add(obstacle);
        currentLineEndPoint = endPoint;
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
