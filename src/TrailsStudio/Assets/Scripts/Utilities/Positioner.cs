using Assets.Scripts.Builders;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
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
    /// Derived classes should implement the logic for positioning an obstacle to the desired position and initialize the highlighter.
    /// As the obstacle may differ across derived classes, this class does not work with it directly.
    /// </summary>
    /// <remarks>This script and any class that derives from it is supposed to be used by attaching it to the same GameObject where the component representing the obstacle is.<br/>
    /// Together with an instance of this class, a UI derived from <see cref="PositionUI"/> needs to be active as well.</remarks>
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

        protected PositionUI positionUI;

        // create a layer mask for the raycast so that it ignores all layers except the terrain
        protected LayerMask terrainLayerMask;

        bool _canMoveHighlight = true;

        protected bool CanMoveHighlight {
            get => _canMoveHighlight;

            set
            {
                positionUI.ToggleAnchorIcon(!value);
                _canMoveHighlight = value;
            }
        }

        protected IBuilder baseBuilder;

        public virtual void OnClick(InputAction.CallbackContext context)
        {
            if (!isPointerOverUI)
            {
                CanMoveHighlight = !CanMoveHighlight;
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

            positionUI = UIManager.Instance.CurrentUI.GetComponent<PositionUI>();
            CanMoveHighlight = _canMoveHighlight;
        }

        protected virtual void Update()
        {
            isPointerOverUI = EventSystem.current.IsPointerOverGameObject();
        }

        protected virtual void FixedUpdate()
        {
            if (CanMoveHighlight)
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