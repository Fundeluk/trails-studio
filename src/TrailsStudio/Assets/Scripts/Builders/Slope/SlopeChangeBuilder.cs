using Assets.Scripts.Builders.Slope;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Assets.Scripts.Builders
{
    public class SlopeChangeBuilder : SlopeChangeBase, IBuilder
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            Length = SlopeConstants.MIN_LENGTH;
            Start = transform.position;
            startHeight = Start.y;
            endHeight = startHeight;
            Width = previousLineElement.GetBottomWidth();
            highlight = GetComponent<DecalProjector>();
            previousLineElement = Line.Instance.GetLastLineElement();
            highlight.enabled = true;
            UpdateHighlight();            
        }

        public void CanBuild(bool canBuild)
        {
            if (canBuild)
            {
                highlight.material.color = Color.green;
            }
            else
            {
                highlight.material.color = Color.red;
            }
        }

        public bool IsValid()
        {
            if (HeightDifference == 0 || Length == 0)
            {
                UIManager.Instance.ShowMessage("Slope height difference and length have to be non-zero.", 2);
                return false;
            }

            if (HeightDifference < SlopeConstants.MIN_HEIGHT_DIFFERENCE)
            {
                UIManager.Instance.ShowMessage($"Height difference cannot be lower than {SlopeConstants.MIN_HEIGHT_DIFFERENCE}m", 3f);

                return false;
            }

            if (HeightDifference > SlopeConstants.MAX_HEIGHT_DIFFERENCE)
            {
                UIManager.Instance.ShowMessage($"Height difference cannot be greater than {SlopeConstants.MAX_HEIGHT_DIFFERENCE}m", 3f);
                return false;
            }

            float angleDeg = Angle * Mathf.Rad2Deg;

            if (Mathf.Abs(angleDeg) < SlopeConstants.MIN_SLOPE_ANGLE_DEG)
            {
                UIManager.Instance.ShowMessage($"Angle of the slope cannot be lower than {SlopeConstants.MIN_SLOPE_ANGLE_DEG}°", 3f);
                return false;
            }

            if (Mathf.Abs(angleDeg) > SlopeConstants.MAX_SLOPE_ANGLE_DEG)
            {
                UIManager.Instance.ShowMessage($"Angle of the slope cannot be greater than {SlopeConstants.MAX_SLOPE_ANGLE_DEG}°", 3f);
                return false;
            }

            float exitSpeed = GetExitSpeed();

            if (exitSpeed < Line.MIN_EXIT_SPEED_MS)
            {
                UIManager.Instance.ShowMessage($"Insufficient speed: Speed at slope exit is lower than {PhysicsManager.MsToKmh(Line.MIN_EXIT_SPEED_MS)}km/h.", 3f);
                return false;
            }

            if (exitSpeed > Line.MAX_EXIT_SPEED_MS)
            {
                UIManager.Instance.ShowMessage($"Too much speed: Speed at slope exit is higher than {PhysicsManager.MsToKmh(Line.MAX_EXIT_SPEED_MS)}km/h.", 3f);
                return false;
            }

            if (!IsBuildable(Start, Length,Line.Instance.GetCurrentRideDirection()))
            {
                UIManager.Instance.ShowMessage($"Slope cannot be built here. The area is occupied.", 3f);
                return false;
            }

            return true;
        }

        protected override void UpdateHighlight()
        {
            if (Length == 0)
            {
                highlight.size = new Vector3(0.1f, Width, 20);
            }
            else
            {
                base.UpdateHighlight();
            }
        }

        public void SetLength(float length)
        {
            Vector3 rideDir = Vector3.ProjectOnPlane(Line.Instance.GetCurrentRideDirection(), Vector3.up).normalized;            

            this.Length = length;
            transform.position = Vector3.Lerp(Start, Start + length * rideDir, 0.5f);

            UpdateAngle();
            UpdateHighlight();

            OnLengthChanged(Length);
        }        

        

        public void SetHeightDifference(float heightDifference)
        {
            this.endHeight = startHeight + heightDifference;

            UpdateAngle();

            UpdateHighlight();

            OnHeightDiffChanged(HeightDifference);
        }

        public bool IsBuildable(Vector3 start, float length, Vector3 direction)
        {
            return TerrainManager.Instance.IsAreaFree(start, start + length * direction, Width);
        }

        public float GetExitSpeed()
        {
            float speedFromLast = Line.Instance.GetLastLineElement().GetExitSpeed();

            if (!PhysicsManager.TryCalculateExitSpeed(speedFromLast, Vector3.Distance(Line.Instance.GetLastLineElement().GetEndPoint(), Start), out float entrySpeed))
            {
                return 0;
            }

            if (!PhysicsManager.TryCalculateExitSpeed(entrySpeed, Length, out float exitSpeed, -Angle))
            {
                return 0;
            }

            return exitSpeed;

        }

        /// <summary>
        /// Checks whether the slope can be traversed so that the rider has high enough exit speed at the end of the slope
        /// </summary>       
        public static bool HasEnoughExitSpeed(Vector3 start, float length, float heightDiff)
        {
            float angle = GetSlopeAngle(length, heightDiff);

            float speedFromLast = Line.Instance.GetLastLineElement().GetExitSpeed();

            if (!PhysicsManager.TryCalculateExitSpeed(speedFromLast, Vector3.Distance(Line.Instance.GetLastLineElement().GetEndPoint(), start), out float entrySpeed))
            {
                return false;
            }

            if (!PhysicsManager.TryCalculateExitSpeed(entrySpeed, length, out float exitSpeed, -angle))
            {
                return false;
            }

            if (exitSpeed < Line.MIN_EXIT_SPEED_MS)
            {
                return false;
            }

            return true;
        }

        public SlopeChange Build()
        {
            enabled = false;

            SlopeChange slopeChange = GetComponent<SlopeChange>();
            slopeChange.Initialize(Start, endHeight, Length);      
            slopeChange.enabled = true;

            return slopeChange;
        }

        /// <summary>
        /// Sets the position of the slope's start point and updates the highlight.
        /// </summary>        
        public void SetPosition(Vector3 position)
        {
            Vector3 rideDir = Vector3.ProjectOnPlane(Line.Instance.GetCurrentRideDirection(), Vector3.up).normalized;
            Start = position;
            transform.position = Vector3.Lerp(Start, Start + rideDir * Length, 0.5f);
            UpdateHighlight();

            OnPositionChanged(Start);
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            Vector3 rideDirNormal = Vector3.Cross(rideDirection, Vector3.up).normalized;
            Quaternion rotation = Quaternion.LookRotation(-Vector3.up, rideDirNormal);
            highlight.transform.rotation = rotation;
            UpdateHighlight();
        }

        public Vector3 GetStartPoint()
        {
            return Start;
        }

        public Vector3 GetEndPoint()
        {
            return Start + Length * Line.Instance.GetCurrentRideDirection();
        }

        public Transform GetTransform()
        {
            return highlight.transform;
        }

        public Vector3 GetRideDirection()
        {
            return Line.Instance.GetCurrentRideDirection();
        }

        public void DestroyUnderlyingGameObject()
        {
            TerrainManager.Instance.ActiveSlope = null;
            Destroy(gameObject);
        }
    }
}