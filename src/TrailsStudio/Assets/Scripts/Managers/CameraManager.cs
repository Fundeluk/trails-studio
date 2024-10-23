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
