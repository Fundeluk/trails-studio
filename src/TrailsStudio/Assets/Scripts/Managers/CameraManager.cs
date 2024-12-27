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
        public void TopDownFollowHighlight()
        {
            currentCam = topDownCam;
            currentCam.GetComponent<CinemachineCamera>().Prioritize();
        }

        public void DefaultView()
        {
            ILineElement lastObstacle = Line.Instance.line[^1];

            // position the camera so that it looks at the last obstacle in line from an angle
            defaultCam.transform.position = lastObstacle.GetEndPoint() + new Vector3(5, Mathf.Max(lastObstacle.GetHeight(), 3), 5);
            defaultCam.GetComponent<CinemachineCamera>().LookAt = lastObstacle.GetCameraTarget().transform;

            defaultCam.GetComponent<CinemachineCamera>().Prioritize();
            currentCam = defaultCam;
        }

        // TODO for build phase, change camera to free look camera starting at side view
        public void DetailedView(ILineElement target)
        {
            currentCam = detailedViewCam;

            GameObject cameraTarget = target.GetCameraTarget();
                        
            CinemachineCamera cinemachineCamera = currentCam.GetComponent<CinemachineCamera>();

            cinemachineCamera.Target.TrackingTarget = cameraTarget.transform;
            cinemachineCamera.Target.CustomLookAtTarget = false;            

            cinemachineCamera.Prioritize();
        }

        public Transform GetCurrentCamTransform()
        {
            return currentCam.transform;
        }

        public void Start()
        {
            defaultCam.SetActive(true);
            topDownCam.SetActive(true);
            detailedViewCam.SetActive(true);
        }
        
    }
}
