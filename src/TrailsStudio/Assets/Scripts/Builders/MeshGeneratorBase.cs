using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Builders
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
    public abstract class MeshGeneratorBase : MonoBehaviour
    {
        [SerializeField]
        ObstacleMaterialCache materials;

        protected float _height;
        public virtual float Height
        {
            get => _height;
            set
            {
                if (_height != value)
                {
                    _height = value;
                    GenerateMesh();
                }
            }
        }

        protected float _width;
        public virtual float Width
        {
            get => _width;
            set
            {
                if (_width != value)
                {
                    _width = value;
                    GenerateMesh();
                }
            }
        }

        protected float _thickness;
        public virtual float Thickness
        {
            get => _thickness;
            set
            {
                if (_thickness != value)
                {
                    _thickness = value;
                    GenerateMesh();
                }
            }
        }

        protected int _resolution;
        public virtual int Resolution // Number of segments along the curve
        {
            get => _resolution;
            set
            {
                if (_resolution != value)
                {
                    _resolution = value;
                    GenerateMesh();
                }
            }
        }        

        public void SetCanBuildMaterial() => GetComponent<MeshRenderer>().material = materials.canBuildMaterial;

        public void SetCannotBuildMaterial() => GetComponent<MeshRenderer>().material = materials.cannotBuildMaterial;        

        public void SetDefaultDirtMaterial() => GetComponent<MeshRenderer>().material = materials.defaultDirtMaterial;
        

        /// <summary>
        /// Set all parameters at once without redrawing the mesh for each change.
        /// Parameters are optional, if they are not provided or null, the current value will be used.
        /// </summary>    
        public void SetBatch(float? height = null, float? width = null, float? thickness = null, int? resolution = null)
        {
            if (height.HasValue)
            {
                _height = height.Value;
            }

            if (width.HasValue)
            {
                _width = width.Value;
            }

            if (thickness.HasValue)
            {
                _thickness = thickness.Value;
            }            

            if (resolution.HasValue)
            {
                _resolution = resolution.Value;
            }
        }

        protected abstract Vector3[] CreateVertices();
        protected abstract int[] CreateTriangles();

        public virtual void GenerateMesh()
        {
            Mesh mesh = new();

            //Debug.Log("Length in mesh generation method: " + CalculateLength());

            Vector3[] vertices = CreateVertices();

            int[] triangles = CreateTriangles();

            // Assign to mesh
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GetComponent<MeshFilter>().mesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        public abstract float GetSideSlope();       
    }
}