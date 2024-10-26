using Assets.Scripts.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class CameraManager : Singleton<CameraManager>
    {
        [Header("Camera component")]
        public Camera mainCamera;

        [Header("Virtual cameras")]
        public Cinemachine.CinemachineVirtualCamera defaultCam;
        public Cinemachine.CinemachineVirtualCamera topDownCam;

        public void TopDownView()
        {

            // TODO update the position so that it has the last line element at the center of the screen (or is bounded by edge of the terrain)
            topDownCam.transform.position = new Vector3(Line.Instance.currentLineEndPoint.x, 25, Line.Instance.currentLineEndPoint.z);

            // set default rotation
            topDownCam.transform.rotation = new Quaternion(0 , 0, 0, 0);

            // rotate the cam so that it looks straight down and its right vector is aligned with the ride direction
            topDownCam.transform.right = Line.Instance.currentRideDirection;
            topDownCam.transform.Rotate(90, 0, 0);

            // change view to this camera
            topDownCam.MoveToTopOfPrioritySubqueue();
        }

        public void DefaultView()
        {
            defaultCam.MoveToTopOfPrioritySubqueue();
        }

        public Transform GetTopDownCamTransform()
        {
            return topDownCam.transform;
        }
    }
}
