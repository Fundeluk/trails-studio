using Assets.Scripts.Managers;
using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

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

        public static Quaternion GetRotationForDirection(Vector3 direction)
        {
            Vector3 newRideDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            Vector3 rideDirNormal = Vector3.Cross(newRideDirection, Vector3.up).normalized;
            return Quaternion.LookRotation(-Vector3.up, rideDirNormal);
        }

        public static void UpdateHighlight(GameObject highlight, float length, Vector3 position, Vector3 direction)
        {
            if (length == 0)
            {
                highlight.SetActive(false);
                return;
            }
            else
            {
                highlight.SetActive(true);
            }

            Quaternion newRot = Highlighter.GetRotationForDirection(direction);
            highlight.transform.SetPositionAndRotation(position, newRot);

            DecalProjector projector = highlight.GetComponent<DecalProjector>();
            projector.size = new Vector3(length, Line.Instance.GetLastLineElement().GetBottomWidth(), 10);
        }

        protected virtual void Initialize()
        {
            lastLineElement = Line.Instance.GetLastLineElement();

            // if highlight is not positioned somewhere in front of the last line element, move it there
            if ((highlight.transform.position - lastLineElement.GetEndPoint()).normalized != lastLineElement.GetRideDirection().normalized)
            {
                highlight.transform.position = lastLineElement.GetEndPoint() + lastLineElement.GetRideDirection().normalized;
            }

            highlight.transform.rotation = GetRotationForDirection(lastLineElement.GetRideDirection());

            // disable the visual elements to prevent them from flashing when script is enabled
            highlight.SetActive(false);
            lineRenderer.enabled = false;
            distanceMeasure.SetActive(false);

            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
            lineRenderer.material.color = Color.black;            
            InputSystem.actions.FindAction("Select").performed += OnHighlightClicked;
        }

        private void OnEnable()
        {
            Initialize();
        }        

        // Update is called once per frame
        void FixedUpdate()
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (Line.Instance.activeSlopeChange != null)
                {
                    if (Line.Instance.activeSlopeChange.IsOnSlope(hit.point))
                    {
                        UIManager.Instance.ShowOnSlopeMessage();
                    }
                    else
                    {
                        UIManager.Instance.HideOnSlopeMessage();
                    }
                }
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

        protected void OnDisable()
        {
            InputSystem.actions.FindAction("Select").performed -= OnHighlightClicked;
            highlight.SetActive(false);
            distanceMeasure.SetActive(false);
            lineRenderer.enabled = false;
        }
    }
}