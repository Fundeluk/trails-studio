using System.Collections;
using UnityEngine;
using Assets.Scripts;
using Assets.Scripts.Utilities;
using UnityEngine.InputSystem;
using TMPro;
using Unity.VisualScripting;
using Assets.Scripts.States;
using UnityEngine.Rendering.Universal;
using Assets.Scripts.Managers;
using UnityEngine.EventSystems;


namespace Assets.Scripts.Builders
{
    /// <summary>
    /// Moves a highlight object anywhere after the last line element based on user input. <br/>
    /// Positions the highlight based on where the mouse is pointing on the terrain. Draws a line from the last line element to the highlight<br/>
    /// and shows distance from the line endpoint to the highlight + the angle between the last line elements ride direction and the line.<br/>
    /// </summary>
    /// <remarks>Here, the highlight is the LandingBuilder mesh which is <b>attached to the same GameObject</b> as this highlighter.</remarks>
    public class LandingPositionHighlighter : Highlighter
    {
        // the minimum and maximum distances between the last line element and new obstacle
        [Header("Build bounds")]
        [Tooltip("The minimum distance between the last line element and the new obstacle.")]
        public float minBuildDistance = 0.5f;
        [Tooltip("The maximum distance between the last line element and the new obstacle.")]
        public float maxBuildDistance = 15;

        private LandingBuilder builder;


        public override void Initialize()
        {
            base.Initialize();
            builder = gameObject.GetComponent<LandingBuilder>();
            builder.Initialize();
            builder.SetRideDirection(lastLineElement.GetRideDirection());

            float startToEdge = (builder.transform.position - builder.GetStartPoint()).magnitude;

            transform.position = lastLineElement.GetEndPoint() + (minBuildDistance + startToEdge) * lastLineElement.GetRideDirection();

            GetComponent<MeshRenderer>().enabled = true;
        }

        public override void OnHighlightClicked(InputAction.CallbackContext context)
        {
            if (validHighlightPosition && !EventSystem.current.IsPointerOverGameObject()) // if the mouse is not over a UI element
            {
                Debug.Log("clicked to build landing. in gridhighlighter now.");
                StateController.Instance.ChangeState(new LandingBuildState(builder));
            }
        }

        public override bool MoveHighlightToProjectedHitPoint(Vector3 hit)
        {
            
            Vector3 toHit = hit - lastLineElement.GetTransform().position;
            float distanceToStartPoint = Vector3.Distance(hit, lastLineElement.GetEndPoint()) - Vector3.Distance(builder.transform.position, builder.GetStartPoint());

            // prevent the landing from being placed behind the takeoff
            float projection = Vector3.Dot(toHit, lastLineElement.GetRideDirection());
            if (projection < 0 || distanceToStartPoint > maxBuildDistance || distanceToStartPoint < minBuildDistance)
            {
                return false;
            }

            // place the highlight at the hit point
            builder.SetPosition(hit);

            UpdateOnSlopeMessage(builder.GetEndPoint());            

            // rotate the highlight along y axis to match the toHit vector's direction
            builder.SetRideDirection(toHit);

            // make the text go along the line and lay flat on the terrain
            float camDistance = CameraManager.Instance.GetTDCamDistance();
            textMesh.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, camDistance)), Quaternion.LookRotation(-Vector3.up, Vector3.Cross(toHit, Vector3.up)));
            textMesh.GetComponent<TextMeshPro>().text = $"Distance: {distanceToStartPoint:F2}m\nAngle: {(int)Vector3.SignedAngle(lastLineElement.GetRideDirection(), toHit, Vector3.up):F2}°";

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, lastLineElement.GetTransform().position + 0.1f * Vector3.up);
            lineRenderer.SetPosition(1, builder.GetStartPoint());

            return true;            
        }
    }
}
