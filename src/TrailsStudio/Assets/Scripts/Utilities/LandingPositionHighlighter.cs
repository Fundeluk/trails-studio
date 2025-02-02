using System.Collections;
using UnityEngine;
using Assets.Scripts;
using Assets.Scripts.Utilities;
using UnityEngine.InputSystem;
using TMPro;
using Unity.VisualScripting;
using Assets.Scripts.States;
using UnityEngine.Rendering.Universal;

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

        // prevent the landing from being placed behind the takeoff
        float projection = Vector3.Dot(toHit, lastLineElement.GetRideDirection());
        if (projection < 0 || toHit.magnitude > 10)
        {
            return false;
        }

        // place the highlight at the hit point
        highlight.transform.position = hit.point;

        // rotate the highlight along y axis to match the toHit vector's direction
        RotateHighlightToDirection(toHit);

        distanceMeasure.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, Line.baseHeight));
        //distanceMeasure.transform.position = Vector3.Lerp(lastLineElement.GetEndPoint(), hit.point, Line.baseHeight);

        // make the text go along the line and lay flat on the terrain
        distanceMeasure.transform.rotation = Quaternion.LookRotation(-Vector3.up, Vector3.Cross(toHit, Vector3.up));        

        distanceMeasure.GetComponent<TextMeshPro>().text = $"Distance: {toHit.magnitude:F2}m\nAngle: {(int)Vector3.SignedAngle(lastLineElement.GetRideDirection(), toHit, Vector3.up):F2}°";

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, lastLineElement.GetTransform().position + 0.1f * Vector3.up);
        lineRenderer.SetPosition(1, hit.point + 0.1f * Vector3.up);

        return true;
    }
}
