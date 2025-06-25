using UnityEngine;

namespace Assets.Scripts.Builders
{
    public class LandingMeshGenerator : MeshGeneratorBase
    {
        // Default values for new takeoffs
        [Header("Default Parameters")]
        [SerializeField] float defaultHeight = 2.5f;
        [SerializeField] float defaultWidth = 3.0f;
        [SerializeField] float defaultThickness = 1f;
        [SerializeField] float defaultSlope = 45f;
        [SerializeField] int defaultResolution = 10;

        private float _slope;

        /// <summary>
        /// Slope of the landing. In radians.
        /// </summary>
        public float Slope
        {
            get => _slope;
            set
            {                
                if (_slope != value)
                {
                    _slope = value;
                    radius = CalculateRadius();
                    GenerateMesh();
                }
            }
        }

        public override float Height
        {
            set
            {
                if (_height != value)
                {
                    _height = value;
                    radius = CalculateRadius();
                    GenerateMesh();
                }
            }
        }
        

        public override float GetSideSlope() => 0.3f;

        private float radius;

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

        /// <summary>
        /// Set all parameters at once without redrawing the mesh for each change.
        /// Parameters are optional, if they are not provided or null, the current value will be used.
        /// </summary>    
        public void SetBatch(float? height = null, float? width = null, float? thickness = null, float? slope = null, int? resolution = null)
        {
            if (slope.HasValue)
            {
                _slope = slope.Value;
            }

            base.SetBatch(height, width, thickness, resolution);

            radius = CalculateRadius();

            GenerateMesh();
        }

        public float CalculateSlopeLength() => Height / 2 * Mathf.Sin(Slope);

        /// <returns>What length does the landing area have in meters.</returns>
        public float CalculateLandingAreaLength()
        {
            float transitionLength = radius * Slope;

            return CalculateSlopeLength() + transitionLength;
        }

        /// <returns>What Length does the mesh's radius part have in the forward direction in meters.</returns>
        public float CalculateRadiusLengthXZ()
        {
            float radiusLengthXZ = radius * Mathf.Sin(Slope); // use parametric equation (x = r * sin(a) )
            return radiusLengthXZ;
        }


        /// <returns>What Length does the mesh's flat slope part have in the forward directon in meters.</returns>
        public float CalculateSlopeLengthXZ()
        {
            float ninetyDegInRad = 90 * Mathf.Deg2Rad;
            return Mathf.Tan(ninetyDegInRad - Slope) * Height / 2;
        }

        public float CalculateRadius()
        {
            float radius = Height / (2 - 2 * Mathf.Cos(Slope));
            return radius;
        }

       
        /// <returns>What length does the mesh's landing portion have on the XZ plane</returns>
        public float CalculateLandingAreaLengthXZ()
        {
            return CalculateRadiusLengthXZ() + CalculateSlopeLengthXZ();
        }

        /// <returns>What Length does the entire mesh have from its start point to endpoint.</returns>
        public float CalculateLength()
        {
            return GetSideSlope() * Height + Thickness + CalculateSlopeLengthXZ() + CalculateRadiusLengthXZ();
        }

        // Use this for initialization
        protected void Awake()
        {
            SetBatch(defaultHeight, defaultWidth, defaultThickness, defaultSlope * Mathf.Deg2Rad, defaultResolution);
        }

