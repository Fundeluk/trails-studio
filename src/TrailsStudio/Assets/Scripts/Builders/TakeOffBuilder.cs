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

    private const float sideSlope = 1.5f;

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

        // make the bottom corners wider than the top
        float bottomCornerWidth = width * sideSlope;
        float bottomCornerOffset = thickness * sideSlope;

        // Generate points for the takeoff's curve + corners
        Vector3[] leftFrontArc = new Vector3[resolution + 1];
        Vector3 leftFrontBottomCorner = new(-bottomCornerWidth / 2, 0, length);
        Vector3 leftRearBottomCorner  = new(-bottomCornerWidth / 2, 0, length + bottomCornerOffset);
        Vector3 leftRearUpperCorner = new(-width / 2, height, length + thickness);

        Vector3[] rightFrontArc = new Vector3[resolution + 1];
        Vector3 rightFrontBottomCorner = new(bottomCornerWidth / 2, 0, length);
        Vector3 rightRearBottomCorner = new(bottomCornerWidth / 2, 0, length + bottomCornerOffset);
        Vector3 rightRearUpperCorner = new(width / 2, height, length + thickness);


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
        Vector3[] vertices = new Vector3[(resolution + 1) * 2 + 6];
        
        for (int i = 0; i <= resolution; i++)
        {
            vertices[i] = leftFrontArc[i];
            vertices[i + resolution + 1] = rightFrontArc[i];
        }

        // add front bottom corners
        int leftFrontBottomCornerIndex = resolution * 2 + 2;
        int rightFrontBottomCornerIndex = resolution * 2 + 3;
        vertices[leftFrontBottomCornerIndex] = leftFrontBottomCorner;
        vertices[rightFrontBottomCornerIndex] = rightFrontBottomCorner;

        // add rear bottom corners
        int leftRearBottomCornerIndex = resolution * 2 + 4;
        int rightRearBottomCornerIndex = resolution * 2 + 5;
        vertices[leftRearBottomCornerIndex] = leftRearBottomCorner;
        vertices[rightRearBottomCornerIndex] = rightRearBottomCorner;

        // add rear upper corners
        int leftRearUpperCornerIndex = resolution * 2 + 6;
        int rightRearUpperCornerIndex = resolution * 2 + 7;
        vertices[leftRearUpperCornerIndex] = leftRearUpperCorner;
        vertices[rightRearUpperCornerIndex] = rightRearUpperCorner;


        // Generate triangles
        int triangleCount = 2*resolution + 2*resolution + 8;
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
            triangles[triIndex++] = leftFrontBottomCornerIndex;
            triangles[triIndex++] = leftIndex + 1;
            triangles[triIndex++] = leftIndex;
            //right
            triangles[triIndex++] = rightIndex;
            triangles[triIndex++] = rightIndex + 1;
            triangles[triIndex++] = rightFrontBottomCornerIndex;
        }

        // create left side thickness triangles
        triangles[triIndex++] = leftRearBottomCornerIndex;
        triangles[triIndex++] = leftRearUpperCornerIndex;
        triangles[triIndex++] = leftFrontBottomCornerIndex;

        triangles[triIndex++] = leftRearUpperCornerIndex;
        triangles[triIndex++] = resolution;
        triangles[triIndex++] = leftFrontBottomCornerIndex;

        // create right side thickness triangles
        triangles[triIndex++] = rightFrontBottomCornerIndex;
        triangles[triIndex++] = rightRearUpperCornerIndex;
        triangles[triIndex++] = rightRearBottomCornerIndex;

        triangles[triIndex++] = 2 * resolution + 1;
        triangles[triIndex++] = rightRearUpperCornerIndex;
        triangles[triIndex++] = rightFrontBottomCornerIndex;

        // create upper flat side triangles
        triangles[triIndex++] = rightRearUpperCornerIndex;
        triangles[triIndex++] = 2 * resolution + 1;
        triangles[triIndex++] = leftRearUpperCornerIndex;

        triangles[triIndex++] = 2 * resolution + 1;
        triangles[triIndex++] = resolution;
        triangles[triIndex++] = leftRearUpperCornerIndex;


        // create back side triangles
        triangles[triIndex++] = rightRearBottomCornerIndex;
        triangles[triIndex++] = leftRearUpperCornerIndex;
        triangles[triIndex++] = leftRearBottomCornerIndex;

        triangles[triIndex++] = rightRearBottomCornerIndex;
        triangles[triIndex++] = rightRearUpperCornerIndex;
        triangles[triIndex++] = leftRearUpperCornerIndex;

        //for (int i = 2*resolution; i < 2*resolution + 4; i++)
        //{
        //    Debug.Log("vertex " + i + ": " + vertices[i]);
        //}


        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
