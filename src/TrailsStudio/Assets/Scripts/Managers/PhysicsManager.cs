using Assets.Scripts.Builders;
using Assets.Scripts.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace Assets.Scripts.Managers
{
    public class PhysicsManager : Singleton<PhysicsManager>
    {
        /// <summary>
        /// The frontal area of a rider on a BMX in square meters. Used for aerodynamic calculations.
        /// </summary>
        public static float FrontalArea { get; private set; } = 0.6f; // m^2

        public static float RollingDragCoefficient { get; private set; } = 0.008f; // Dimensionless


        public static float AirDragCoefficient { get; private set; } = 1f; // Dimensionless

        /// <summary>
        /// Air density in kg/m^3. This is a constant value used for aerodynamic calculations.
        /// </summary>
        public static float AirDensity { get; private set; } = 1.2f;

        /// <summary>
        /// Force of gravity in m/s^2
        /// </summary>
        public static float Gravity { get; private set; } = 9.81f;

        /// <summary>
        /// Mass of the rider and the bike in kg.
        /// </summary>
        public static float RiderBikeMass { get; private set; } = 100f;

        public const float timeStep = 0.05f;


        // TODO make work for upward slopes as well
        /// <summary>
        /// Calculates the final speed of a rider after riding some distance.
        /// </summary>
        /// <param name="initSpeed">Initial speed in m/s</param>
        /// <param name="slopeAngle">Angle of the slope in radians, if on flat ground, enter 0</param>
        /// <param name="distance">Distance in m</param>
        /// <returns>The final speed in m/s</returns>
        public static float CalculateFinalSpeed(float initSpeed, float slopeAngle, float distance, float timeStep = timeStep)
        {
            float traveled = 0;
            float speed = initSpeed;

            while (traveled < distance)
            {
                // aerodynamic drag: Fd = ½ ρ Cd A v²
                float dragForce = 0.5f * AirDensity * AirDragCoefficient * FrontalArea * Mathf.Pow(speed, 2);

                // rolling resistance: Fr = Cr * N = Cr * m * g * cos(theta)
                float normalForce = RiderBikeMass * Gravity * Mathf.Cos(slopeAngle);
                float rollingForce = RollingDragCoefficient * normalForce;

                // slope force: Fslope = m * g * sin(theta) 
                //   + when theta>0 (downhill) this is +ve, aids motion
                //   when theta<0 (uphill) it's –ve, opposes motion
                float slopeForce = RiderBikeMass * Gravity * Mathf.Sin(slopeAngle);

                // total force along slope
                float totalForce = slopeForce - (dragForce + rollingForce);

                // a = F / m
                float acceleration = totalForce / RiderBikeMass;

                // update speed
                speed += acceleration * timeStep;
                if (speed < 0f) return 0;

                // advance position along slope
                traveled += speed * timeStep;
            }            

            return speed;
        }

        public static float CalculateFinalSpeedAfterDistance(float initSpeed, float distance)
        {
            return CalculateFinalSpeed(initSpeed, 0, distance);
        }

        /// <summary>
        /// Calculates the speed of a rider at a specific position.
        /// </summary>
        /// <remarks>
        /// There should not be any <see cref="ILineElement"/>s between lastLineElement and end point.
        /// </remarks>
        /// <param name="lastLineElement"><see cref="ILineElement"/> from which the speed is calculated.</param>        
        /// <returns>Speed at end point in m/s</returns>
        public static float GetSpeedAtPosition(ILineElement lastLineElement, Vector3 endPoint)
        {
            float initSpeed = lastLineElement.GetExitSpeed();

            float segmentLength;

            Vector3 startPoint = lastLineElement.GetEndPoint();

            if (TerrainManager.Instance.ActiveSlope == null)
            {
                segmentLength = Vector3.Distance(startPoint, endPoint);
                return CalculateFinalSpeedAfterDistance(initSpeed, segmentLength);
            }
            
            SlopeChange slopeChange = TerrainManager.Instance.ActiveSlope;

            // the slope starts after the endpoint
            if (slopeChange.IsPositionBeforeStart(endPoint))
            {
                segmentLength = Vector3.Distance(startPoint, endPoint);
                return CalculateFinalSpeedAfterDistance(initSpeed, segmentLength);
            }

            float slopeAngle = Mathf.Abs(slopeChange.Angle) * Mathf.Deg2Rad;

            // if the slope change starts after the last line element ends, calculate the speed at the slope start
            if (slopeChange.IsPositionBeforeStart(startPoint))
            {
                initSpeed = CalculateFinalSpeedAfterDistance(initSpeed, Vector3.Distance(startPoint, slopeChange.Start));
                startPoint = slopeChange.Start;
            }

            // if the end position is still on the slope
            if (slopeChange.IsOnSlope(endPoint))
            {
                segmentLength = slopeChange.GetLength(Vector3.Distance(startPoint, endPoint));
                return CalculateFinalSpeed(initSpeed, slopeAngle, segmentLength);
            }
            // if not, calculate the 
            else
            {
                segmentLength = slopeChange.GetLength(Vector3.Distance(startPoint, slopeChange.GetFinishedEndPoint()));
                initSpeed = CalculateFinalSpeed(initSpeed, slopeAngle, segmentLength);
                startPoint = slopeChange.GetFinishedEndPoint();
            }

            return CalculateFinalSpeedAfterDistance(initSpeed, Vector3.Distance(startPoint, endPoint));
        }

        public static float MsToKmh(float speed)
        {
            return speed * 3.6f;
        }

        /// <summary>
        /// Calculates the speed at which the rider exits the takeoff transition.
        /// </summary>        
        public static float GetExitSpeed(TakeoffBase takeoff)
        {
            return Mathf.Sqrt(Mathf.Pow(takeoff.EntrySpeed, 2) - 2 * PhysicsManager.Gravity * takeoff.GetHeight());
        }        

        /// <summary>
        /// Calculates the flight trajectory of a takeoff.
        /// </summary>
        /// <param name="timeStep">How long are the time intervals between the samples on the trajectory in seconds.</param>
        public static List<(Vector3 position, Vector3 velocity)> GetFlightTrajectory(TakeoffBase takeoff, float timeStep = timeStep)
        {
            Vector3 position = takeoff.GetTransitionEnd();
            float exitSpeed = takeoff.GetExitSpeed();
            Vector3 velocity = takeoff.GetTakeoffDirection() * exitSpeed;

            var results = new List<(Vector3 position, Vector3 velocity)>
            {
                (position, velocity)
            };

            // Air resistance calculation factors
            float airResistanceFactor = 0.5f * AirDensity * FrontalArea * AirDragCoefficient / RiderBikeMass;

            Func<Vector3, bool> isCollidingWithGround;
            if (TerrainManager.Instance.ActiveSlope == null)
            {
                isCollidingWithGround = (Vector3 pos) => TerrainManager.GetHeightAt(pos) >= pos.y;
            }
            else
            {
                isCollidingWithGround = (Vector3 pos) => TerrainManager.Instance.ActiveSlope.GetHeightAt(pos) >= pos.y;                    
            }


            while (!isCollidingWithGround(position))
            {
                // Calculate air resistance
                float speedSquared = velocity.sqrMagnitude;
                Vector3 airResistance = -airResistanceFactor * speedSquared * velocity.normalized;

                // Calculate gravity
                Vector3 gravity = Vector3.down * Gravity;

                // Calculate total acceleration
                Vector3 acceleration = gravity + airResistance;

                // Update velocity using acceleration
                velocity += acceleration * timeStep;

                // Update position using velocity
                position += velocity * timeStep;

                // Add point to trajectory
                results.Add((position, velocity));
            }

            return results;
        }

        
        
    }
}