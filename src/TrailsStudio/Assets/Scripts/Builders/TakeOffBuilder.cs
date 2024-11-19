using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TakeoffMeshGenerator : MonoBehaviour
{
    public float height = 2f;
    public float width = 2f;
    public float thickness = 0.5f;

    public float radius = 5f;

    public int resolution = 10; // Number of segments along the curve

    private float length;

    void Start()
    {
        GenerateTakeoffMesh();
    }

    public void GenerateTakeoffMesh()
    {
        Mesh mesh = new();
        GetComponent<MeshFilter>().mesh = mesh;

        // Convert angle range to radians
        float angleStart = 270 * Mathf.Deg2Rad;
        float angleEnd = angleStart - Mathf.Asin((height - radius) / radius);
        Debug.Log("End angle: " + angleEnd);

        length = Mathf.Cos(angleEnd) * radius;
        Debug.Log("Length: " + length);

        // Generate points for the takeoff's curve + corners
        Vector3[] leftFrontArc = new Vector3[resolution + 1];
        Vector3 leftCorner = new(-width / 2, 0, length);

        Vector3[] rightFrontArc = new Vector3[resolution + 1];
        Vector3 rightCorner = new(width / 2, 0, length);

        for (int i = 0; i <= resolution; i++)
        {
            float t = (float)i / resolution;
            float angle = Mathf.Lerp(angleStart, angleEnd, t);
            float lengthwise = Mathf.Cos(angle) * radius;
            float heightwise = Mathf.Sin(angle) * radius + radius;
            leftFrontArc[i] = new Vector3(-width/2, heightwise, lengthwise);
            rightFrontArc[i] = new Vector3(width / 2, heightwise, lengthwise);            
        }

        // Combine vertices into one array
        Vector3[] vertices = new Vector3[(resolution + 1) * 2 + 2];
        
        for (int i = 0; i <= resolution; i++)
        {
            vertices[i] = leftFrontArc[i];
            vertices[i + resolution + 1] = rightFrontArc[i];
        }

        int leftBottomCornerIndex = resolution * 2 + 1;
        int rightBottomCornerIndex = resolution * 2 + 2;
        vertices[leftBottomCornerIndex] = leftCorner;
        vertices[rightBottomCornerIndex] = rightCorner;


        // Generate triangles
        int triangleCount = 2*resolution + 2*resolution + 2;
        int triangleIndexCount = triangleCount * 3;
        int[] triangles = new int[triangleIndexCount];
        int triIndex = 0;

        // Connect front face quads
        for (int i = 0; i < resolution; i++)
        {
            int leftIndex = i;
            int rightIndex = i + resolution + 1;

            // create triangle pointing left
            triangles[triIndex++] = leftIndex;
            triangles[triIndex++] = rightIndex+1;
            triangles[triIndex++] = rightIndex;

            // create triangle pointing right
            triangles[triIndex++] = leftIndex;
            triangles[triIndex++] = leftIndex + 1;
            triangles[triIndex++] = rightIndex + 1;

            //create side triangles
            //left
            triangles[triIndex++] = leftBottomCornerIndex;
            triangles[triIndex++] = leftIndex + 1;
            triangles[triIndex++] = leftIndex;
            //right
            triangles[triIndex++] = rightIndex;
            triangles[triIndex++] = rightIndex + 1;
            triangles[triIndex++] = rightBottomCornerIndex;
        }

        // create back side triangles
        triangles[triIndex++] = leftBottomCornerIndex;
        triangles[triIndex++] = 2*resolution;
        triangles[triIndex++] = resolution;

        triangles[triIndex++] = leftBottomCornerIndex;
        triangles[triIndex++] = rightBottomCornerIndex;
        triangles[triIndex++] = 2 * resolution;


        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