        protected override Vector3[] CreateVertices()
        {
            // make the bottom corners wider than the top
            float bottomCornerWidth = Width + 2 * Height * GetSideSlope();
            float radiusSlopeBorderWidth = Width + 2 * Height / 2 * GetSideSlope();
            float bottomCornerOffset = Height * GetSideSlope();

            // z coordinate of the point where the Slope and the Radius of the landing meet
            float radSlopeBorder = CalculateSlopeLengthXZ();
            float radLengthZ = CalculateRadiusLengthXZ();

            float leftUpperX = -Width / 2;
            float leftBottomX = -bottomCornerWidth / 2;
            float rightUpperX = Width / 2;
            float rightBottomX = bottomCornerWidth / 2;


            Vector3[] leftArc = new Vector3[Resolution + 1];
            Vector3 leftFrontUpperCorner = new(leftUpperX, Height, 0);
            Vector3 leftRearUpperCorner = new(leftUpperX, Height, -Thickness);
            Vector3 leftFrontBottomCorner = new(leftBottomX, 0, radSlopeBorder);
            Vector3 leftRearBottomCorner = new(leftBottomX, 0, -(Thickness + bottomCornerOffset));

            Vector3[] rightArc = new Vector3[Resolution + 1];
            Vector3 rightFrontUpperCorner = new(rightUpperX, Height, 0);
            Vector3 rightRearUpperCorner = new(rightUpperX, Height, -Thickness);
            Vector3 rightFrontBottomCorner = new(rightBottomX, 0, radSlopeBorder);
            Vector3 rightRearBottomCorner = new(rightBottomX, 0, -(Thickness + bottomCornerOffset));


            float angleEnd = 270 * Mathf.Deg2Rad;
            float angleStart = angleEnd + Slope;

            // calculate the arc points
            for (int i = 0; i < Resolution + 1; i++)
            {
                float t = (float)i / Resolution;
                float angle = Mathf.Lerp(angleStart, angleEnd, t);
                float lengthwise = radLengthZ - Mathf.Cos(angle) * radius;
                float heightwise = Mathf.Sin(angle) * radius + radius;
                leftArc[i] = new Vector3(-(radiusSlopeBorderWidth / 2 + (Height / 2 - heightwise) * GetSideSlope()), heightwise, radSlopeBorder + lengthwise);                
                rightArc[i] = new Vector3(radiusSlopeBorderWidth / 2 + (Height / 2 - heightwise) * GetSideSlope(), heightwise, radSlopeBorder + lengthwise);
            }

            // add the points where the Radius ends and the slope starts
            leftRadiusSlopeBorderIndex = 0;
            rightRadiusSlopeBorderIndex = Resolution + 1;


            Vector3[] vertices = new Vector3[2 * (Resolution + 1) + 10];

            for (int i = 0; i < Resolution + 1; i++)
            {
                vertices[i] = leftArc[i];
                vertices[i + Resolution + 1] = rightArc[i];
            }

            // add front bottom corners
            leftFrontBottomCornerIndex = Resolution * 2 + 2;
            rightFrontBottomCornerIndex = Resolution * 2 + 3;
            vertices[leftFrontBottomCornerIndex] = leftFrontBottomCorner;
            vertices[rightFrontBottomCornerIndex] = rightFrontBottomCorner;

            // add rear bottom corners
            leftRearBottomCornerIndex = Resolution * 2 + 4;
            rightRearBottomCornerIndex = Resolution * 2 + 5;
            vertices[leftRearBottomCornerIndex] = leftRearBottomCorner;
            vertices[rightRearBottomCornerIndex] = rightRearBottomCorner;

            // add rear upper corners
            leftRearUpperCornerIndex = Resolution * 2 + 6;
            rightRearUpperCornerIndex = Resolution * 2 + 7;
            vertices[leftRearUpperCornerIndex] = leftRearUpperCorner;
            vertices[rightRearUpperCornerIndex] = rightRearUpperCorner;

            leftFrontUpperCornerIndex = Resolution * 2 + 8;
            rightFrontUpperCornerIndex = Resolution * 2 + 9;
            vertices[leftFrontUpperCornerIndex] = leftFrontUpperCorner;
            vertices[rightFrontUpperCornerIndex] = rightFrontUpperCorner;

            return vertices;
        }

        protected override int[] CreateTriangles()
        {
            // Generate triangles
            int triangleCount = 2 * Resolution + 2 * Resolution + 12;
            int triangleIndexCount = triangleCount * 3;
            int[] triangles = new int[triangleIndexCount];
            int triIndex = 0;

            // Connect front face triangles
            for (int i = 0; i < Resolution; i++)
            {
                int leftIndex = i;
                int rightIndex = i + Resolution + 1;

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

    }
}