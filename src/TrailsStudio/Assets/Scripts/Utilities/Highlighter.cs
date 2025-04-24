using Assets.Scripts.Managers;
using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Utilities
{
    //TODO implement physics model for calculating allowed distance from takeoff

    /// <summary>
    /// Base class for highlighting a position where the user wants to build an element during positioning phase.<br/>
    /// Derived classes should implement the logic for moving the highlight to the desired position, provide the callback for the user clicking on the highlight<br/>
    /// and initialize the highlighter.<br/>
    /// As the objects representing the highlight may differ across derived classes, this class does not work with the highlight object directly.
    /// </summary>
    /// <remarks>This script and any class that derives from it is supposed to be used by attaching it to the same GameObject where the component representing the highlight is.</remarks>
    [RequireComponent(typeof(LineRenderer))]
    public abstract class Highlighter : MonoBehaviour
    {
        /// <summary>
        /// Component used for drawing a line from the last line element to the highlight.
        /// </summary>
        [SerializeField]
        protected LineRenderer lineRenderer;

        /// <summary>
        /// GameObject used for displaying various information during highlighting to the user.
        /// </summary>
        [SerializeField]
        protected GameObject textMesh;

        protected bool validHighlightPosition = false;

        protected ILineElement lastLineElement;

        public abstract void OnHighlightClicked(InputAction.CallbackContext context);

        /// <summary>
        /// Moves the highlight to the point where the raycast hit the ground.
        /// </summary>
        /// <returns>Whether the position of the raycast hit is valid.</returns>
        public abstract bool MoveHighlightToProjectedHitPoint(Vector3 position);

        /// <summary>
        /// Initializes visual elements and assigns the on click callback method.
        /// </summary>
        public virtual void Initialize()
        {
            lastLineElement = Line.Instance.GetLastLineElement();
            
            float camDistance = CameraManager.Instance.GetTDCamDistance();
            textMesh = Instantiate(textMesh, Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, camDistance)), 
                                            Quaternion.LookRotation(-Vector3.up, Vector3.Cross(Line.Instance.GetCurrentRideDirection(), Vector3.up)));
            textMesh.transform.SetParent(transform);

            InputSystem.actions.FindAction("Select").performed += OnHighlightClicked;

            lineRenderer.enabled = true;
            textMesh.SetActive(true);
        }

        public virtual void OnEnable()
        {
            Initialize();
        }
       
        // Update is called once per frame
        protected virtual void FixedUpdate()
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            // create a layer mask for the raycast so that it ignores all layers except the terrain
            int terrainLayerMask = 1 << 6;

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayerMask))
            {                
                validHighlightPosition = MoveHighlightToProjectedHitPoint(hit.point);                
            }
        }

        protected virtual void OnDisable()
        {
            InputSystem.actions.FindAction("Select").performed -= OnHighlightClicked;
            Destroy(textMesh);
            lineRenderer.enabled = false;
        }

        protected static void UpdateOnSlopeMessage(Vector3 position)
        {
            // if the hit point is on a slope, show a message
            if (TerrainManager.Instance.ActiveSlope != null && TerrainManager.Instance.ActiveSlope.IsOnSlope(position))
            {                
                UIManager.Instance.ShowMessage("The obstacle you are building will be placed on a slope.");
                return;                
            }

            UIManager.Instance.HideMessage();
        }
    }
}