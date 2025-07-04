using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.Builders
{
    public class ParamChangeEventArgs<ParamT> : EventArgs
    {
        public string ParamName { get; }
        public ParamT NewValue { get; }
        public ParamChangeEventArgs(string paramName, ParamT newValue)
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

        protected GameObject cameraTarget;

        protected SlopeChange slope = null;

        protected ILineElement previousLineElement;

        private bool hasTooltipOn = false;

        /// <summary>
        /// If the obstacle is built on a slope, this is the set of coordinates that are occupied as a result of the build.
        /// </summary>
        protected HeightmapCoordinates slopeHeightmapCoordinates = null;

        protected void InitCameraTarget()
        {
            cameraTarget = new GameObject("Camera Target");
            cameraTarget.transform.SetParent(transform);
        }

        public virtual void Initialize()
        {            
            meshGenerator = GetComponent<T>();            

            InitCameraTarget();
            previousLineElement = Line.Instance.GetLastLineElement();
        }

        public virtual void Initialize(T meshGenerator, GameObject cameraTarget, ILineElement previousLineElement)
        {
            this.meshGenerator = meshGenerator;
            this.cameraTarget = cameraTarget;
            this.previousLineElement = previousLineElement;
        }

        /// <summary>
        /// Adds the coordinates of the heightmap that are occupied by the slope to <see cref="slopeHeightmapCoordinates"/> and marks them as occuppied.
        /// </summary>
        public virtual void AddSlopeHeightmapCoords(HeightmapCoordinates coords)
        {
            if (slopeHeightmapCoordinates == null)
            {
                slopeHeightmapCoordinates = new(coords);
            }
            else
            {
                slopeHeightmapCoordinates.Add(coords);
            }

            slopeHeightmapCoordinates.MarkAs(new HeightSetCoordinateState());
        }

        public void SetSlopeChange(SlopeChange slope)
        {
            this.slope = slope;
        }

        public SlopeChange GetSlopeChange() => slope;

        public void RecalculateCameraTargetPosition()
        {
            cameraTarget.transform.position = Vector3.Lerp(GetStartPoint(), GetEndPoint(), 0.5f) + (0.5f * GetHeight() * GetTransform().up);
        }        

        public virtual void DestroyUnderlyingGameObject()
        {      
            if (cameraTarget != null)
            {
                Destroy(cameraTarget);
            }
            
            Destroy(gameObject);
        }

        public abstract Vector3 GetEndPoint();

        public abstract Vector3 GetStartPoint();

        public abstract float GetLength();

        /// <remarks>Calculated on XZ plane to avoid influence of height differences in terrain.</remarks>
        /// <returns>The distance from previous <see cref="ILineElement"/>s endpoint to this takeoffs startpoint in meters.</returns>        
        public virtual float GetDistanceFromPreviousLineElement()
        {
            Vector3 previousEnd = previousLineElement.GetEndPoint();
            previousEnd.y = 0;

            Vector3 start = GetStartPoint();
            start.y = 0;

            return Vector3.Distance(previousEnd, start);
        }

        public float GetPreviousElementBottomWidth() => previousLineElement.GetBottomWidth();        

        public float GetBottomWidth() => meshGenerator.Width + 2 * meshGenerator.Height * GetSideSlope();

        public float GetHeight() => meshGenerator.Height;    

        public float GetThickness() => meshGenerator.Thickness;

        public float GetWidth() => meshGenerator.Width;

        public Vector3 GetRideDirection() => transform.forward.normalized;

        public Transform GetTransform() => transform;

        public GameObject GetCameraTarget() => cameraTarget;

        public float GetSideSlope() => meshGenerator.GetSideSlope();

        public HeightmapCoordinates GetObstacleHeightmapCoordinates() => new (GetStartPoint(), GetEndPoint(), Mathf.Max(GetBottomWidth(), GetPreviousElementBottomWidth()));

        public HeightmapCoordinates GetUnderlyingSlopeHeightmapCoordinates()
        {            
            return slopeHeightmapCoordinates;
        }

        public void Outline()
        {
            GetComponent<MeshRenderer>().renderingLayerMask = Line.outlinedElementRenderLayerMask;
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
            Outline();
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