using Assets.Scripts.Builders;
using Assets.Scripts.Managers;
using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Utilities
{
    /// <summary>
    /// Base class for positioning an element during its build phase.<br/>
    /// Derived classes should implement the logic for moving the highlight to the desired position, provide the callback for the user clicking on the highlight<br/>
    /// and initialize the highlighter.<br/>
    /// As the objects representing the highlight may differ across derived classes, this class does not work with the highlight object directly.
    /// </summary>
    /// <remarks>This script and any class that derives from it is supposed to be used by attaching it to the same GameObject where the component representing the highlight is.</remarks>
    [RequireComponent(typeof(LineRenderer))]
    public abstract class Positioner : MonoBehaviour
    {
        /// <summary>
        /// Component used for drawing a line from the last line element to the highlight.
        /// </summary>
        [SerializeField]
        protected LineRenderer lineRenderer;

        [SerializeField]
        GameObject textMeshPrefab;

        /// <summary>
        /// GameObject used for displaying various information during highlighting to the user.
        /// </summary>
        protected GameObject textMesh;

        protected bool isPointerOverUI = false;

        protected ILineElement lastLineElement;

        // create a layer mask for the raycast so that it ignores all layers except the terrain
        protected LayerMask terrainLayerMask;

        protected bool canMoveHighlight = true;

        protected IBuilder baseBuilder;

        public virtual void OnClick(InputAction.CallbackContext context)
        {
            if (!isPointerOverUI)
            {
                canMoveHighlight = !canMoveHighlight;
            }
        }

        /// <summary>
        /// Moves the highlight to the point where the raycast hit the ground.
        /// </summary>
        /// <returns>Whether the supposed new highlight position is valid.</returns>
        public abstract bool TrySetPosition(Vector3 position);
        
        /// <summary>
        /// Initializes visual elements and assigns the on click callback method.
        /// </summary>
        public virtual void OnEnable()
        {
            lastLineElement = Line.Instance.GetLastLineElement();

            float camDistance = CameraManager.Instance.GetTDCamDistance();
            textMesh = Instantiate(textMeshPrefab, Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, camDistance)),
                                            Quaternion.LookRotation(-Vector3.up, Vector3.Cross(Line.Instance.GetCurrentRideDirection(), Vector3.up)));

            InputSystem.actions.FindAction("Select").performed += OnClick;

            lineRenderer.enabled = true;
            textMesh.SetActive(true);
        }

        protected virtual void Update()
        {
            isPointerOverUI = EventSystem.current.IsPointerOverGameObject();
        }

        protected virtual void FixedUpdate()
        {
            if (canMoveHighlight)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayerMask))
                {
                    bool success = TrySetPosition(hit.point);
                    baseBuilder.CanBuild(success);
                }
            }
        }

        protected virtual void OnDisable()
        {
            InputSystem.actions.FindAction("Select").performed -= OnClick;
            Destroy(textMesh);
            lineRenderer.enabled = false;
        }

        protected virtual void Awake()
        {
            terrainLayerMask = LayerMask.GetMask("Terrain");
        }        
    }
}