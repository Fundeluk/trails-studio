using Assets.Scripts.Builders;
using Assets.Scripts.Managers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Assets.Scripts.Builders
{
    public class TakeoffMeshGenerator : MeshGeneratorBase
    {
        // Default values for new takeoffs
        [Header("Default Parameters")]
        [SerializeField] float defaultHeight = 2.0f;
        [SerializeField] float defaultWidth = 1.5f;
        [SerializeField] float defaultThickness = 0.5f;
        [SerializeField] int defaultResolution = 10;
        [SerializeField] float defaultRadius = 4.0f;

        private float _radius;
        public float Radius
        {
            get => _radius;
            set
            {
                if (_radius != value)
                {
                    _radius = value;
                    angleEnd = CalculateEndAngle(Radius, Height);
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
                    angleEnd = CalculateEndAngle(Radius, Height);
                    GenerateMesh();
                }
            }
        }

        const float angleStart = 270 * Mathf.Deg2Rad;
        float angleEnd;

        public float GetEndAngle() => angleEnd - angleStart;


        /// <summary>
        /// Set all parameters at once without redrawing the mesh for each change.
        /// Parameters are optional, if they are not provided or null, the current value will be used.
        /// </summary>    
        public void SetBatch(float? height = null, float? width = null, float? thickness = null, float? radius = null, int? resolution = null)
        {
            if (radius.HasValue)
            {
                _radius = radius.Value;
            }

            base.SetBatch(height, width, thickness, resolution);

            angleEnd = CalculateEndAngle(Radius, Height);

            GenerateMesh();
        }

        public override float GetSideSlope() => 0.2f;

        // instance-wide indices for the corners
        private int leftFrontBottomCornerIndex;
        private int rightFrontBottomCornerIndex;

        private int leftRearBottomCornerIndex;
        private int rightRearBottomCornerIndex;

        private int leftRearUpperCornerIndex;
        private int rightRearUpperCornerIndex;

        private int leftFrontUpperCornerIndex;
        private int rightFrontUpperCornerIndex;

        protected void Awake()
        {
            SetBatch(defaultHeight, defaultWidth, defaultThickness, defaultRadius, defaultResolution);
        }

        /// <summary>
        /// Get the Angle at which the takeoff's curve ends.
        /// </summary>
        /// <param name="radius">Radius of the circle that defines the takeoff's curve</param>
        /// <param name="height">Height of the takeoff</param>
        /// <returns>The Angle in radians.</returns>
        public static float CalculateEndAngle(float radius, float height)
        {
            float betaAngle = Mathf.Asin((radius - height) / radius);
            float alphaAngle = 90 * Mathf.Deg2Rad - betaAngle;
            return angleStart + alphaAngle;
        }

        protected override Vector3[] CreateVertices()
        {
            // make the bottom corners wider than the top
            float bottomCornerWidth = Width + 2 * Height * GetSideSlope();
            float bottomCornerOffset = Height * GetSideSlope();

            // Generate points for the takeoff's curve + corners
            Vector3[] leftFrontArc = new Vector3[Resolution + 1];
            Vector3 leftFrontBottomCorner = new(-bottomCornerWidth / 2, 0, 0);
            Vector3 leftRearBottomCorner = new(-bottomCornerWidth / 2, 0, Thickness + bottomCornerOffset);
            Vector3 leftRearUpperCorner = new(-Width / 2, Height, Thickness);

            Vector3[] rightFrontArc = new Vector3[Resolution + 1];
            Vector3 rightFrontBottomCorner = new(bottomCornerWidth / 2, 0, 0);
            Vector3 rightRearBottomCorner = new(bottomCornerWidth / 2, 0, Thickness + bottomCornerOffset);
            Vector3 rightRearUpperCorner = new(Width / 2, Height, Thickness);

            float transitionLength = CalculateTransitionLengthXZ();

            for (int i = 0; i <= Resolution; i++)
            {
                float t = (float)i / Resolution;
                float angle = Mathf.Lerp(angleStart, angleEnd, t);
                float lengthwise = -transitionLength + Mathf.Cos(angle) * Radius;
                float heightwise = Mathf.Sin(angle) * Radius + Radius;
                leftFrontArc[i] = new Vector3(-(bottomCornerWidth / 2 - heightwise * GetSideSlope()), heightwise, lengthwise);
                rightFrontArc[i] = new Vector3(bottomCornerWidth / 2 - heightwise * GetSideSlope(), heightwise, lengthwise);
            }

            // Combine vertices into one array
            Vector3[] vertices = new Vector3[(Resolution + 1) * 2 + 6];

            for (int i = 0; i <= Resolution; i++)
            {
                vertices[i] = leftFrontArc[i];
                vertices[i + Resolution + 1] = rightFrontArc[i];
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

            leftFrontUpperCornerIndex = Resolution;
            rightFrontUpperCornerIndex = 2 * Resolution + 1;

            return vertices;
        }

        
        /// <returns>The length of the transition part of the takeoff in the forward direction (XZ plane) in meters.</returns>
        public float CalculateTransitionLengthXZ()
        {
            return Mathf.Cos(angleEnd) * Radius;
        }


        /// <returns>The length of the transition in m.</returns>
        public float CalculateTransitionLength()
        {
            return Radius * (angleEnd - angleStart);
        }

        protected override int[] CreateTriangles()
        {
            // Generate triangles
            int triangleCount = 2 * Resolution + 2 * Resolution + 8;
            int triangleIndexCount = triangleCount * 3;
            int[] triangles = new int[triangleIndexCount];
            int triIndex = 0;

            // Connect front face quads
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
                triangles[triIndex++] = leftFrontBottomCornerIndex;
                triangles[triIndex++] = leftIndex + 1;
                triangles[triIndex++] = leftIndex;
                //right
                triangles[triIndex++] = rightIndex;
                triangles[triIndex++] = rightIndex + 1;
                triangles[triIndex++] = rightFrontBottomCornerIndex;
            }

            // create left side Thickness triangles
            triangles[triIndex++] = leftRearBottomCornerIndex;
            triangles[triIndex++] = leftRearUpperCornerIndex;
            triangles[triIndex++] = leftFrontBottomCornerIndex;

            triangles[triIndex++] = leftRearUpperCornerIndex;
            triangles[triIndex++] = leftFrontUpperCornerIndex;
            triangles[triIndex++] = leftFrontBottomCornerIndex;

            // create right side Thickness triangles
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
                
    }
}
