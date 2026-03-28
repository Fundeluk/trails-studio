using UnityEngine;

namespace Obstacles
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
                if (!Mathf.Approximately(_height, value))
                {
                    _height = value;
                    GenerateMesh();
                }
            }
        }

        private float width;
        public virtual float Width
        {
            get => width;
            set
            {
                if (!Mathf.Approximately(width, value))
                {
                    width = value;
                    GenerateMesh();
                }
            }
        }

        private float thickness;
        public virtual float Thickness
        {
            get => thickness;
            set
            {
                if (!Mathf.Approximately(thickness, value))
                {
                    thickness = value;
                    GenerateMesh();
                }
            }
        }

        private int resolution;
        public virtual int Resolution // Number of segments along the curve
        {
            get => resolution;
            set
            {
                if (resolution != value)
                {
                    resolution = value;
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
        protected void SetBatch(float? height = null, float? width = null, float? thickness = null, int? resolution = null)
        {
            if (height.HasValue)
            {
                _height = height.Value;
            }

            if (width.HasValue)
            {
                this.width = width.Value;
            }

            if (thickness.HasValue)
            {
                this.thickness = thickness.Value;
            }            

            if (resolution.HasValue)
            {
                this.resolution = resolution.Value;
            }
        }

        protected abstract Vector3[] CreateVertices();
        protected abstract int[] CreateTriangles();

        protected virtual void GenerateMesh()
        {
            Mesh mesh = new();

            //InternalDebug.Log("Length in mesh generation method: " + CalculateLength());

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