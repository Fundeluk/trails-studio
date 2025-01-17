using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Utilities
{
    //TODO implement physics model for calculating allowed distance from takeoff


    [RequireComponent(typeof(LineRenderer))]
    public abstract class Highlighter : MonoBehaviour
    {
        public GameObject highlight;
        public LineRenderer lineRenderer;
        public GameObject distanceMeasure;

        protected bool validHighlightPosition = false;

        protected ILineElement lastLineElement;

        public abstract void OnHighlightClicked(InputAction.CallbackContext context);

        public abstract bool MoveHighlightToProjectedHitPoint(RaycastHit hit);

        protected void RotateHighlightToDirection(Vector3 direction)
        {
            Vector3 newRideDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            Vector3 rideDirNormal = Vector3.Cross(newRideDirection, Vector3.up).normalized;
            highlight.transform.rotation = Quaternion.LookRotation(-Vector3.up, rideDirNormal);
        }

        private void Initialize()
        {
            lastLineElement = Line.Instance.line[^1];

            // if highlight is not positioned somewhere in front of the last line element, move it there
            if ((highlight.transform.position - lastLineElement.GetEndPoint()).normalized != lastLineElement.GetRideDirection().normalized)
            {
                highlight.transform.position = lastLineElement.GetEndPoint() + lastLineElement.GetRideDirection().normalized;
            }

            RotateHighlightToDirection(lastLineElement.GetRideDirection());

            // disable the visual elements to prevent them from flashing when script is enabled
            highlight.SetActive(false);
            lineRenderer.enabled = false;
            distanceMeasure.SetActive(false);

            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
            lineRenderer.material.color = Color.black;
            InputSystem.actions.FindAction("Click").performed += OnHighlightClicked;
        }

            // Use this for initialization
        void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }        

        // Update is called once per frame
        void Update()
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                validHighlightPosition = MoveHighlightToProjectedHitPoint(hit);
                if (!validHighlightPosition)
                {
                    return;
                }

                if (!highlight.activeSelf)
                {
                    highlight.SetActive(true);
                }
                if (!distanceMeasure.activeSelf)
                {
                    distanceMeasure.SetActive(true);
                }
                if (!lineRenderer.enabled)
                {
                    lineRenderer.enabled = true;
                }
            }
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction("Click").performed -= OnHighlightClicked;
            highlight.SetActive(false);
            distanceMeasure.SetActive(false);
            lineRenderer.enabled = false;
        }
    }
}