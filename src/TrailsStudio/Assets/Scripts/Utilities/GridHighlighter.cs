using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// TODO jak udelat grid highlighter, aby dokazal obepinat i komplikovanej teren? DECALS
// TODO move the highlight only on a straight line from the previous line end point
// find the angle between the mouse vector and the forward vector of the roll in transform and use sine and cosine to find the x and z coordinates of the point where the mouse is pointing

[RequireComponent(typeof(LineRenderer))]
public class GridHighlighter : MonoBehaviour
{
    public Terrain terrain;
    public GameObject highlightPrefab;
    public LineRenderer lineRenderer;
    public GameObject distanceMeasure;

    private GameObject highlight;

    private Vector3 lastLineElementPosition;
    private Vector3 rideDirection;

    private void Initialize()
    {
        highlight = Instantiate(highlightPrefab);
        highlight.SetActive(false);
        distanceMeasure.SetActive(true);
        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = Color.black;

        lastLineElementPosition = Line.Instance.currentLineEndPoint;
        rideDirection = Line.Instance.currentRideDirection;

        Debug.DrawRay(lastLineElementPosition, rideDirection*500, Color.red, 500, false);
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

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == terrain.gameObject)
            {
                Vector3 hitPoint = hit.point;

                // project the hit point on a line that goes from the last line element position in the direction of riding
                Vector3 projectedHitPoint = Vector3.Project(hitPoint - lastLineElementPosition, rideDirection) + lastLineElementPosition;

                // place the highlight a little above the terrain so that it does not clip through
                highlight.transform.position = new Vector3(projectedHitPoint.x, projectedHitPoint.y + 0.1f, projectedHitPoint.z);

                float distance = Vector3.Distance(projectedHitPoint,lastLineElementPosition);

                // position the text in the middle of the line
                distanceMeasure.transform.position = Vector3.Lerp(projectedHitPoint,lastLineElementPosition, 0.5f);

                // make the text go along the line
                distanceMeasure.transform.right = Vector3.ProjectOnPlane(projectedHitPoint - lastLineElementPosition, Vector3.up);
                // make the text lay flat on the terrain
                distanceMeasure.transform.Rotate(90, 0, 0);

                distanceMeasure.GetComponent<TextMeshPro>().text = $"Distance: {distance:F2}m";

                // draw a line between the current line end point and the point where the mouse is pointing
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, lastLineElementPosition);
                lineRenderer.SetPosition(1, projectedHitPoint);
            }

            if (!highlight.activeSelf)
            {
                highlight.SetActive(true);
            }

            if (distanceMeasure.activeSelf)
            {
                distanceMeasure.SetActive(true);
            }

        }
    }

    private void OnDisable()
    {
        if (highlight != null)
        {
            Destroy(highlight);
        }
        distanceMeasure.SetActive(false);
    }
}
