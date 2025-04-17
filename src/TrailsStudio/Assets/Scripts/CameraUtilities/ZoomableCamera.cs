
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manipulates FOV of a <see cref="CinemachineCamera"/> to zoom in and out using user input.
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public class ZoomableCamera : MonoBehaviour
{
    [SerializeField] CinemachineCamera cam;
    [SerializeField] float zoomSpeed = 80f;
    [SerializeField] float minZoom = 20f;
    [SerializeField] float maxZoom = 85f;

    [SerializeField]
    float defaultFOV = 60f;

    public InputAction ZoomAction { get; private set; }

    private void OnEnable()
    {
        cam.Lens.FieldOfView = defaultFOV;

        ZoomAction = InputSystem.actions.FindAction("Zoom");
        ZoomAction.performed += OnZoom;
    }

    private void OnDisable()
    {
        ZoomAction.performed -= OnZoom;
    }

    void OnZoom(InputAction.CallbackContext context)
    {
        if (cam == null) return;
        Vector2 rawInput = context.ReadValue<Vector2>();
        float zoomChange = rawInput.x * zoomSpeed * Time.deltaTime;
        float finalZoom = Mathf.Clamp(cam.Lens.FieldOfView + zoomChange, minZoom, maxZoom);
        cam.Lens.FieldOfView = finalZoom;
    }
}
