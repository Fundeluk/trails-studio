using Assets.Scripts;
using Assets.Scripts.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// TODO jak udelat grid highlighter, aby dokazal obepinat i komplikovanej teren? DECALS

[RequireComponent(typeof(LineRenderer))]
public class GridHighlighter : Singleton<GridHighlighter>
{
    public GameObject highlight;
    public LineRenderer lineRenderer;
    public GameObject distanceMeasure;

    public Vector3? desiredTakeOffPosition = null;

    // the minimum and maximum distances between the last line element and new obstacle
    [Header("Build bounds")]
    [Tooltip("The minimum distance between the last line element and the new obstacle.")]
    public float MIN_BUILD_DISTANCE = 1;
    [Tooltip("The maximum distance between the last line element and the new obstacle.")]
    public float MAX_BUILD_DISTANCE = 30;

    private LineElement lastLineElement;
    private bool validHighlightPosition = false;

    private void Initialize()
    {
        lastLineElement = Line.Instance.line[^1];

        
        // if highlight is not positioned somewhere in front of the last line element, move it there
        if ((highlight.transform.position - lastLineElement.endPoint).normalized != lastLineElement.rideDirection.normalized)
        {
            highlight.transform.position = lastLineElement.endPoint + lastLineElement.rideDirection.normalized;
        }

        highlight.SetActive(false);

        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = Color.black;

        distanceMeasure.SetActive(false);

        InputSystem.actions.FindAction("Click").performed += OnHighlightClicked;
    }

    // Start is called before the first frame update
    void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void OnHighlightClicked(InputAction.CallbackContext context)
    {
        if (validHighlightPosition)
        {
            desiredTakeOffPosition = highlight.transform.position;
            StateController.Instance.ChangeState(StateController.takeOffBuildState);
        }
    }

    /// <summary>
    /// Moves the highlight, its distance text and the line renderer to the point where the mouse is pointing on the terrain.
    /// </summary>
    /// <param name="hit">The hitpoint on the terrain where the mouse points</param>
    /// <returns>True if the move destination is valid</returns>
    private bool MoveHighlightToProjectedHitPoint(RaycastHit hit)
    {
        // if the mouse is pointing at the terrain
        if (hit.collider.gameObject.TryGetComponent<Terrain>(out var _))
        {
            Vector3 hitPoint = hit.point;

            // project the hit point on a line that goes from the last line element position in the direction of riding
            Vector3 projectedHitPoint = Vector3.Project(hitPoint - lastLineElement.endPoint, lastLineElement.rideDirection) + lastLineElement.endPoint;

            // if the projected point is not in front of the last line element, return
            if ((projectedHitPoint - lastLineElement.endPoint).normalized != lastLineElement.rideDirection.normalized)
            {
                return false;
            }

            // if the projected point is too close to the last line element or too far from it, return
            if ((projectedHitPoint - lastLineElement.endPoint).magnitude < MIN_BUILD_DISTANCE ||
                (projectedHitPoint - lastLineElement.endPoint).magnitude > MAX_BUILD_DISTANCE)
            {
                return false;
            }



            // place the highlight a little above the terrain so that it does not clip through
            highlight.transform.position = new Vector3(projectedHitPoint.x, projectedHitPoint.y + 0.1f, projectedHitPoint.z);

            float distance = Vector3.Distance(projectedHitPoint, lastLineElement.endPoint);

            // position the text in the middle of the screen
            distanceMeasure.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, Camera.main.transform.position.y));

            // make the text go along the line
            distanceMeasure.transform.right = CameraManager.Instance.GetTopDownCamTransform().right;

            // make the text lay flat on the terrain
            distanceMeasure.transform.Rotate(90, 0, 0);

            distanceMeasure.GetComponent<TextMeshPro>().text = $"Distance: {distance:F2}m";

            // draw a line between the current line end point and the point where the mouse is pointing
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, lastLineElement.endPoint);
            lineRenderer.SetPosition(1, projectedHitPoint);

            return true;

        }
        else { return false; }
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            validHighlightPosition = MoveHighlightToProjectedHitPoint(hit);

            if (!highlight.activeSelf)
            {
                highlight.SetActive(true);
            }

            if (!distanceMeasure.activeSelf)
            {
                distanceMeasure.SetActive(true);
            }

        }
    }

    public GameObject GetHighlight()
    {
        return highlight;
    }

    private void OnDisable()
    {
        highlight.SetActive(false);
        distanceMeasure.SetActive(false);
        InputSystem.actions.FindAction("Select").performed -= OnHighlightClicked;
    }
}
