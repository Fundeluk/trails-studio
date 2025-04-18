using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace Assets.Scripts.CameraUtilities
{
    public class SplineCamTargetRotater : MonoBehaviour
    {
        [SerializeField]
        CinemachineSplineCart splineCart;


        /// <param name="t">Normalized position on the spline (0 to 1).</param>
        /// <returns>The world space point on the spline.</returns>
        Vector3 GetPositionOnSpline(float t)
        {
            // Get the position on the spline at parameter t
            return splineCart.GetComponent<CinemachineSplineCart>().Spline.EvaluatePosition(t);
        }

        /// <summary>
        /// Updates this camera tracking target's look direction to the last line element from a position on the spline.
        /// </summary>
        /// <param name="t">Normalized position on the spline (0 to 1).</param>
        /// <returns>Vector that points from the spline position to the target in world coordinates.</returns>
        public Vector3 UpdateTrackingTarget(float t)
        {
            // Get the position on the spline at parameter t
            Vector3 targetPosition = GetPositionOnSpline(t);
            Vector3 worldToTarget = (Line.Instance.GetLastLineElement().GetCameraTarget().transform.position - targetPosition).normalized;
            var rotation = Quaternion.LookRotation(worldToTarget);
            transform.rotation = rotation;
            return worldToTarget;
        }       
    }
}