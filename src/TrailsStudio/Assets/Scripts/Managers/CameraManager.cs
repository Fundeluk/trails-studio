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
            LineElement lastObstacle = Line.Instance.line[^1];

            // get terrain bounds
            Terrain terrain = Terrain.activeTerrain;
            Vector3 terrainPos = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;

            var desiredPosition = new Vector3(lastObstacle.endPoint.x, 25 + lastObstacle.height, lastObstacle.endPoint.z);
                       
            topDownCam.transform.position = desiredPosition;

            
            // set default rotation
            topDownCam.transform.rotation = new Quaternion(0 , 0, 0, 0);

            // rotate the cam so that it looks straight down and its right vector is aligned with the ride direction
            topDownCam.transform.right = lastObstacle.rideDirection;
            topDownCam.transform.Rotate(90, 0, 0);

            // change view to this camera
            topDownCam.GetComponent<CinemachineCamera>().Prioritize();
            currentCam = topDownCam;
        }

        /// <summary>
        /// Positions the camera to look at the target from a top-down view.
        /// </summary>
        /// <param name="target">The target to focus on</param>
        public void TopDownFollow(GameObject target)
        {
            LineElement lastObstacle = Line.Instance.line[^1];

            // set default rotation
            topDownCam.transform.rotation = new Quaternion(0, 0, 0, 0);

            // rotate the cam so that it looks straight down and its right vector is aligned with the ride direction
            topDownCam.transform.right = lastObstacle.rideDirection;
            topDownCam.transform.Rotate(90, 0, 0);

            CinemachinePositionComposer composer = topDownCam.GetComponent<CinemachinePositionComposer>();
            composer.CameraDistance = lastObstacle.height + 25;
            topDownCam.GetComponent<CinemachineCamera>().Follow = target.transform;

            topDownCam.GetComponent<CinemachineCamera>().Prioritize();
            currentCam = topDownCam;
        }

        public void DefaultView()
        {
            LineElement lastObstacle = Line.Instance.line[^1];

            // position the camera so that it looks at the last obstacle in line from an angle
            defaultCam.transform.position = lastObstacle.endPoint + new Vector3(5, lastObstacle.height, 5);
            defaultCam.GetComponent<CinemachineCamera>().LookAt = lastObstacle.obstacle;

            defaultCam.GetComponent<CinemachineCamera>().Prioritize();
            currentCam = defaultCam;
        }

        public void SideView(Vector3 targetPosition)
        {
            LineElement lastObstacle = Line.Instance.line[^1];
            Vector3 rideDirectionNormal = Vector3.Cross(lastObstacle.rideDirection, Vector3.up).normalized;

            sideViewCam.transform.position = targetPosition + rideDirectionNormal * 5 + Vector3.up * 2;
            sideViewCam.transform.LookAt(targetPosition);

            sideViewCam.GetComponent<CinemachineCamera>().Prioritize();
            currentCam = sideViewCam;
        }

        public Transform GetTopDownCamTransform()
        {
            return topDownCam.transform;
        }

        public void Start()
        {
            defaultCam.SetActive(true);
            topDownCam.SetActive(true);
            sideViewCam.SetActive(true);
        }
        
    }
}
