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
        public GameObject sideViewCam;

        private GameObject currentCam;

        /// <summary>
        /// Positions the camera to look at the last obstacle in line from a top-down view.
        /// </summary>
        public void TopDownView()
        {
            ILineElement lastObstacle = Line.Instance.line[^1];           

            var desiredPosition = new Vector3(lastObstacle.GetEndPoint().x, 25 + lastObstacle.GetHeight(), lastObstacle.GetEndPoint().z);
                       
            topDownCam.transform.position = desiredPosition;

            
            // set default rotation
            topDownCam.transform.rotation = new Quaternion(0 , 0, 0, 0);

            // rotate the cam so that it looks straight down and its right vector is aligned with the ride direction
            topDownCam.transform.right = lastObstacle.GetRideDirection();
            topDownCam.transform.Rotate(90, 0, 0);

            // change view to this camera
            topDownCam.GetComponent<CinemachineCamera>().Prioritize();
            currentCam = topDownCam;
        }

        /// <summary>
        /// Positions the camera to look at the target from a top-down view.
        /// </summary>
        /// <param name="target">The target to focus on</param>
        public void TopDownFollowHighlight()
        {
            ILineElement lastObstacle = Line.Instance.line[^1];

            // set default rotation
            topDownCam.transform.rotation = new Quaternion(0, 0, 0, 0);

            // rotate the cam so that it looks straight down and its right vector is aligned with the ride direction
            topDownCam.transform.right = lastObstacle.GetRideDirection();
            topDownCam.transform.Rotate(90, 0, 0);

            CinemachinePositionComposer composer = topDownCam.GetComponent<CinemachinePositionComposer>();
            composer.CameraDistance = lastObstacle.GetHeight() + 25;

            topDownCam.GetComponent<CinemachineCamera>().Prioritize();
            currentCam = topDownCam;
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

        public void SideView(ILineElement target)
        {
            ILineElement lastObstacle = Line.Instance.line[^1];

            Vector3 rideDirection = lastObstacle.GetRideDirection();
            Vector3 rideDirectionNormal = Vector3.Cross(rideDirection, Vector3.up).normalized;

            GameObject cameraTarget = target.GetCameraTarget();
            
            sideViewCam.transform.position = cameraTarget.transform.position + rideDirectionNormal * -8 + Vector3.up * 2;
            
            CinemachineCamera cinemachineCamera = sideViewCam.GetComponent<CinemachineCamera>();

            CinemachineHardLookAt cinemachineHardLookAt = sideViewCam.GetComponent<CinemachineHardLookAt>();
            //cinemachineHardLookAt.LookAtOffset

            sideViewCam.transform.LookAt(cameraTarget.transform);
            cinemachineCamera.LookAt = cameraTarget.transform;
            

            cinemachineCamera.Prioritize();


            currentCam = sideViewCam;
        }

        public Transform GetCurrentCamTransform()
        {
            return currentCam.transform;
        }

        public void Start()
        {
            defaultCam.SetActive(true);
            topDownCam.SetActive(true);
            sideViewCam.SetActive(true);
        }
        
    }
}
