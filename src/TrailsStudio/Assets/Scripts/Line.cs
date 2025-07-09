using Assets.Scripts.Builders;
using Assets.Scripts.Managers;
using Assets.Scripts.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using Unity.Cinemachine;

public interface ILineElement
{
    public int GetIndex();
    public Transform GetTransform();

    public Vector3 GetStartPoint();

    public GameObject GetCameraTarget();

    public HeightmapCoordinates GetObstacleHeightmapCoordinates();

    public float GetHeight();

    public float GetLength();

    public Vector3 GetEndPoint();

    public Vector3 GetRideDirection();

    public float GetWidth();

    public HeightmapCoordinates GetUnderlyingSlopeHeightmapCoordinates();

    public void SetSlopeChange(SlopeChange slope);

    public SlopeChange GetSlopeChange();

    public float GetPreviousElementBottomWidth();

    /// <summary>
    /// Returns the Width of the line element at its bottom level.
    /// </summary>
    public float GetBottomWidth();
    public List<(string name, string value)> GetLineElementInfo();

    public void Outline();

    public void RemoveOutline();

    public void OnTooltipShow();

    public void OnTooltipClosed();

    public void DestroyUnderlyingGameObject();

    /// <returns>The speed at which a rider exits the line element in meters per second.</returns>
    public float GetExitSpeed();
    
}


[RequireComponent(typeof(SplineContainer))]
public class Line : Singleton<Line> , IReadOnlyCollection<ILineElement>
{
    private List<ILineElement> line = new();    

    private Spline lineSpline;

    public static float MIN_EXIT_SPEED_MS = PhysicsManager.KmhToMs(15f); // 15 km/h 

    public static float MAX_EXIT_SPEED_MS = PhysicsManager.KmhToMs(70f); // 70 km/h

    public const string LINE_ELEMENT_TAG = "LineElement";

    public static RenderingLayerMask outlinedElementRenderLayerMask;

    public int Count => ((IReadOnlyCollection<ILineElement>)line).Count;

    private void Awake()
    {
        lineSpline = GetComponent<SplineContainer>().AddSpline();
        RebuildSpline();
        outlinedElementRenderLayerMask = RenderingLayerMask.GetMask("Default", "OutlineObject");
    }

    public RollIn GetRollIn()
    {
        if (line.Count == 0 || line[0] is not RollIn rollin)
        {
            Debug.LogError("No roll-in found in the line.");
            return null;
        }
        return rollin;
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
        
        RebuildSpline();

        element.GetTransform().tag = LINE_ELEMENT_TAG;

        return line.Count - 1;
    } 
    
    public ILineElement this[Index index]
    {
        get => line[index];
    }

    public Vector3 GetCurrentRideDirection()
    {
        if (line.Count == 0)
        {
            throw new System.InvalidOperationException("No line elements in the line.");
        }

        return GetLastLineElement().GetRideDirection().normalized;
    }

    public void RemoveLastLineElement()
    {
        if (line.Count == 0)
        {
            Debug.LogError("No line elements to remove.");
            return;
        }

        line.RemoveAt(line.Count - 1);

        if (line.Count <= 1)
        {
            UIManager.Instance.GetSidebar().DeleteButtonEnabled = false;
        }

        RebuildSpline();
    }

    /// <summary>
    /// Destroys all line elements from index onwards. 
    /// </summary>
    /// <param name="index">Index where the deletion starts</param>
    public void DestroyLineElementsFromIndex(int index)
    {
        if (index >= line.Count || index <= 0) //0 is roll in; dont allow that
        {
            throw new IndexOutOfRangeException("Index out of bounds.");
        }

        int count = line.Count - index;

        for (int i = 0; i < count; i++)
        {
            ILineElement element = line[^1];
            RemoveLastLineElement();
            element.DestroyUnderlyingGameObject();
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

    /// <summary>
    /// Tries to get a component derived from <see cref="ILineElement"/> on a GameObject.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="gameObject"/> argument is null.</exception>
    public static bool TryGetLineElementFromGameObject(GameObject gameObject, out ILineElement element)
    {
        if (gameObject == null)
        {
            throw new ArgumentNullException(nameof(gameObject));
        }

        ILineElement lineElement = gameObject.GetComponent<ILineElement>();

        // special case for roll-in where its individual parts with colliders are its children
        lineElement ??= gameObject.GetComponentInParent<ILineElement>();

        if (lineElement != null)
        {
            element = lineElement;
            return true;
        }
        else
        {
            element = null;
            return false;
        }
    }

    void RebuildSpline()
    {
        lineSpline.Clear();

        if (line.Count == 0)
        {
            return;
        }

        List<BezierKnot> knots = new();

        foreach (ILineElement element in line)
        {
            Vector3 position = element.GetTransform().position + (element.GetHeight() + 10) * Vector3.up;

            if (element == line[0])
            {
                AddKnot(position - 1.5f * element.GetLength() * element.GetRideDirection(), knots);
            }

            AddKnot(position, knots);           

            if (element == line[^1])
            {
                AddKnot(position + 1.5f * element.GetLength() * element.GetRideDirection(), knots);
            }

            lineSpline.Knots = knots;
        }
    }

    private void AddKnot(Vector3 position, List<BezierKnot> knots)
    {
        var container = GetComponent<SplineContainer>();

        Vector3 localPosition = container.transform.InverseTransformPoint(position);
        BezierKnot knot = new(localPosition);

        knots.Add(knot);
    }

    public IEnumerator<ILineElement> GetEnumerator()
    {
        return ((IEnumerable<ILineElement>)line).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)line).GetEnumerator();
    }
}
