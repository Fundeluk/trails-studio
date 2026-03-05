using LineSystem;
using Managers;
using Obstacles;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TerrainEditing.Slope
{
    public class SlopeChangeBuilder : SlopeChangeBase, IBuilder
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            Length = SlopeSettings.MinLength;
            Start = transform.position;
            StartHeight = Start.y;
            EndHeight = StartHeight;
            Width = PreviousLineElement.GetBottomWidth();
            Highlight = GetComponent<DecalProjector>();
            PreviousLineElement = Line.Instance.GetLastLineElement();
            Highlight.enabled = true;
            UpdateHighlight();            
        }

        public void CanBuild(bool canBuild)
        {
            Highlight.material.color = canBuild ? Color.green : Color.red;
        }

        public bool IsValid()
        {
            if (HeightDifference == 0 || Length == 0)
            {
                StudioUIManager.Instance.ShowMessage("Slope height difference and length have to be non-zero.", 2);
                return false;
            }

            if (HeightDifference < SlopeSettings.MinHeightDifference)
            {
                StudioUIManager.Instance.ShowMessage($"Height difference cannot be lower than {SlopeSettings.MinHeightDifference}", 3f);

                return false;
            }

            if (HeightDifference > SlopeSettings.MaxHeightDifference)
            {
                StudioUIManager.Instance.ShowMessage($"Height difference cannot be greater than {SlopeSettings.MaxHeightDifference}", 3f);
                return false;
            }

            float angleDeg = Angle * Mathf.Rad2Deg;

            if (Mathf.Abs(angleDeg) < SlopeSettings.MinSlopeAngleDeg)
            {
                StudioUIManager.Instance.ShowMessage($"Angle of the slope cannot be lower than {SlopeSettings.MinSlopeAngleDeg}", 3f);
                return false;
            }

            if (Mathf.Abs(angleDeg) > SlopeSettings.MaxSlopeAngleDeg)
            {
                StudioUIManager.Instance.ShowMessage($"Angle of the slope cannot be greater than {SlopeSettings.MaxSlopeAngleDeg}", 3f);
                return false;
            }

            float exitSpeed = GetExitSpeed();

            if (exitSpeed < LineSettings.MinExitSpeedMS)
            {
                StudioUIManager.Instance.ShowMessage($"Insufficient speed: Speed at slope exit is lower than {PhysicsManager.MsToKmh(LineSettings.MinExitSpeedMS)}.", 3f);
                return false;
            }

            if (exitSpeed > LineSettings.MaxExitSpeedMS)
            {
                StudioUIManager.Instance.ShowMessage($"Too much speed: Speed at slope exit is higher than {PhysicsManager.MsToKmh(LineSettings.MaxExitSpeedMS)}.", 3f);
                return false;
            }

            if (!IsBuildable(Start, Length,Line.Instance.GetCurrentRideDirection()))
            {
                StudioUIManager.Instance.ShowMessage($"Slope cannot be built here. The area is occupied.", 3f);
                return false;
            }

            return true;
        }

        protected override void UpdateHighlight()
        {
            if (Length == 0)
            {
                Highlight.size = new Vector3(0.1f, Width, 20);
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
            this.EndHeight = StartHeight + heightDifference;

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

            if (exitSpeed < LineSettings.MinExitSpeedMS)
            {
                return false;
            }

            return true;
        }

        public SlopeChange Build()
        {
            enabled = false;

            SlopeChange slopeChange = GetComponent<SlopeChange>();
            slopeChange.Initialize(Start, EndHeight, Length);      
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
            Highlight.transform.rotation = rotation;
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
            return Highlight.transform;
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