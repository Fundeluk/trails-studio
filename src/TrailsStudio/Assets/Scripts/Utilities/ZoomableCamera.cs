
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Utilities
{
    /// <summary>
    /// Manipulates FOV of a <see cref="CinemachineCamera"/> to zoom in and out using user input.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public class ZoomableCamera : MonoBehaviour
    {
        [SerializeField] CinemachineCamera cam;
        [SerializeField] float zoomSpeed = 1f;
        [SerializeField] float minZoom = 20f;
        [SerializeField] float maxZoom = 100f;

        InputAction zoomAction;

        private void OnEnable()
        {
            zoomAction = InputSystem.actions.FindAction("Zoom");
            zoomAction.performed += OnZoom;
        }

        private void OnDisable()
        {
            zoomAction.performed -= OnZoom;
        }

        void OnZoom(InputAction.CallbackContext context)
        {
            if (cam == null) return;
            float zoomChange = context.ReadValue<float>() * zoomSpeed * Time.deltaTime;
            float finalZoom = Mathf.Clamp(cam.Lens.FieldOfView + zoomChange, minZoom, maxZoom);
            cam.Lens.FieldOfView = finalZoom;
        }
    }
}