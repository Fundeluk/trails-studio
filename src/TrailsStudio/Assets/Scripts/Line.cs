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

    public HeightmapCoordinates GetHeightmapCoordinates();

    public float GetHeight();

    public float GetLength();

    public Vector3 GetEndPoint();

    public Vector3 GetRideDirection();

    public float GetWidth();

    /// <summary>
    /// Returns the Width of the line element at its bottom level.
    /// </summary>
    public float GetBottomWidth();

    public void DestroyUnderlyingGameObject();    
}


public class Line : Singleton<Line>
{
    public List<ILineElement> line = new();    

    //public Spline spline;

    public int GetLineLength()
    {
        return line.Count;
    }

    /// <summary>
    /// Adds an already created LineElement to the line.
    /// </summary>
    /// <param name="element">The LineElement to add</param>
    /// <returns>Index of the new element in the line.</returns>
    public int AddLineElement(ILineElement element)
    {
        line.Add(element);
        if (line.Count > 1)
        {
            UIManager.Instance.GetSidebar().DeleteButtonEnabled = true;
        }
        return line.Count - 1;
        //var splineContainer = GetComponent<SplineContainer>();
        //spline.Add(splineContainer.transform.InverseTransformPoint(element.GetTransform().position));
    }    

    public Vector3 GetCurrentRideDirection()
    {
        if (line.Count == 0)
        {
            Debug.LogError("Ride directon request error: no line elements in the line.");
            return Vector3.forward;
        }

        return GetLastLineElement().GetRideDirection().normalized;
    }

    public void DestroyLastLineElement()
    {
        ILineElement lastElement = GetLastLineElement();

        lastElement.DestroyUnderlyingGameObject();

        if (line.Count == 1)
        {
            UIManager.Instance.GetSidebar().DeleteButtonEnabled = false;
        }
        //spline.RemoveAt(line.Count - 1);
    }

    /// <summary>
    /// Destroys all line elements from index onwards. 
    /// </summary>
    /// <param name="index">Index where the deletion starts</param>
    public void DestroyLineElementsFromIndex(int index)
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
