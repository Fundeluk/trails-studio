using Assets.Scripts.Builders;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TakeoffMeshGenerator : MonoBehaviour
{
    public class Takeoff : ILineElement
    {
        private readonly TakeoffMeshGenerator meshGenerator;

        private readonly GameObject cameraTarget;

        private LandingMeshGenerator.Landing landing = null;

        private readonly GameObject pathProjector;

        private readonly ILineElement previousLineElement;

        private void UpdatePathProjector()
        {
            Vector3 takeoffStart = GetTransform().position - GetRideDirection().normalized * meshGenerator.CalculateRadiusLength();

            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, GetRideDirection());
            pathProjector.transform.SetPositionAndRotation(Vector3.Lerp(previousLineElement.GetEndPoint(), takeoffStart, 0.5f) + Vector3.up, rotation);

            float distance = Vector3.Distance(previousLineElement.GetEndPoint(), takeoffStart);
            float width = Mathf.Lerp(previousLineElement.GetWidth(), GetWidth() + 2 * GetHeight() * 0.2f, 0.5f);

            DecalProjector decalProjector = pathProjector.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(width, distance, 10);
        }


        private void RecalculateCameraTargetPosition()
        {
            cameraTarget.transform.position = GetTransform().position + (0.5f * GetHeight() * GetTransform().up);
        }

        public Takeoff(TakeoffMeshGenerator meshGenerator)
        {
            this.meshGenerator = meshGenerator;
            cameraTarget = new GameObject("Camera Target");
            cameraTarget.transform.SetParent(meshGenerator.transform);
            RecalculateCameraTargetPosition();

            previousLineElement = Line.Instance.line[^1];

            pathProjector = Instantiate(Line.Instance.pathProjectorPrefab);
            pathProjector.transform.SetParent(meshGenerator.transform);

            UpdatePathProjector();
        }

        public Vector3 GetEndPoint() => GetTransform().position + GetRideDirection().normalized * (meshGenerator.thickness + GetHeight() * TakeoffMeshGenerator.sideSlope);

        public float GetHeight() => meshGenerator.height;

        public float GetLength() => meshGenerator.CalculateRadiusLength() + meshGenerator.thickness + GetHeight() * sideSlope;

        public Vector3 GetRideDirection() => meshGenerator.transform.forward;

        public Transform GetTransform() => meshGenerator.transform;

        public GameObject GetCameraTarget() => cameraTarget;

        public void SetEndPoint(Vector3 endPoint)
        {
            throw new System.InvalidOperationException("Cannot set end point of takeoff.");
        }

        public void SetHeight(float height)
        {
            meshGenerator.height = height;
            meshGenerator.GenerateTakeoffMesh();
            RecalculateCameraTargetPosition();
            UpdatePathProjector();
        }

        public void SetLanding(LandingMeshGenerator.Landing landing)
        {
            this.landing = landing;
        }

        public void SetLength(float length)
        {
            // TODO it may make sense in the future to edit the takeoff by changing supposed length
            throw new System.InvalidOperationException("Cannot set length of takeoff.");
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            meshGenerator.transform.forward = rideDirection;
        }

        public float GetRadius() => meshGenerator.radius;

        public void SetRadius(float radius)
        {
            meshGenerator.radius = radius;
            meshGenerator.GenerateTakeoffMesh();
            UpdatePathProjector();
        }

        public float GetWidth() => meshGenerator.width;

        public void SetWidth(float width)
        {
            meshGenerator.width = width;
            meshGenerator.GenerateTakeoffMesh();
            UpdatePathProjector();
        }

        public float GetThickness() => meshGenerator.thickness;

        public void SetThickness(float thickness)
        {
            meshGenerator.thickness = thickness;
            meshGenerator.GenerateTakeoffMesh();
            RecalculateCameraTargetPosition();
        }

        public void DestroyUnderlyingGameObject()
        {
            landing?.DestroyUnderlyingGameObject();
            Destroy(pathProjector);
            Destroy(cameraTarget);
            Destroy(meshGenerator.gameObject);
        }
    }

    public float height;
    public float width;
    public float thickness;
    public float radius;

    public int resolution; // Number of segments along the curve

    private float radiusLength;

    private const float sideSlope = 0.2f;

    // instance-wide indices for the corners
    private int leftFrontBottomCornerIndex;
    private int rightFrontBottomCornerIndex;

    private int leftRearBottomCornerIndex;
    private int rightRearBottomCornerIndex;

    private int leftRearUpperCornerIndex;
    private int rightRearUpperCornerIndex;

    private int leftFrontUpperCornerIndex;
    private int rightFrontUpperCornerIndex;

    void Start()
    {
        GenerateTakeoffMesh();
    }

    /// <summary>
    /// Get the angle at which the takeoff's curve ends.
    /// </summary>
    /// <param name="radius">Radius of the circle that defines the takeoff's curve</param>
    /// <param name="height">Height of the takeoff</param>
    /// <returns>The angle</returns>
    public static float GetEndAngle(float radius, float height)
    {
        float betaAngle = Mathf.Asin((radius - height) / radius);
        float alphaAngle = 90 * Mathf.Deg2Rad - betaAngle;
        return alphaAngle;
    }

    Vector3[] CreateVertices(float angleStart, float angleEnd)
    {
        // make the bottom corners wider than the top
        float bottomCornerWidth = width + 2 * height * sideSlope;
        float bottomCornerOffset = height * sideSlope;

        // Generate points for the takeoff's curve + corners
        Vector3[] leftFrontArc = new Vector3[resolution + 1];
        Vector3 leftFrontBottomCorner = new(-bottomCornerWidth / 2, 0, 0);
        Vector3 leftRearBottomCorner = new(-bottomCornerWidth / 2, 0, thickness + bottomCornerOffset);
        Vector3 leftRearUpperCorner = new(-width / 2, height, thickness);

        Vector3[] rightFrontArc = new Vector3[resolution + 1];
        Vector3 rightFrontBottomCorner = new(bottomCornerWidth / 2, 0, 0);
        Vector3 rightRearBottomCorner = new(bottomCornerWidth / 2, 0, thickness + bottomCornerOffset);
        Vector3 rightRearUpperCorner = new(width / 2, height, thickness);


        for (int i = 0; i <= resolution; i++)
        {
            float t = (float)i / resolution;
            float angle = Mathf.Lerp(angleStart, angleEnd, t);
            float lengthwise = -radiusLength + Mathf.Cos(angle) * radius;
            float heightwise = Mathf.Sin(angle) * radius + radius;
            leftFrontArc[i] = new Vector3(-(bottomCornerWidth / 2 - heightwise * sideSlope), heightwise, lengthwise);
            rightFrontArc[i] = new Vector3(bottomCornerWidth / 2 - heightwise * sideSlope, heightwise, lengthwise);
        }

        // Combine vertices into one array
        Vector3[] vertices = new Vector3[(resolution + 1) * 2 + 6];

        for (int i = 0; i <= resolution; i++)
        {
            vertices[i] = leftFrontArc[i];
            vertices[i + resolution + 1] = rightFrontArc[i];
        }

        // add front bottom corners
        leftFrontBottomCornerIndex = resolution * 2 + 2;
        rightFrontBottomCornerIndex = resolution * 2 + 3;
        vertices[leftFrontBottomCornerIndex] = leftFrontBottomCorner;
        vertices[rightFrontBottomCornerIndex] = rightFrontBottomCorner;

        // add rear bottom corners
        leftRearBottomCornerIndex = resolution * 2 + 4;
        rightRearBottomCornerIndex = resolution * 2 + 5;
        vertices[leftRearBottomCornerIndex] = leftRearBottomCorner;
        vertices[rightRearBottomCornerIndex] = rightRearBottomCorner;

        // add rear upper corners
        leftRearUpperCornerIndex = resolution * 2 + 6;
        rightRearUpperCornerIndex = resolution * 2 + 7;
        vertices[leftRearUpperCornerIndex] = leftRearUpperCorner;
        vertices[rightRearUpperCornerIndex] = rightRearUpperCorner;

        leftFrontUpperCornerIndex = resolution;
        rightFrontUpperCornerIndex = 2 * resolution + 1;

        return vertices;
    }

    float CalculateRadiusLength()
    {
        // the angles are calculated from scratch here,
        // because Takeoff class may call this at times where the angles are not available yet
        float angleStart = 270 * Mathf.Deg2Rad;
        float angleEnd = angleStart + GetEndAngle(radius, height);

        return Mathf.Cos(angleEnd) * radius;
    }

    int[] CreateTriangles()
    {
        // Generate triangles
        int triangleCount = 2 * resolution + 2 * resolution + 8;
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
            triangles[triIndex++] = rightIndex + 1;
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
        triangles[triIndex++] = leftFrontUpperCornerIndex;
        triangles[triIndex++] = leftFrontBottomCornerIndex;

        // create right side thickness triangles
        triangles[triIndex++] = rightFrontBottomCornerIndex;
        triangles[triIndex++] = rightRearUpperCornerIndex;
        triangles[triIndex++] = rightRearBottomCornerIndex;

        triangles[triIndex++] = rightFrontUpperCornerIndex;
        triangles[triIndex++] = rightRearUpperCornerIndex;
        triangles[triIndex++] = rightFrontBottomCornerIndex;

        // create upper flat side triangles
        triangles[triIndex++] = rightRearUpperCornerIndex;
        triangles[triIndex++] = rightFrontUpperCornerIndex;
        triangles[triIndex++] = leftRearUpperCornerIndex;

        triangles[triIndex++] = rightFrontUpperCornerIndex;
        triangles[triIndex++] = leftFrontUpperCornerIndex;
        triangles[triIndex++] = leftRearUpperCornerIndex;


        // create back side triangles
        triangles[triIndex++] = rightRearBottomCornerIndex;
        triangles[triIndex++] = leftRearUpperCornerIndex;
        triangles[triIndex++] = leftRearBottomCornerIndex;

        triangles[triIndex++] = rightRearBottomCornerIndex;
        triangles[triIndex++] = rightRearUpperCornerIndex;
        triangles[triIndex++] = leftRearUpperCornerIndex;

        return triangles;
    }
    
    public void GenerateTakeoffMesh()
    {
        Mesh mesh = new();
        GetComponent<MeshFilter>().mesh = mesh;

        float angleStart = 270 * Mathf.Deg2Rad;
        float angleEnd = angleStart + GetEndAngle(radius,height);
       
        radiusLength = CalculateRadiusLength();
        Debug.Log("Length in mesh generation method: " + radiusLength);

        Vector3[] vertices = CreateVertices(angleStart, angleEnd);

        int[] triangles = CreateTriangles();

        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
