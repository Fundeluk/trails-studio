using Assets.Scripts.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Cinemachine;

namespace Assets.Scripts
{
    public class CameraManager : Singleton<CameraManager>
    {
        [Header("Virtual cameras")]
        public GameObject defaultCam;
        public GameObject topDownCam;
        public GameObject detailedViewCam;

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

            cinemachineCamera.Prioritize();
        }

        public void DefaultView()
        {
            currentCam = defaultCam;

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


        public Transform GetCurrentCamTransform()
        {
            return currentCam.transform;
        }

        public void Start()
        {
            // activate the cameras at this point to ensure the default cameras view gets shown first
            defaultCam.SetActive(true);
            topDownCam.SetActive(true);
            detailedViewCam.SetActive(true);
        }
    }
}
