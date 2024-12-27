using System.Collections;
using UnityEngine;
using Assets.Scripts;
using Assets.Scripts.Utilities;
using UnityEngine.InputSystem;
using TMPro;
using Unity.VisualScripting;
using Assets.Scripts.States;

[RequireComponent(typeof(LineRenderer))]
public class LandingPositionHighlighter : Highlighter
{
    public override void OnHighlightClicked(InputAction.CallbackContext context)
    {
        if (validHighlightPosition)
        {
            Debug.Log("clicked to build landing. in gridhighlighter now.");
            Vector3 rideDirection = highlight.transform.position - lastLineElement.GetTransform().position;
            StateController.Instance.ChangeState(new LandingBuildState(highlight.transform.position, rideDirection));
        }
    }

    public override bool MoveHighlightToProjectedHitPoint(RaycastHit hit)
    {
        Vector3 toHit = hit.point - lastLineElement.GetTransform().position;
        float projection = Vector3.Dot(toHit, lastLineElement.GetRideDirection());
        if (projection < 0 || toHit.magnitude > 10)
        {
            return false;
        }

        // place the highlight at the hit point
        highlight.transform.position = hit.point;

        // rotate the highlight along y axis to match the toHit vector's direction
        RotateHighlightToDirection(toHit);

        distanceMeasure.transform.position = Vector3.Lerp(lastLineElement.GetEndPoint(), hit.point, 0.5f);

        // make the text go along the line
        distanceMeasure.transform.right = CameraManager.Instance.GetCurrentCamTransform().right;

        // make the text lay flat on the terrain
        distanceMeasure.transform.Rotate(90, 0, 0);

        distanceMeasure.GetComponent<TextMeshPro>().text = $"Distance: {toHit.magnitude:F2}m";

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, lastLineElement.GetTransform().position);
        lineRenderer.SetPosition(1, hit.point);

        return true;
    }
}
