using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Assets.Scripts.Builders
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class LandingMeshGenerator : MonoBehaviour
    {
        public class Landing : ILineElement
        {
            private readonly LandingMeshGenerator meshGenerator;
            private readonly GameObject cameraTarget;
            private readonly TakeoffMeshGenerator.Takeoff takeoff;            

            public Landing(LandingMeshGenerator meshGenerator, TakeoffMeshGenerator.Takeoff takeoff)
            {
                this.meshGenerator = meshGenerator;
                this.takeoff = takeoff;
                cameraTarget = new GameObject("Camera Target");
                cameraTarget.transform.SetParent(meshGenerator.transform);
                RecalculateCameraTargetPosition();
            }

            private void RecalculateCameraTargetPosition()
            {
                cameraTarget.transform.position = GetTransform().position + (0.5f * GetHeight() * GetTransform().up);
            }

            public void DestroyUnderlyingGameObject()
            {
                takeoff.SetLanding(null);
                Destroy(cameraTarget);
                Destroy(meshGenerator.gameObject);
            }

            public GameObject GetCameraTarget() => cameraTarget;

            public Vector3 GetEndPoint() => meshGenerator.transform.position + (meshGenerator.CalculateRadiusLength(meshGenerator.CalculateRadius()) + meshGenerator.CalculateSlopeLength()) * meshGenerator.transform.forward;

            public float GetHeight() => meshGenerator.height;

            public float GetLength() => meshGenerator.CalculateLength();

            public float GetWidth() => meshGenerator.width;

            public float GetThickness() => meshGenerator.thickness;

            public Vector3 GetRideDirection() => meshGenerator.transform.forward;

            public Transform GetTransform() => meshGenerator.transform;

            public int GetRotation() => (int)Vector3.SignedAngle(takeoff.GetRideDirection().normalized, meshGenerator.transform.forward, Vector3.up);

            public void SetHeight(float height)
            {
                meshGenerator.height = height;
                meshGenerator.GenerateLandingMesh();
                RecalculateCameraTargetPosition();
            }

            public void SetLength(float length)
            {
                throw new System.InvalidOperationException("Cannot set length of landing.");
            }

            public void SetRideDirection(Vector3 rideDirection)
            {
                meshGenerator.transform.forward = rideDirection;
            }

            public void SetWidth(float width)
            {
                meshGenerator.width = width;
                meshGenerator.GenerateLandingMesh();
            }

            public void SetThickness(float thickness)
            {
                meshGenerator.thickness = thickness;
                meshGenerator.GenerateLandingMesh();
                RecalculateCameraTargetPosition();
            }

            public void SetSlope(float slope)
            {
                meshGenerator.slope = slope * Mathf.Deg2Rad;
                meshGenerator.GenerateLandingMesh();
                RecalculateCameraTargetPosition();
            }

            /// <summary>
            /// Rotates the landing around the y-axis. Negative values rotate to  riders left, positive to riders right.
            /// </summary>
            /// <param name="angle">The angle in degrees.</param>
            public void SetRotation(int angle)
            {
                float angleDiff = angle - GetRotation();
                meshGenerator.transform.Rotate(Vector3.up, angleDiff);               
            }

            public float GetSlope() => meshGenerator.slope * Mathf.Rad2Deg;
        }

        public float height;
        public float width;
        public float thickness;
        public float slope; // how steep the landing is in rad

        public int resolution;

        private const float sideSlope = 0.3f;

        // instance-wide indices for the corners
        private int leftFrontBottomCornerIndex;
        private int rightFrontBottomCornerIndex;

        private int leftRearBottomCornerIndex;
        private int rightRearBottomCornerIndex;

        private int leftRearUpperCornerIndex;
        private int rightRearUpperCornerIndex;

        private int leftFrontUpperCornerIndex;
        private int rightFrontUpperCornerIndex;

        private int leftRadiusSlopeBorderIndex;
        private int rightRadiusSlopeBorderIndex;

        float CalculateRadiusLength(float radius)
        {
            float radiusLength = radius * Mathf.Cos(90 * Mathf.Deg2Rad - slope);
            Debug.Log("radius length:" + radiusLength);

            return radiusLength;
        }

        float CalculateSlopeLength()
        {
            float ninetyDegInRad = 90 * Mathf.Deg2Rad;

            Debug.Log("slope length: " + Mathf.Tan(ninetyDegInRad - slope) * height / 2);
            return Mathf.Tan(ninetyDegInRad-slope) * height / 2;
        }

        float CalculateRadius()
        {
            float radius = height / (2 - 2 * Mathf.Cos(slope));
            Debug.Log("radius: " + radius);
            return radius;
        }

        float CalculateLength()
        {
            return sideSlope*height + thickness + CalculateSlopeLength() + CalculateRadiusLength(CalculateRadius());
        }

        // Use this for initialization
        void Start()
        {
            GenerateLandingMesh();
        }

        Vector3[] CreateVertices()
        {
            // make the bottom corners wider than the top
            float bottomCornerWidth = width + 2 * height * sideSlope;
            float radiusSlopeBorderWidth = width + 2 * height/2 * sideSlope;
            float bottomCornerOffset = thickness * sideSlope;

            float radius = CalculateRadius();

            // z coordinate of the point where the slope and the radius of the landing meet
            float radSlopeBorder = CalculateSlopeLength();
            float radLengthZ = CalculateRadiusLength(radius);

            float leftUpperX = -width / 2;
            float leftBottomX = -bottomCornerWidth / 2;
            float rightUpperX = width / 2;
            float rightBottomX = bottomCornerWidth / 2;



            Vector3[] leftArc = new Vector3[resolution + 1];
            Vector3 leftFrontUpperCorner = new(leftUpperX, height, 0);
            Vector3 leftRearUpperCorner = new(leftUpperX, height, -thickness);
            Vector3 leftFrontBottomCorner = new(leftBottomX, 0, radSlopeBorder);
            Vector3 leftRearBottomCorner = new(leftBottomX, 0, -(thickness + bottomCornerOffset));

            Vector3[] rightArc = new Vector3[resolution + 1];
            Vector3 rightFrontUpperCorner = new(rightUpperX, height, 0);
            Vector3 rightRearUpperCorner = new(rightUpperX, height, -thickness);
            Vector3 rightFrontBottomCorner = new(rightBottomX, 0, radSlopeBorder);
            Vector3 rightRearBottomCorner = new(rightBottomX, 0, -(thickness + bottomCornerOffset));


            float angleEnd = 270 * Mathf.Deg2Rad;
            float angleStart = angleEnd + slope;

            // calculate the arc points
            for (int i = 0; i < resolution + 1; i++)
            {
                float t = (float)i / resolution;
                float angle = Mathf.Lerp(angleStart, angleEnd, t);
                float lengthwise = radLengthZ - Mathf.Cos(angle) * radius;
                float heightwise = Mathf.Sin(angle) * radius + radius;
                leftArc[i] = new Vector3(-(radiusSlopeBorderWidth/2 + (height/2 - heightwise) * sideSlope), heightwise, radSlopeBorder + lengthwise);
                if (i < resolution / 2)
                {
                    Debug.DrawRay(transform.TransformPoint(leftArc[i]), Vector3.up, Color.cyan, 10);
                }
                else
                {
                    Debug.DrawRay(transform.TransformPoint(leftArc[i]), Vector3.up, Color.black, 10);
                }
                rightArc[i] = new Vector3(radiusSlopeBorderWidth/2 + (height / 2 - heightwise) * sideSlope, heightwise, radSlopeBorder + lengthwise);
            }

            // add the points where the radius ends and the slope starts
            leftRadiusSlopeBorderIndex = 0;
            rightRadiusSlopeBorderIndex = resolution + 1;
            

            Vector3[] vertices = new Vector3[2 * (resolution + 1) + 10];

            for (int i = 0; i < resolution + 1; i++)
            {
                vertices[i] = leftArc[i];
                vertices[i + resolution + 1] = rightArc[i];
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

            leftFrontUpperCornerIndex = resolution * 2 + 8;
            rightFrontUpperCornerIndex = resolution * 2 + 9;
            vertices[leftFrontUpperCornerIndex] = leftFrontUpperCorner;
            vertices[rightFrontUpperCornerIndex] = rightFrontUpperCorner;

            Debug.DrawRay(transform.TransformPoint(leftFrontUpperCorner), Vector3.up, Color.red, 10);
            Debug.DrawRay(transform.TransformPoint(rightFrontUpperCorner), Vector3.up, Color.blue, 10);

            return vertices;
        }

        int[] CreateTriangles()
        {
            // Generate triangles
            int triangleCount = 2 * resolution + 2 * resolution + 12;
            int triangleIndexCount = triangleCount * 3;
            int[] triangles = new int[triangleIndexCount];
            int triIndex = 0;

            // Connect front face triangles
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
                triangles[triIndex++] = leftIndex + 1;
                triangles[triIndex++] = leftIndex;
                triangles[triIndex++] = leftFrontBottomCornerIndex;
                //right
                triangles[triIndex++] = rightIndex;
                triangles[triIndex++] = rightIndex + 1;
                triangles[triIndex++] = rightFrontBottomCornerIndex;
            }

            // create front face triangles
            triangles[triIndex++] = leftRadiusSlopeBorderIndex;
            triangles[triIndex++] = rightRadiusSlopeBorderIndex;
            triangles[triIndex++] = leftFrontUpperCornerIndex;

            triangles[triIndex++] = leftFrontUpperCornerIndex;
            triangles[triIndex++] = rightRadiusSlopeBorderIndex;
            triangles[triIndex++] = rightFrontUpperCornerIndex;

            // create left side slope triangles
            triangles[triIndex++] = leftRearBottomCornerIndex;
            triangles[triIndex++] = leftFrontBottomCornerIndex;
            triangles[triIndex++] = leftRadiusSlopeBorderIndex;

            triangles[triIndex++] = leftRearBottomCornerIndex;
            triangles[triIndex++] = leftRadiusSlopeBorderIndex;
            triangles[triIndex++] = leftFrontUpperCornerIndex;

            triangles[triIndex++] = leftRearBottomCornerIndex;
            triangles[triIndex++] = leftFrontUpperCornerIndex;
            triangles[triIndex++] = leftRearUpperCornerIndex;

            // create right side slope triangles
            triangles[triIndex++] = rightRearBottomCornerIndex;
            triangles[triIndex++] = rightRadiusSlopeBorderIndex;
            triangles[triIndex++] = rightFrontBottomCornerIndex;

            triangles[triIndex++] = rightRearBottomCornerIndex;
            triangles[triIndex++] = rightFrontUpperCornerIndex;
            triangles[triIndex++] = rightRadiusSlopeBorderIndex;

            triangles[triIndex++] = rightRearBottomCornerIndex;
            triangles[triIndex++] = rightRearUpperCornerIndex;
            triangles[triIndex++] = rightFrontUpperCornerIndex;


            // create upper flat side triangles
            triangles[triIndex++] = leftRearUpperCornerIndex;
            triangles[triIndex++] = rightFrontUpperCornerIndex;
            triangles[triIndex++] = rightRearUpperCornerIndex;

            triangles[triIndex++] = leftFrontUpperCornerIndex;
            triangles[triIndex++] = rightFrontUpperCornerIndex;
            triangles[triIndex++] = leftRearUpperCornerIndex;


            // create back side triangles
            triangles[triIndex++] = leftRearBottomCornerIndex;
            triangles[triIndex++] = leftRearUpperCornerIndex;
            triangles[triIndex++] = rightRearBottomCornerIndex;

            triangles[triIndex++] = leftRearUpperCornerIndex;
            triangles[triIndex++] = rightRearUpperCornerIndex;
            triangles[triIndex++] = rightRearBottomCornerIndex;

            return triangles;
        }

        public void GenerateLandingMesh()
        {
            Mesh mesh = new();
            GetComponent<MeshFilter>().mesh = mesh;

            Debug.Log("Length in mesh generation method: " + CalculateLength());

            Vector3[] vertices = CreateVertices();

            int[] triangles = CreateTriangles();

            // Assign to mesh
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
        }
    }
}