﻿using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using Assets.Scripts.States;
using TMPro;
using UnityEngine.Rendering.Universal;
using Assets.Scripts.Managers;

namespace Assets.Scripts.Utilities
{
	[RequireComponent(typeof(LineRenderer))]
    public class SlopePositionHighlighter : Highlighter
	{
        // the minimum and maximum distances between the last line element and new obstacle
        [Header("Build bounds")]
        [Tooltip("The minimum distance between the last line element and the new obstacle.")]
        public float minBuildDistance = 0;
        [Tooltip("The maximum distance between the last line element and the new obstacle.")]
        public float maxBuildDistance = 30;

        enum SlopePositionState
        {
            Start,
            End
        }

        SlopePositionState state = SlopePositionState.Start;
        Vector3 startPosition;
        Vector3 endPosition;
        Vector3 lastValidHitPoint;


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

                lastValidHitPoint = projectedHitPoint;

                if (state == SlopePositionState.Start)
                {
                    highlight.transform.position = new Vector3(projectedHitPoint.x, projectedHitPoint.y, projectedHitPoint.z);

                    float distance = Vector3.Distance(projectedHitPoint, endPoint);

                    // position the text in the middle of the screen

                    // make the text go along the line and lay flat on the terrain
                    distanceMeasure.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, Line.baseHeight)), Quaternion.LookRotation(-Vector3.up, Vector3.Cross(toHit, Vector3.up)));
                    distanceMeasure.GetComponent<TextMeshPro>().text = $"Distance: {distance:F2}m";

                    // draw a line between the current line end point and the point where the mouse is pointing
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, endPoint + 0.1f * Vector3.up);
                    lineRenderer.SetPosition(1, projectedHitPoint - rideDirection * highlight.GetComponent<DecalProjector>().size.x / 2 + 0.1f * Vector3.up);

                    return true;
                }
                else
                {
                    highlight.transform.position = Vector3.Lerp(startPosition, projectedHitPoint, 0.5f);

                    float length = Vector3.Distance(startPosition, projectedHitPoint);

                    // position the text in the middle of the screen

                    // make the text go along the line and lay flat on the terrain
                    distanceMeasure.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, Line.baseHeight)), Quaternion.LookRotation(-Vector3.up, Vector3.Cross(toHit, Vector3.up)));
                    distanceMeasure.GetComponent<TextMeshPro>().text = $"Length: {length:F2}m";

                    float width = lastLineElement.GetWidth() + 2 * lastLineElement.GetHeight() * 0.2f;

                    DecalProjector decalProjector = highlight.GetComponent<DecalProjector>();
                    decalProjector.size = new Vector3(length, width, 10);

                    return true;
                }
            }
            else { return false; }
        }

        public override void OnHighlightClicked(InputAction.CallbackContext context)
        {
            Debug.Log("highlight clicked");
            if (validHighlightPosition)
            {
                if (state == SlopePositionState.End)
                {
                    Debug.Log("state end: clicked to build slope end. in slopehighlighter now.");
                    endPosition = lastValidHitPoint;
                    SlopeChange change = new (startPosition, endPosition, 0, lastLineElement.GetBottomWidth());
                    TerrainManager.Instance.AddSlopeChange(change);
                    StateController.Instance.ChangeState(new SlopeBuildState(change));
                    return;
                }
                else if (state == SlopePositionState.Start)
                {
                    Debug.Log("state start: clicked to build slope start. in slopehighlighter now.");
                    startPosition = lastValidHitPoint;
                    state = SlopePositionState.End;
                    return;
                }
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            state = SlopePositionState.Start;
           
            float width = lastLineElement.GetWidth() + 2 * lastLineElement.GetHeight() * 0.2f;

            DecalProjector decalProjector = highlight.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(0.5f, width, 10);
        }        

        private new void OnDisable()
        {
            base.OnDisable();

            DecalProjector projector = highlight.GetComponent<DecalProjector>();
            projector.size = new Vector3(3, 3, 10);

        }
    }
}