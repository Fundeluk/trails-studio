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
    /// Derived classes should implement the logic for moving the highlight to the desired position, provide the callback for the user clicking on the highlight and initialize the highlighter.<br/>
    /// As the objects representing the highlight may differ across derived classes, this class does not work with the highlight object directly.
    /// </summary>
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
        public abstract bool MoveHighlightToProjectedHitPoint(RaycastHit hit);

        /// <summary>
        /// Initializes visual elements (highlight included) but disables them before the user does something.<br/>
        /// Assigns the on click callback method.
        /// </summary>
        public virtual void Initialize()
        {
            lastLineElement = Line.Instance.GetLastLineElement();            

            // disable the visual elements to prevent them from flashing when script is enabled
            lineRenderer.enabled = false;
            textMesh.SetActive(false);

            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
            lineRenderer.material.color = Color.black;            
            InputSystem.actions.FindAction("Select").performed += OnHighlightClicked;
        }

        private void OnEnable()
        {
            Initialize();
        }        

        // Update is called once per frame
        protected virtual void FixedUpdate()
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {                
                validHighlightPosition = MoveHighlightToProjectedHitPoint(hit);

                //if (!validHighlightPosition)
                //{
                //    return;
                //}

                //if (!highlight.activeSelf)
                //{
                //    highlight.SetActive(true);
                //}
                //if (!textMesh.activeSelf)
                //{
                //    textMesh.SetActive(true);
                //}
                //if (!lineRenderer.enabled)
                //{
                //    lineRenderer.enabled = true;
                //}
            }
        }

        protected virtual void OnDisable()
        {
            InputSystem.actions.FindAction("Select").performed -= OnHighlightClicked;
            textMesh.SetActive(false);
            lineRenderer.enabled = false;
        }
    }
}