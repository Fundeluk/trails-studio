using Assets.Scripts.Managers;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Assets.Scripts.Builders.Slope
{
    /// <summary>
    /// Moves a highlight object based on user input on a line that goes from the last line element position in the direction of riding.<br/>
    /// Measures the distance from the last line element to the highlight and shows it to the user.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class PositionHighlighter : Highlighter
    {
        // the minimum and maximum distances between the last line element and new obstacle
        [Header("Build bounds")]
        [Tooltip("The minimum distance between the last line element and the new obstacle.")]
        public float minBuildDistance = 0;
        [Tooltip("The maximum distance between the last line element and the new obstacle.")]
        public float maxBuildDistance = 30;

        /// <summary>
        /// The GameObject that is used to indicate the place where the element currently being positioned will be built.
        /// </summary>
        [SerializeField]
        protected GameObject highlight;

        Vector3 lastValidHitPoint;

        /// <summary>
        /// For a given direction, creates a rotation that positions a Unity DecalProjector flush with the ground and facing the direction.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static Quaternion GetRotationForDirection(Vector3 direction)
        {
            Vector3 newRideDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            Vector3 rideDirNormal = Vector3.Cross(newRideDirection, Vector3.up).normalized;
            return Quaternion.LookRotation(-Vector3.up, rideDirNormal);
        }

        /// <summary>
        /// Updates a highlight GameObject with given parameters.
        /// </summary>
        /// <param name="highlight">The highlight to update.</param>
        /// <param name="length">How much the highlight should be stretched.</param>
        /// <param name="position">The position the center of the highlight should be placed at.</param>
        /// <param name="direction">The direction the highlight should be rotated towards and stretched in.</param>
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

            Quaternion newRot = GetRotationForDirection(direction);
            highlight.transform.SetPositionAndRotation(position, newRot);

            DecalProjector projector = highlight.GetComponent<DecalProjector>();
            projector.size = new Vector3(length, Line.Instance.GetLastLineElement().GetBottomWidth(), 10);
        }

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

                highlight.transform.position = new Vector3(projectedHitPoint.x, projectedHitPoint.y, projectedHitPoint.z);

                float distance = Vector3.Distance(projectedHitPoint, endPoint);

                // position the text in the middle of the screen

                // make the text go along the line and lay flat on the terrain
                textMesh.transform.SetPositionAndRotation(Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, Line.baseHeight)), Quaternion.LookRotation(-Vector3.up, Vector3.Cross(toHit, Vector3.up)));
                textMesh.GetComponent<TextMeshPro>().text = $"Distance: {distance:F2}m";

                // draw a line between the current line end point and the point where the mouse is pointing
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, endPoint + 0.1f * Vector3.up);
                lineRenderer.SetPosition(1, projectedHitPoint - rideDirection * highlight.GetComponent<DecalProjector>().size.x / 2 + 0.1f * Vector3.up);

                return true;
            }
            else { return false; }
        }

        public override void OnHighlightClicked(InputAction.CallbackContext context)
        {
            Debug.Log("highlight clicked");
            if (validHighlightPosition)
            {
                Debug.Log("state start: clicked to build slope start. in slopehighlighter now.");
                SlopeChangeBuilder slopeBuilder = new(lastValidHitPoint);
                StateController.Instance.ChangeState(new SlopeBuildState(slopeBuilder));
                return;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            highlight.SetActive(false);

            // if highlight is not positioned somewhere in front of the last line element, move it there
            if ((highlight.transform.position - lastLineElement.GetEndPoint()).normalized != lastLineElement.GetRideDirection().normalized)
            {
                highlight.transform.position = lastLineElement.GetEndPoint() + lastLineElement.GetRideDirection().normalized;
            }

            highlight.transform.rotation = GetRotationForDirection(lastLineElement.GetRideDirection());

            float width = lastLineElement.GetWidth() + 2 * lastLineElement.GetHeight() * 0.2f;

            DecalProjector decalProjector = highlight.GetComponent<DecalProjector>();
            decalProjector.size = new Vector3(0.1f, width, 10);
        }

        private new void OnDisable()
        {
            base.OnDisable();

            DecalProjector projector = highlight.GetComponent<DecalProjector>();
            projector.size = new Vector3(3, 3, 10);

        }
    }
}