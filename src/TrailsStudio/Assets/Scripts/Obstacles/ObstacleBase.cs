using System;
using LineSystem;
using Managers;
using TerrainEditing;
using TerrainEditing.Slope;
using UnityEngine;

namespace Obstacles
{
    public class ParamChangeEventArgs<TParam> : EventArgs
    {
        public string ParamName { get; }
        public TParam NewValue { get; }
        public ParamChangeEventArgs(string paramName, TParam newValue)
        {
            ParamName = paramName;
            NewValue = newValue;
        }
    }

    public abstract class ObstacleBase<T> : MonoBehaviour where T : MeshGeneratorBase
    {
        public event EventHandler<ParamChangeEventArgs<float>> HeightChanged;
        protected void OnHeightChanged(float newHeight)
        {
            HeightChanged?.Invoke(this, new ParamChangeEventArgs<float>("Height", newHeight));
        }

        public event EventHandler<ParamChangeEventArgs<float>> WidthChanged;
        protected void OnWidthChanged(float newWidth)
        {
            WidthChanged?.Invoke(this, new ParamChangeEventArgs<float>("Width", newWidth));
        }

        public event EventHandler<ParamChangeEventArgs<float>> ThicknessChanged;
        protected void OnThicknessChanged(float newThickness)
        {
            ThicknessChanged?.Invoke(this, new ParamChangeEventArgs<float>("Thickness", newThickness));
        }

        public event EventHandler<ParamChangeEventArgs<Vector3>> PositionChanged;
        protected void OnPositionChanged(Vector3 newPosition)
        {
            PositionChanged?.Invoke(this, new ParamChangeEventArgs<Vector3>("Position", newPosition));
        }

        public event EventHandler<ParamChangeEventArgs<Quaternion>> RotationChanged;
        protected void OnRotationChanged(Quaternion newRotation)
        {
            RotationChanged?.Invoke(this, new ParamChangeEventArgs<Quaternion>("Rotation", newRotation));
        }

        [SerializeField]
        protected T meshGenerator;        

        protected GameObject CameraTarget;

        protected SlopeChange Slope = null;

        protected ILineElement PreviousLineElement;

        private bool hasTooltipOn = false;

        protected void InitCameraTarget()
        {
            CameraTarget = new GameObject("Camera Target");
            CameraTarget.transform.SetParent(transform);
        }

        public virtual void Initialize()
        {            
            meshGenerator = GetComponent<T>();            

            InitCameraTarget();
            PreviousLineElement = Line.Instance.GetLastLineElement();
        }

        public virtual void Initialize(T meshGenerator, GameObject cameraTarget, ILineElement previousLineElement)
        {
            this.meshGenerator = meshGenerator;
            this.CameraTarget = cameraTarget;
            this.PreviousLineElement = previousLineElement;
        }
        

        public void SetSlopeChange(SlopeChange slope)
        {
            this.Slope = slope;
        }

        public SlopeChange GetSlopeChange() => Slope;

        public void RecalculateCameraTargetPosition()
        {
            if (CameraTarget == null)
            {
                InitCameraTarget();
            }

            CameraTarget.transform.position = Vector3.Lerp(GetStartPoint(), GetEndPoint(), 0.5f) + (0.5f * GetHeight() * GetTransform().up);
        }        

        public virtual void DestroyUnderlyingGameObject()
        {      
            if (CameraTarget != null)
            {
                Destroy(CameraTarget);
            }
            
            Destroy(gameObject);
        }

        public abstract Vector3 GetEndPoint();

        public abstract Vector3 GetStartPoint();

        public abstract float GetLength();

        /// <remarks>Calculated on XZ plane to avoid influence of height differences in terrain.</remarks>
        /// <returns>The distance from previous <see cref="ILineElement"/>s endpoint to this takeoff's startpoint in meters.</returns>        
        public virtual float GetDistanceFromPreviousLineElement()
        {
            Vector3 previousEnd = PreviousLineElement.GetEndPoint();
            previousEnd.y = 0;

            Vector3 start = GetStartPoint();
            start.y = 0;

            return Vector3.Distance(previousEnd, start);
        }

        public float GetPreviousElementBottomWidth() => PreviousLineElement.GetBottomWidth();        

        public float GetBottomWidth() => meshGenerator.Width + 2 * meshGenerator.Height * GetSideSlope();

        public float GetHeight() => meshGenerator.Height;    

        public float GetThickness() => meshGenerator.Thickness;

        public float GetWidth() => meshGenerator.Width;

        public Vector3 GetRideDirection() => transform.forward.normalized;

        public Transform GetTransform() => transform;

        public GameObject GetCameraTarget() => CameraTarget;

        public float GetSideSlope() => meshGenerator.GetSideSlope();

        public TerrainManager.HeightmapCoordinates GetObstacleHeightmapCoordinates() => TerrainManager.Instance.GetCoordinatesForArea(
            GetStartPoint(), GetEndPoint(), Mathf.Max(GetBottomWidth(), GetPreviousElementBottomWidth()));

        public void AddOutline()
        {
            GetComponent<MeshRenderer>().renderingLayerMask = Line.OUTLINED_ELEMENT_RENDER_LAYER_MASK;
        }

        public void RemoveOutline()
        {
            if (hasTooltipOn)
            {
                return;
            }

            GetComponent<MeshRenderer>().renderingLayerMask = RenderingLayerMask.defaultRenderingLayerMask;
        }

        public void OnTooltipShow()
        {
            AddOutline();
            hasTooltipOn = true;
            CameraManager.Instance.DetailedView(GetCameraTarget());
        }

        public void OnTooltipClosed()
        {
            hasTooltipOn = false;
            RemoveOutline();
            CameraManager.Instance.SplineCamView();
        }
    }
}