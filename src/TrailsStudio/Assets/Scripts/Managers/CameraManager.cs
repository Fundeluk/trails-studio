using Assets.Scripts.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Cinemachine;
using Assets.Scripts.Builders;

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

        private GameObject currentCam;

        /// <summary>
        /// Positions the camera to look at the target from a top-down view.
        /// </summary>
        /// <param name="target">The target to focus on</param>
        public void TopDownFollowHighlight(GameObject highlight)
        {
            currentCam = topDownCam;
            CinemachineCamera cinemachineCamera = currentCam.GetComponent<CinemachineCamera>();

            cinemachineCamera.Target.TrackingTarget = highlight.transform;

            Vector3 rideDir = Line.Instance.GetCurrentRideDirection();

            Vector3 rideDirNormal = Vector3.Cross(rideDir, Vector3.up).normalized;

            Quaternion camRotation = Quaternion.LookRotation(highlight.transform.position - (highlight.transform.position + 20f * Vector3.up), rideDirNormal);

            cinemachineCamera.transform.rotation = camRotation;

            cinemachineCamera.Prioritize();
        }

        public void SplineCamView()
        {
            currentCam = splineCam;

            // move the camera so that it is near the last line element
            splineCart.GetComponent<CinemachineSplineCart>().SplinePosition = 0.9f;            

            currentCam.GetComponent<CinemachineCamera>().Prioritize();
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
            currentCam = rotateAroundCam;

            ILineElement lastObstacle = Line.Instance.GetLastLineElement();

            GameObject cameraTarget = lastObstacle.GetCameraTarget();

            currentCam.transform.position = cameraTarget.transform.position + 2f * lastObstacle.GetLength() * lastObstacle.GetRideDirection() + 0.75f * lastObstacle.GetHeight() * Vector3.up;

            CinemachineCamera cinemachineCamera = currentCam.GetComponent<CinemachineCamera>();

            cinemachineCamera.Target.TrackingTarget = cameraTarget.transform;
            cinemachineCamera.Target.CustomLookAtTarget = false;

            currentCam.GetComponent<ConstantRotation>().target = cameraTarget;

            cinemachineCamera.Prioritize();
        }

        public void DetailedView(ILineElement target)
        {
            currentCam = detailedViewCam;

            GameObject cameraTarget = target.GetCameraTarget();
                        
            CinemachineCamera cinemachineCamera = currentCam.GetComponent<CinemachineCamera>();

            cinemachineCamera.Target.TrackingTarget = cameraTarget.transform;
            cinemachineCamera.Target.CustomLookAtTarget = false;            

            cinemachineCamera.Prioritize();
        }

        public void DetailedView(GameObject target)
        {
            currentCam = detailedViewCam;
            CinemachineCamera cinemachineCamera = currentCam.GetComponent<CinemachineCamera>();
            cinemachineCamera.Target.TrackingTarget = target.transform;
            cinemachineCamera.Target.CustomLookAtTarget = false;
            cinemachineCamera.Prioritize();
        }

        public void Start()
        {
            // activate the cameras at this point to ensure the default cameras view gets shown first
            rotateAroundCam.SetActive(true);
            // enable rotate script only when the camera is activated
            CinemachineCameraEvents rotateCMEvents = rotateAroundCam.GetComponent<CinemachineCameraEvents>();
            rotateCMEvents.CameraActivatedEvent.AddListener((mixer, cam) => rotateAroundCam.GetComponent<ConstantRotation>().enabled = true);
            rotateCMEvents.CameraDeactivatedEvent.AddListener((mixer, cam) => rotateAroundCam.GetComponent<ConstantRotation>().enabled = false);

            splineCart.SetActive(true);
            CinemachineCameraEvents splineCamEvents = splineCam.GetComponent<CinemachineCameraEvents>();
            ZoomableCamera zoomScript = splineCam.GetComponent<ZoomableCamera>();
            MovableSplineCart splineCartScript = splineCart.GetComponent<MovableSplineCart>();

            // enable zooming and moving the cam along lineSpline only if activated
            splineCamEvents.CameraActivatedEvent.AddListener((mixer, cam) => { splineCartScript.enabled = true; zoomScript.enabled = true;});
            splineCamEvents.CameraDeactivatedEvent.AddListener((mixer, cam) => { splineCartScript.enabled = false; zoomScript.enabled = false; });

            topDownCam.SetActive(true);
            detailedViewCam.SetActive(true);
        }
    }
}
