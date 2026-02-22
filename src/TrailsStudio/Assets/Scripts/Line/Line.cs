using Assets.Scripts.Builders;
using Assets.Scripts.Managers;
using Assets.Scripts.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;


[RequireComponent(typeof(SplineContainer))]
public class Line : Singleton<Line> , IReadOnlyCollection<ILineElement>, ISaveable<LineData>
{
    private List<ILineElement> line = new();    

    private Spline lineSpline;    

    public const string LINE_ELEMENT_TAG = "LineElement";

    public static RenderingLayerMask outlinedElementRenderLayerMask;

    public string Name { get; set; } = "New Line";

    public int Count => line.Count;

    private void Awake()
    {
        Name = MainMenuController.lineName;
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
            StudioUIManager.Instance.GetSidebar().DeleteButtonEnabled = true;
        }
        
        RebuildSpline();

        element.GetTransform().tag = LINE_ELEMENT_TAG;

        return line.Count - 1;
    } 

    public int GetLineElementIndex(ILineElement element)
    {
        int index = line.IndexOf(element);
        if (index < 0)
        {
            Debug.LogError("Element not found in the line.");
            return -1;
        }
        return index;
    }

    public ILineElement this[Index index] => line[index];

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

        SlopeChange slope = TerrainManager.Instance.ActiveSlope;
        bool slopeHasToBeDeletedFirst = slope != null && !slope.IsBuiltOn;
        
        if (line.Count <= 1 || slopeHasToBeDeletedFirst)
        {
            StudioUIManager.Instance.GetSidebar().DeleteButtonEnabled = false;
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

    public LineData GetSerializableData() => new LineData(this);

    public void LoadFromData(LineData data)
    {
        Name = data.name;

        RollIn rollin = line[0] as RollIn;

        line.Clear();

        rollin.LoadFromData(data.rollIn);

        AddLineElement(rollin);

        for (int i = 0; i < data.landings.Count; i++)
        {
            Takeoff takeoff = Instantiate(DataManager.Instance.takeoffPrefab).GetComponent<Takeoff>();
            takeoff.LoadFromData(data.takeoffs[i]);

            AddLineElement(takeoff);

            Landing landing = Instantiate(DataManager.Instance.landingPrefab).GetComponent<Landing>();
            landing.LoadFromData(data.landings[i]);

            AddLineElement(landing);
        }
    }

    /// <summary>
    /// Generates the textual representation object from the current line state.
    /// </summary>
    public LineTextInfo GenerateLineTextInfo()
    {
        LineTextInfo info = new LineTextInfo();

        // add rollin
        RollIn rollIn = GetRollIn();
        info.Items.Add(new LineTextInfo.RollInItem(rollIn.Angle, rollIn.GetHeight()));

        ILineElement previousElement = rollIn;
        List<SlopeChange> allSlopes = TerrainManager.Instance.slopeChanges;

        // iterate through line elements
        for (int i = 1; i < Count; i++)
        {
            ILineElement currentElement = this[i];

            // 1. Check for **single** slope STARTING after the previous element
            var startingSlope = allSlopes.FirstOrDefault(s => s.PreviousLineElement == previousElement);
            if (startingSlope != null)
            {
                float dist = Vector3.Distance(previousElement.GetEndPoint(), startingSlope.Start);
                info.Items.Add(new LineTextInfo.SlopeStartItem(dist, startingSlope.Angle * Mathf.Rad2Deg, startingSlope.HeightDifference, startingSlope.Length));
            }

            // 2. Add the current line element info
            if (currentElement is Takeoff takeoff)
            {
                float dist = Vector3.Distance(previousElement.GetEndPoint(), takeoff.GetStartPoint());

                // check if takeoff is on a slope
                float? slopeAngle = null;
                // We check if the takeoff is tilted relative to up vector to determine if it's on a slope
                float tiltAngle = Vector3.Angle(Vector3.ProjectOnPlane(takeoff.GetRideDirection(), Vector3.up), takeoff.GetRideDirection());
                if (tiltAngle > float.Epsilon)
                {
                     slopeAngle = tiltAngle;
                }

                info.Items.Add(new LineTextInfo.TakeoffItem(
                    previousElement, dist, takeoff.GetHeight(), takeoff.GetLength(), takeoff.GetWidth(),
                    takeoff.GetRadius(), takeoff.GetEndAngle() * Mathf.Rad2Deg,
                    Vector3.Distance(takeoff.PairedLanding.GetLandingPoint(), takeoff.GetTransitionEnd()), // Jump Length
                    slopeAngle
                ));
            }
            else if (currentElement is Landing landing)
            {
                Takeoff paired = (currentElement as Landing).PairedTakeoff;
                float jumpLength = Vector3.Distance(paired.GetTransitionEnd(), landing.GetLandingPoint());

                // calculate side shift
                Vector3 closestPointOnTakeoffLine = MathHelper.GetNearestPointOnLine(paired.GetTransitionEnd(), paired.GetRideDirection(), landing.GetLandingPoint());
                float shift = Vector3.Distance(closestPointOnTakeoffLine, landing.GetLandingPoint());

                // check if landing is titled
                float? slopeAngle = null;
                float tiltAngle = Vector3.Angle(Vector3.ProjectOnPlane(landing.GetRideDirection(), Vector3.up), landing.GetRideDirection());
                if (tiltAngle > 0.1f)
                {
                    slopeAngle = tiltAngle;
                }

                info.Items.Add(new LineTextInfo.LandingItem(
                    landing.GetSlopeAngle() * Mathf.Rad2Deg, landing.GetHeight(), landing.GetLength(), landing.GetWidth(),
                    jumpLength, landing.GetRotation(), shift, slopeAngle
                ));
            }

            // 3. Check for **single** Slope ENDING at this element
            // We use LastElementOnSlope to deterministically find where the slope "stops" being a slope
            var endingSlope = allSlopes.FirstOrDefault(s => s.LastElementOnSlope == currentElement);
            if (endingSlope != null)
            {
                float distToSlopeEnd = Vector3.Distance(previousElement.GetEndPoint(), currentElement.GetStartPoint());
                info.Items.Add(new LineTextInfo.SlopeEndItem(distToSlopeEnd));
            }

            previousElement = currentElement;
        }
        
        return info;
    }
}


