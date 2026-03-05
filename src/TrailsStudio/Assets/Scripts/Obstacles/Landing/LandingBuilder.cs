using System;
using System.Linq;
using LineSystem;
using Managers;
using Obstacles.TakeOff;
using TerrainEditing;
using UnityEngine;

namespace Obstacles.Landing
{   

    public class LandingBuilder : LandingBase, IObstacleBuilder
    {
        private void CloneSetSlope(object sender, ParamChangeEventArgs<float> args) => InvisibleClone.SetSlope(args.NewValue);

        private void CloneSetHeight(object sender, ParamChangeEventArgs<float> args) => InvisibleClone.SetHeight(args.NewValue);

        private void CloneSetThickness(object sender, ParamChangeEventArgs<float> args) => InvisibleClone.SetThickness(args.NewValue);

        private void CloneSetWidth(object sender, ParamChangeEventArgs<float> args) => InvisibleClone.SetHeight(args.NewValue);

        private void CloneSetExitSpeed(object sender, ParamChangeEventArgs<float> args) => InvisibleClone.UpdateExitSpeed();

        public LandingBuilder InvisibleClone { get; private set; }


        public override void Initialize()
        {
            base.Initialize();

            if (Line.Instance.GetLastLineElement() is not Takeoff)
            {
                throw new Exception("Landing must be built after a takeoff.");
            }

            meshGenerator.SetCanBuildMaterial();

            PairedTakeoff = Line.Instance.GetLastLineElement() as Takeoff;      

            SetHeight(PairedTakeoff.GetHeight());

            RecalculateCameraTargetPosition();

            CreateInvisibleClone();
        }

        

        private void CreateInvisibleClone()
        {
            // Create a new empty GameObject instead of using the prefab
            GameObject clone = new("LandingBuilder_PositioningClone");

            // Add only the essential components
            LandingMeshGenerator meshGen = clone.AddComponent<LandingMeshGenerator>();
            LandingBuilder cloneBuilder = clone.AddComponent<LandingBuilder>();

            // Set position and rotation
            clone.transform.SetPositionAndRotation(transform.position, transform.rotation);

            cloneBuilder.InitCameraTarget();

            // Copy mesh generator properties from original
            meshGen.Height = meshGenerator.Height;
            meshGen.Width = meshGenerator.Width;
            meshGen.Thickness = meshGenerator.Thickness;
            meshGen.Slope = meshGenerator.Slope;

            // Set up the clone builder properties
            cloneBuilder.meshGenerator = meshGen;
            cloneBuilder.MatchingTrajectory = MatchingTrajectory;
            cloneBuilder.ExitSpeed = ExitSpeed;
            cloneBuilder.PreviousLineElement = PreviousLineElement;
            cloneBuilder.PairedTakeoff = PairedTakeoff;

            InvisibleClone = cloneBuilder;

            ExitSpeedChanged += CloneSetExitSpeed;
            SlopeChanged += CloneSetSlope;
            HeightChanged += CloneSetHeight;
            ThicknessChanged += CloneSetThickness;
            WidthChanged += CloneSetWidth;


        }

        private void OnDisable()
        {
            ExitSpeedChanged -= CloneSetExitSpeed;
            SlopeChanged -= CloneSetSlope;
            HeightChanged -= CloneSetHeight;
            ThicknessChanged -= CloneSetThickness;
            WidthChanged -= CloneSetWidth;

            if (InvisibleClone != null)
            {
                Destroy(InvisibleClone.gameObject);
            }
        }

        public void CanBuild(bool canBuild)
        {
            if (canBuild)
            {
                meshGenerator.SetCanBuildMaterial();
            }
            else
            {
                meshGenerator.SetCannotBuildMaterial();
            }
        }    

        private void UpdateExitSpeed()
        {
            if (MatchingTrajectory == null)
            {
                return;
            }

            ExitSpeed = PhysicsManager.GetExitSpeed(this, MatchingTrajectory.Last());

            OnExitSpeedChanged(ExitSpeed);
        }
        
        public void SetMatchingTrajectory(Trajectory trajectory)
        {
            if (trajectory == MatchingTrajectory)
            {
                return;
            }

            MatchingTrajectory = trajectory;
            UpdateExitSpeed();

            PairedTakeoff.SetMatchingTrajectory(trajectory);
        }

        public void SetHeight(float height)
        {
            if (Mathf.Approximately(height, meshGenerator.Height))
            {
                return;
            }

            meshGenerator.Height = height;
            RecalculateCameraTargetPosition();
            UpdateExitSpeed();
            OnHeightChanged(GetHeight());
        }

        public void SetWidth(float width)
        {
            if (Mathf.Approximately(width, meshGenerator.Width))
            {
                return;
            }
            meshGenerator.Width = width;      
            OnWidthChanged(GetWidth());
        }

        public void SetThickness(float thickness)
        {
            if (Mathf.Approximately(thickness, meshGenerator.Thickness))
            {
                return;
            }
            meshGenerator.Thickness = thickness;
            RecalculateCameraTargetPosition();       
            OnThicknessChanged(GetThickness());
        }

        /// <param name="slope">Slope in radians.</param>
        public void SetSlope(float slope)
        {
            if (Mathf.Approximately(slope, meshGenerator.Slope))
            {
                return;
            }
            meshGenerator.Slope = slope;
            UpdateExitSpeed();
            OnSlopeChanged(GetSlopeAngle());
        }

        /// <summary>
        /// Sets new position of the landing and returns the new exit speed.
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            if (position == meshGenerator.transform.position)
            {
                return;
            }

            meshGenerator.transform.position = position;            

            RecalculateCameraTargetPosition();

            UpdateExitSpeed();
            OnPositionChanged(transform.position);
        }
        

        /// <summary>
        /// Rotates the landing around the y-axis to angle. Negative values rotate to riders left, positive to riders right.
        /// </summary>
        /// <param name="toAngle">The Angle to rotate to in degrees.</param>
        public void SetRotation(float toAngle)
        {
            float angleDiff = toAngle - GetRotation();
            meshGenerator.transform.Rotate(Vector3.up, angleDiff);
            
            RecalculateCameraTargetPosition();

            UpdateExitSpeed();

            OnRotationChanged(transform.rotation);
        }
        
        public void SetRideDirection(Vector3 rideDirection)
        {
            if (rideDirection == GetRideDirection())
            {
                return;
            }

            transform.forward = rideDirection;
            
            RecalculateCameraTargetPosition();

            UpdateExitSpeed();

            OnRotationChanged(transform.rotation);
        }

        /// <returns>The distance between the Start point and the takeoff edge.</returns>
        public float GetCurrentSlopeLength()
        {
            return Vector3.Distance(GetEndPoint(), transform.position);
        }


        public Landing Build()
        {
            enabled = false;

            Landing landing = GetComponent<Landing>();

            landing.Initialize(meshGenerator, CameraTarget, PreviousLineElement, ExitSpeed);

            landing.enabled = true;

            PairedTakeoff.SetLanding(landing);

            BuildManager.Instance.ActiveBuilder = null;

            landing.GetObstacleHeightmapCoordinates().MarkAs(new OccupiedCoordinateState(landing));

            if (TerrainManager.Instance.ActiveSlope != null)
            {
                TerrainManager.Instance.ActiveSlope.ConfirmChanges(landing);
            }

            return landing;
        }

        public void Cancel()
        {
            BuildManager.Instance.ActiveBuilder = null;
            DestroyUnderlyingGameObject();
        }
    }
}