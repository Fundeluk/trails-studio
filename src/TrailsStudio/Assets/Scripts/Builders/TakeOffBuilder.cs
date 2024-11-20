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

    static float GetEndAngle(float radius, float height)
    {
        float betaAngle = Mathf.Asin((radius - height) / radius);
        Debug.Log("Beta angle: " + betaAngle * Mathf.Rad2Deg);
        float alphaAngle = 90 * Mathf.Deg2Rad - betaAngle;
        Debug.Log("Alpha angle: " + alphaAngle * Mathf.Rad2Deg);
        return alphaAngle;
    }

    public void GenerateTakeoffMesh()
    {
        Mesh mesh = new();
        GetComponent<MeshFilter>().mesh = mesh;

        // Convert angle range to radians
        float angleStart = 270 * Mathf.Deg2Rad;
        float angleEnd = angleStart + GetEndAngle(radius,height);
        //Debug.Log("Start angle: " + angleStart + "rad. End angle: " + angleEnd + "rad.");
        //Debug.Log("Height at supposed end angle: " + (Mathf.Sin(angleEnd) * radius + radius));
        //Debug.Log("Length at start angle: " + Mathf.Cos(angleStart) * radius + " at end angle: " + Mathf.Cos(angleEnd) * radius);

        length = Mathf.Cos(angleEnd) * radius;

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
            //Debug.Log("angle: " + angle + " length: " + lengthwise + " height: " + heightwise);
            //Debug.DrawLine(transform.position, transform.position + new Vector3(0, heightwise, 0), Color.red, 20f);
            //Debug.DrawLine(transform.position + new Vector3(0, radius, 0), transform.position + new Vector3(0, heightwise, lengthwise), Color.green, 20f);
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

        int leftBottomCornerIndex = resolution * 2 + 2;
        int rightBottomCornerIndex = resolution * 2 + 3;
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
        triangles[triIndex++] = 2*resolution + 1;
        triangles[triIndex++] = resolution;

        triangles[triIndex++] = leftBottomCornerIndex;
        triangles[triIndex++] = rightBottomCornerIndex;
        triangles[triIndex++] = 2 * resolution + 1;

        // TODO height is wrong
        for (int i = 2*resolution; i < 2*resolution + 4; i++)
        {
            Debug.Log("vertex " + i + ": " + vertices[i]);
        }


        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
