using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// ZEPTAT SE
// jak udelat grid highlighter, aby dokazal obepinat i komplikovanej teren? DECALS

[RequireComponent(typeof(LineRenderer))]
public class GridHighlighter : MonoBehaviour
{
    public Terrain terrain;
    public GameObject highlightPrefab;
    public LineRenderer lineRenderer;
    public GameObject distanceMeasure;


    private GameObject highlight;

    // Start is called before the first frame update
    void Start()
    {
        highlight = Instantiate(highlightPrefab);
        highlight.SetActive(false);
        distanceMeasure.SetActive(true);
        
    }

    private void OnEnable()
    {
        highlight = Instantiate(highlightPrefab);
        highlight.SetActive(false);
        distanceMeasure.SetActive(true);
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

                // TODO rotate so that it is along the line, not across the line
                distanceMeasure.transform.rotation = Quaternion.FromToRotation(Vector3.up, (hitPoint - Line.Instance.currentLineEndPoint).normalized);

                distanceMeasure.GetComponent<TextMeshPro>().text = $"Distance: {distance:F2}m";
                
                // TODO solve the material problem!!
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, Line.Instance.currentLineEndPoint);
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
