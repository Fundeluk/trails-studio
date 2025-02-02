using Assets.Scripts;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;


/// <summary>
/// Moves a highlight object on a line that goes from the last line element position in the direction of riding.
/// Positions the highlight based on where the mouse is pointing on the terrain. Draws a line from the last line element to the highlight
/// and shows distance from the line endpoint to the highlight.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class TakeOffPositionHighlighter : Highlighter
{   

    // the minimum and maximum distances between the last line element and new obstacle
    [Header("Build bounds")]
    [Tooltip("The minimum distance between the last line element and the new obstacle.")]
    public float minBuildDistance = 1;
    [Tooltip("The maximum distance between the last line element and the new obstacle.")]
    public float maxBuildDistance = 30;

    
    public override void OnHighlightClicked(InputAction.CallbackContext context)
    {
        if (validHighlightPosition)
        {
            Debug.Log("clicked to build takeoff. in gridhighlighter now.");
            StateController.Instance.ChangeState(new TakeOffBuildState(highlight.transform.position));
        }
    }

    /// <summary>
    /// Moves the highlight, its distance text and the line renderer to the point where the mouse is pointing on the terrain.
    /// </summary>
    /// <param name="hit">The hitpoint on the terrain where the mouse points</param>
    /// <returns>True if the move destination is valid</returns>
    public override bool MoveHighlightToProjectedHitPoint(RaycastHit hit)
    {
        // if the mouse is pointing at the terrain
        if (hit.collider.gameObject.TryGetComponent<Terrain>(out var _))
        {
            Vector3 hitPoint = hit.point;

            Vector3 endPoint = lastLineElement.GetEndPoint();
            Vector3 rideDirection = lastLineElement.GetRideDirection();


            // project the hit point on a line that goes from the last line element position in the direction of riding
            Vector3 projectedHitPoint = Vector3.Project(hitPoint - endPoint, rideDirection) + endPoint;

            Vector3 toHit = projectedHitPoint - endPoint;

            // if the projected point is not in front of the last line element, return
            if (toHit.normalized != rideDirection.normalized)
            {
                return false;
            }

            // if the projected point is too close to the last line element or too far from it, return
            if (toHit.magnitude < minBuildDistance ||
                toHit.magnitude > maxBuildDistance)
            {
                return false;
            }

            // place the highlight a little above the terrain so that it does not clip through
            highlight.transform.position = new Vector3(projectedHitPoint.x, projectedHitPoint.y, projectedHitPoint.z);

            float distance = Vector3.Distance(projectedHitPoint, endPoint);

            // position the text in the middle of the screen
            distanceMeasure.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, Line.baseHeight));

            // make the text go along the line and lay flat on the terrain
            distanceMeasure.transform.rotation = Quaternion.LookRotation(-Vector3.up, Vector3.Cross(toHit, Vector3.up));           

            distanceMeasure.GetComponent<TextMeshPro>().text = $"Distance: {distance:F2}m";

            // draw a line between the current line end point and the point where the mouse is pointing
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, endPoint + 0.1f * Vector3.up);
            lineRenderer.SetPosition(1, projectedHitPoint - rideDirection * highlight.GetComponent<DecalProjector>().size.x/2 + 0.1f * Vector3.up);

            return true;

        }
        else { return false; }
    }
}
