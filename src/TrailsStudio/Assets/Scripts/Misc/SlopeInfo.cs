using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

namespace Assets.Scripts.Utilities
{
    public class SlopeInfo : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI fieldNames;

        [SerializeField]
        TextMeshProUGUI fieldValues;

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

        public void SetSlopeInfo(List<string> names, List<string> values)
        {
            if (names.Count != values.Count)
            {
                throw new ArgumentException("Names and values lists must have the same Length.");
            }
            fieldNames.text = string.Join(":\n", names);
            fieldValues.text = string.Join("\n", values);
        }


    }
}