using LineSystem;
using Managers;
using Obstacles;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using TerrainEditing;

namespace Misc
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

        protected ILineElement lastLineElement;

        protected PositionUI positionUI;

        protected LayerMask raycastTargetLayerMask;

        /// <summary>
        /// Used to define the width of the area that should be clear before a takeoff or after a landing.
        /// </summary>
        protected static float clearanceWidth = 1.5f;

        private bool canMoveHighlight = true;

        protected bool CanMoveHighlight {
            get => canMoveHighlight;

            set
            {
                canMoveHighlight = value;

                if (positionUI != null)
                {
                    positionUI.ToggleAnchorIcon(!canMoveHighlight);
                }
            }
        }

        protected IBuilder baseBuilder;

        public virtual void OnClick(InputAction.CallbackContext context)
        {
            if (!StudioUIManager.IsPointerOverUI)
            {
                CanMoveHighlight = !CanMoveHighlight;
            }
        }

        /// <summary>
        /// Moves the highlight to the point where the raycast hit the ground.
        /// </summary>
        /// <returns>Whether the supposed new highlight position is valid.</returns>
        protected abstract bool TrySetPosition(Vector3 position);
        
        /// <summary>
        /// Projects the raw mouse position onto the constraint (e.g. line) and gets the endpoint of the obstacle placed on that position.
        /// Defaults to no projection (returns raw point).
        /// </summary>
        protected virtual Vector3 GetProjectedEndPoint(Vector3 rawPoint) => rawPoint;
        
        /// <summary>
        /// Projects the raw mouse position onto the constraint (e.g. line).
        /// Defaults to no projection (returns raw point).
        /// </summary>
        protected virtual Vector3 GetProjectedPoint(Vector3 rawPoint) => rawPoint;

        /// <summary>
        /// Checks if the point is within valid bounds for creating terrain.
        /// Defaults to true.
        /// </summary>
        protected virtual bool IsPositionValid(Vector3 point) => true;

        /// <summary>
        /// Initializes visual elements and assigns the on click callback method.
        /// </summary>
        public virtual void OnEnable()
        {
            lastLineElement = Line.Instance.GetLastLineElement();

            float camDistance = CameraManager.Instance.GetTDCamDistance();
            textMesh = Instantiate(textMeshPrefab,
                Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, camDistance)),
                        Quaternion.LookRotation(-Vector3.up,
                            Vector3.Cross(Line.Instance.GetCurrentRideDirection(), Vector3.up)));

            InputSystem.actions.FindAction("Select").performed += OnClick;

            lineRenderer.enabled = true;
            textMesh.SetActive(true);

            positionUI = StudioUIManager.Instance.CurrentUI.GetComponent<PositionUI>();
            CanMoveHighlight = canMoveHighlight;
        }       
        
        private static Plane groundPlane = new(Vector3.up, Vector3.zero); 

        protected virtual void FixedUpdate()
        {
            if (CanMoveHighlight)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

                if (Physics.Raycast(ray, out RaycastHit hit, TerrainManager.MaxHeight*2, raycastTargetLayerMask))
                {
                    bool success = TrySetPosition(hit.point);
                    baseBuilder.CanBuild(success);
                }
                else if (groundPlane.Raycast(ray, out float enter)) 
                {
                    Vector3 hitPoint = ray.GetPoint(enter);

                    Vector3 projectedPoint = GetProjectedEndPoint(hitPoint);
                    
                    if (IsPositionValid(projectedPoint))
                    {
                        // Create the terrain tile if it doesn't exist
                        TerrainManager.Instance.EnsureTerrainAt(projectedPoint);
                    }
                    
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
            raycastTargetLayerMask = LayerMask.GetMask("Terrain");
        }        
    }
}