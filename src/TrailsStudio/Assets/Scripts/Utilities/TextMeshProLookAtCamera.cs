using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace Assets.Scripts.Utilities
{
    public class TextMeshProLookAtCamera : MonoBehaviour
    {
        Transform cam;

        private void Awake()
        {
            cam = Camera.main.transform;
        }

        private void OnEnable()
        {
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
        }
        

        private void OnDisable()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
        }

        void OnCameraUpdated(CinemachineBrain cmBrain)
        {
            transform.forward = cam.forward;
        }
    }
}