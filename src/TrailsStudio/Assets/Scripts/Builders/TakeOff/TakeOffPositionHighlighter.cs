using Assets.Scripts;
using Assets.Scripts.Managers;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using System.Collections;
using System.Collections.Generic;
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
        // TODO enforce minBuildDistance from last line endpoint to this takeoffs startpoint!

        // the minimum and maximum distances between the last line element and new obstacle
        [Header("Build bounds")]
        [Tooltip("The minimum distance between the last line element and the new obstacle.")]
        public float minBuildDistance = 1;
        [Tooltip("The maximum distance between the last line element and the new obstacle.")]
        public float maxBuildDistance = 30;

        private TakeoffBuilder builder;

        public override void Initialize()
        {
            base.Initialize();
            builder = gameObject.GetComponent<TakeoffBuilder>();          
            builder.Initialize();

            builder.SetRideDirection(lastLineElement.GetRideDirection());

            // position the highlight at minimal build distance from the last line element
            transform.position = lastLineElement.GetEndPoint() + (minBuildDistance + builder.GetCurrentRadiusLength()) * lastLineElement.GetRideDirection();

            GetComponent<MeshRenderer>().enabled = true;
        }
        
        public override void OnHighlightClicked(InputAction.CallbackContext context)
        {
            if (validHighlightPosition && !EventSystem.current.IsPointerOverGameObject()) // if the mouse is not over a UI element
            {
                Debug.Log("clicked to build takeoff. in gridhighlighter now.");
                StateController.Instance.ChangeState(new TakeOffBuildState(builder));
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

            // if the hit point is on a slope, show a message
            if (BuildManager.Instance.activeSlopeChange != null)
            {
                if (BuildManager.Instance.activeSlopeChange.IsOnSlope(builder))
                {
                    UIManager.Instance.ShowOnSlopeMessage();
                }
                else
                {
                    UIManager.Instance.HideOnSlopeMessage();
                }
            }

            // position the text in the middle of the screen

            // make the text go along the line and lay flat on the terrain
            textMesh.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, Line.baseHeight)), Quaternion.LookRotation(-Vector3.up, Vector3.Cross(toHit, Vector3.up)));
            textMesh.GetComponent<TextMeshPro>().text = $"Distance: {distanceToStartPoint:F2}m";

            // draw a line between the current line end point and the point where the mouse is pointing
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, endPoint + 0.1f * Vector3.up);
            lineRenderer.SetPosition(1, builder.GetStartPoint());

            return true;            
        }
    }
}
