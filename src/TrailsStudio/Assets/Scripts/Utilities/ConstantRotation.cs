﻿using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Utilities
{
    public class ConstantRotation : MonoBehaviour
    {
        public float rotationSpeed = 10f;
        public GameObject target;


        // Update is called once per frame
        void Update()
        {
            if (target == null)
            {                
                target = Line.Instance.GetLastLineElement().GetCameraTarget();
            }

            transform.RotateAround(target.transform.position, Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}