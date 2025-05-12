using Assets.Scripts;
using Assets.Scripts.Managers;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;


namespace Assets.Scripts.Builders
{
    /// <summary>
    /// Moves a highlight object on a line that goes from the last line element position in the direction of riding. <br/>
    /// Positions the highlight based on where the mouse is pointing on the terrain. Draws a line from the last line element to the highlight<br/>
    /// and shows distance from the line endpoint to the highlight.<br/>
    /// </summary>
    /// <remarks>Here, the highlight is the TakeoffBuilder mesh which is <b>attached to the same GameObject</b> as this highlighter.</remarks>
    public class TakeoffPositionHighlighter : Highlighter
    {
        // the minimum and maximum distances between the last line element and new obstacle
        [Header("Build bounds")]
        [Tooltip("The minimum distance between the last line element and the new obstacle.")]
        public float minBuildDistance = 1;
        [Tooltip("The maximum distance between the last line element and the new obstacle.")]
        public float maxBuildDistance = 30;

        private TakeoffBuilder builder;

        InputAction clickAction;

        bool canMoveHighlight = true;


        public override void OnEnable()
        {
            base.OnEnable();


            clickAction = InputSystem.actions.FindAction("Select");

            builder = gameObject.GetComponent<TakeoffBuilder>();

            builder.SetRideDirection(lastLineElement.GetRideDirection());

            // position the highlight at minimal build distance from the last line element
            transform.position = lastLineElement.GetEndPoint() + (minBuildDistance + builder.GetCurrentRadiusLength() + 1) * lastLineElement.GetRideDirection();

            UpdateLineRenderer();

            GetComponent<MeshRenderer>().enabled = true;

            canMoveHighlight = true;
        }

        protected override void FixedUpdate()
        {
            if (canMoveHighlight)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayerMask))
                {
                    validHighlightPosition = MoveHighlightToProjectedHitPoint(hit.point);
                }
            }
        }

        public void UpdateLineRenderer()
        {
            // draw a line between the current line end point and the point where the mouse is pointing
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, lastLineElement.GetEndPoint());
            lineRenderer.SetPosition(1, builder.GetStartPoint());
        }

        public void UpdateMeasureText()
        {            
            textMesh.GetComponent<TextMeshPro>().text = $"Distance: {builder.GetDistanceFromPreviousLineElement():F2}m";
            textMesh.GetComponent<TextMeshPro>().text += $"\nEntry speed: {PhysicsManager.MsToKmh(builder.EntrySpeed):F2}km/h";
        }

        public override void OnClick(InputAction.CallbackContext context)
        {            
            if (!isPointerOverUI)
            {
                canMoveHighlight = !canMoveHighlight;
            }
        }

        /// <summary>
        /// Moves the highlight, its distance text and the line renderer to the point where the mouse is pointing on the terrain.
        /// </summary>
        /// <param name="hit">The hitpoint on the terrain where the mouse points</param>
        /// <returns>True if the move destination is valid</returns>
        public override bool MoveHighlightToProjectedHitPoint(Vector3 hit)
        {
            Vector3 endPoint = lastLineElement.GetEndPoint();
            Vector3 rideDirection = lastLineElement.GetRideDirection();


            // project the hit point on a line that goes from the last line element position in the direction of riding
            Vector3 projectedHitPoint = Vector3.Project(hit - endPoint, rideDirection) + endPoint;

            Vector3 toHit = projectedHitPoint - endPoint;

            // if the projected point is not in front of the last line element, return
            if (toHit.normalized != rideDirection.normalized)
            {
                return false;
            }

            float distanceToStartPoint = Vector3.Distance(projectedHitPoint, endPoint) - builder.GetCurrentRadiusLength();

            // if the projected point is too close to the last line element or too far from it, return
            if (distanceToStartPoint < minBuildDistance ||
                distanceToStartPoint > maxBuildDistance)
            {
                return false;
            }

            builder.SetPosition(projectedHitPoint);

            UpdateOnSlopeMessage(builder.GetStartPoint());

            // make the text go along the line and lay flat on the terrain
            float camDistance = CameraManager.Instance.GetTDCamDistance();
            textMesh.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, camDistance)), 
                Quaternion.LookRotation(-Vector3.up, Vector3.Cross(toHit, Vector3.up)));

            UpdateLineRenderer();

            return true;            
        }
    }
}
