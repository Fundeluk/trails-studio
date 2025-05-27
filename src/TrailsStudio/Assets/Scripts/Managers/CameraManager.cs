using Assets.Scripts.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Cinemachine;
using Assets.Scripts.Builders;
using UnityEditor.PackageManager;
using UnityEngine.Events;

namespace Assets.Scripts
{
    public class CameraManager : Singleton<CameraManager>
    {
        [Header("Virtual cameras")]
        [SerializeField]
        GameObject rotateAroundCam;
        [SerializeField]
        GameObject topDownCam;
        [SerializeField]
        GameObject detailedViewCam;

        [Header("Spline camera")]
        [SerializeField]
        GameObject splineCart;
        [SerializeField]
        GameObject splineCam;

        public GameObject CurrentCam { get; private set; }

        /// <summary>
        /// Positions the camera to look at the target from a top-down view.
        /// </summary>
        /// <param name="target">The target to focus on</param>
        public void TopDownFollowHighlight(GameObject highlight, Vector3 lookDir)
        {            

            CurrentCam = topDownCam;
            CinemachineCamera cinemachineCamera = CurrentCam.GetComponent<CinemachineCamera>();

            cinemachineCamera.Target.TrackingTarget = highlight.transform;

            Vector3 rideDirNormal = Vector3.Cross(Line.Instance.GetCurrentRideDirection(), Vector3.up).normalized;           

            Quaternion camRotation = Quaternion.LookRotation(lookDir, rideDirNormal);

            cinemachineCamera.transform.rotation = camRotation;

            cinemachineCamera.Prioritize();
        }

        public void SplineCamView()
        {
            if (CurrentCam == splineCam)
            {
                return;
            }

            //// move spline cam near end of spline
            float splinePos = splineCart.GetComponent<MovableSplineCart>().DefaultSplinePosition;
            CinemachineSplineCart splineCartComponent = splineCart.GetComponent<CinemachineSplineCart>();
            splineCartComponent.SplinePosition = splinePos;   

            SplineCamera splineCamComponent = splineCam.GetComponent<SplineCamera>();
            splineCamComponent.trackingTarget.UpdateTrackingTarget(splinePos);

            CurrentCam = splineCam;            
            CurrentCam.GetComponent<CinemachineCamera>().Prioritize();
        }

        /// <summary>
        /// Adds a listener to a <see cref="CinemachineCameraEvents.BlendFinishedEvent"/> of the top-down camera.
        /// If the top-down camera is currently active, the event is invoked immediately.
        /// </summary>
        public void AddOnTDCamBlendFinishedEvent(UnityAction<ICinemachineMixer, ICinemachineCamera> call)
        {
            if (CurrentCam == topDownCam)
            {
                call.Invoke(GetComponent<ICinemachineMixer>(), topDownCam.GetComponent<CinemachineCamera>());
            }

            CinemachineCameraEvents topDownCamEvents = topDownCam.GetComponent<CinemachineCameraEvents>();
            topDownCamEvents.BlendFinishedEvent.AddListener(call);
        }

        public void ClearOnTDCamBlendFinishedEvents()
        {
            CinemachineCameraEvents topDownCamEvents = topDownCam.GetComponent<CinemachineCameraEvents>();
            topDownCamEvents.BlendFinishedEvent.RemoveAllListeners();
        }

        public CinemachineCameraEvents GetTDCamEvents()
        {
            return topDownCam.GetComponent<CinemachineCameraEvents>();
        }

        public float GetTDCamDistance()
        {
            return topDownCam.GetComponent<CinemachinePositionComposer>().CameraDistance;
        }

        public void RotateAroundView()
        {
            CurrentCam = rotateAroundCam;

            ILineElement lastObstacle = Line.Instance.GetLastLineElement();

            GameObject cameraTarget = lastObstacle.GetCameraTarget();

            CurrentCam.transform.position = cameraTarget.transform.position + 2f * lastObstacle.GetLength() * lastObstacle.GetRideDirection() + 0.75f * lastObstacle.GetHeight() * Vector3.up;

            CinemachineCamera cinemachineCamera = CurrentCam.GetComponent<CinemachineCamera>();

            cinemachineCamera.Target.TrackingTarget = cameraTarget.transform;
            cinemachineCamera.Target.CustomLookAtTarget = false;

            ConstantRotation rotateCamScript = rotateAroundCam.GetComponent<ConstantRotation>();

            rotateCamScript.SetTarget(cameraTarget);

            cinemachineCamera.Prioritize();
        }

        public void DetailedView(ILineElement target)
        {
            CurrentCam = detailedViewCam;

            GameObject cameraTarget = target.GetCameraTarget();
                        
            CinemachineCamera cinemachineCamera = CurrentCam.GetComponent<CinemachineCamera>();

            cinemachineCamera.Target.TrackingTarget = cameraTarget.transform;
            cinemachineCamera.Target.CustomLookAtTarget = false;            

            cinemachineCamera.Prioritize();
        }

        public void DetailedView(GameObject target)
        {
            CurrentCam = detailedViewCam;
            CinemachineCamera cinemachineCamera = CurrentCam.GetComponent<CinemachineCamera>();
            cinemachineCamera.Target.TrackingTarget = target.transform;
            cinemachineCamera.Target.CustomLookAtTarget = false;
            cinemachineCamera.Prioritize();
        }

        /// <summary>
        /// Initializes the rotate around camera and its events.
        /// </summary>
        void InitRotateAroundCam()
        {
            var rotateCamScript = rotateAroundCam.GetComponent<RotateAroundCamera>();

            // enable rotate script only when the camera is activated
            CinemachineCameraEvents rotateCMEvents = rotateAroundCam.GetComponent<CinemachineCameraEvents>();
            rotateCMEvents.CameraActivatedEvent.AddListener((mixer, cam) => rotateCamScript.enabled = true);
            rotateCMEvents.CameraDeactivatedEvent.AddListener((mixer, cam) => rotateCamScript.enabled = false);
        }

        /// <summary>
        /// Initializes the spline camera and its events.
        /// </summary>
        void InitSplineCam()
        {            
            CinemachineCameraEvents splineCamEvents = splineCam.GetComponent<CinemachineCameraEvents>();
            SplineCamera splineCamScript = splineCam.GetComponent<SplineCamera>();

            //// move spline cam near end of spline
            float splinePos = splineCart.GetComponent<MovableSplineCart>().DefaultSplinePosition;
            CinemachineSplineCart splineCartComponent = splineCart.GetComponent<CinemachineSplineCart>();
            splineCartComponent.SplinePosition = splinePos;


            splineCamScript.trackingTarget.UpdateTrackingTarget(splinePos);
            splineCamScript.RecenterCamera();

            splineCamEvents.CameraActivatedEvent.AddListener((mixer, cam) => splineCamScript.enabled = true);
            splineCamEvents.CameraDeactivatedEvent.AddListener((mixer, cam) => splineCamScript.enabled = false);
        }

        public void Awake()
        {
            InitSplineCam();
            InitRotateAroundCam();            
        }


        /// <summary>
        /// Returns the camera-relative movement direction based on the input and the camera's forward direction.
        /// </summary>        
        public static Vector3 CameraRelativeFlatten(Vector2 input, Vector3 camForward)
        {
            Vector3 flattened = Vector3.ProjectOnPlane(camForward, Vector3.up);
            Quaternion camOrientation = Quaternion.LookRotation(flattened);

            return camOrientation * new Vector3(input.x, 0, input.y);
        }
    }
}
