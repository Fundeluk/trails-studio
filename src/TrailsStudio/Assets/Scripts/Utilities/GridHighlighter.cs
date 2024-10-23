using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// TODO jak udelat grid highlighter, aby dokazal obepinat i komplikovanej teren? DECALS

[RequireComponent(typeof(LineRenderer))]
public class GridHighlighter : MonoBehaviour
{
    public Terrain terrain;
    public GameObject highlightPrefab;
    public LineRenderer lineRenderer;
    public GameObject distanceMeasure;

    private GameObject highlight;
    private Transform rollInTransform;

    private void Initialize()
    {
        highlight = Instantiate(highlightPrefab);
        highlight.SetActive(false);
        distanceMeasure.SetActive(true);
        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = Color.black;
        rollInTransform = Line.Instance.line[0].transform;
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


                // place the highlight a little above the terrain so that it does not clip through
                highlight.transform.position = new Vector3(hitPoint.x, hitPoint.y + 0.1f, hitPoint.z);

                float distance;            
                distance = Vector3.Distance(hitPoint, Line.Instance.currentLineEndPoint);

                distanceMeasure.transform.position = Vector3.Lerp(hitPoint, Line.Instance.currentLineEndPoint, 0.5f);
                // make the text go a bit higher so that it does not clip through the terrain
                distanceMeasure.transform.position += new Vector3(0, 0.1f, 0);

                // make the text face the camera and position it along the line
                Transform camTransform = CameraManager.Instance.GetTopDownCamTransform();
                distanceMeasure.transform.LookAt(camTransform, camTransform.up);
                distanceMeasure.transform.Rotate(0, 180, 0);

                distanceMeasure.GetComponent<TextMeshPro>().text = $"Distance: {distance:F2}m";

                // draw a line between the current line end point and the point where the mouse is pointing
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, Line.Instance.currentLineEndPoint);
                //lineRenderer.SetPosition(1, Line.Instance.currentLineEndPoint + (Vector3.Project(rollInTransform.forward, terrain.transform.right) * 50));
                lineRenderer.SetPosition(1, hitPoint);
                

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
