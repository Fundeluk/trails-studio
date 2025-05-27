using Assets.Scripts.Builders;
using Assets.Scripts.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class Trajectory : ICollection<Trajectory.TrajectoryPoint>, IReadOnlyCollection<Trajectory.TrajectoryPoint>, IEnumerable<Trajectory.TrajectoryPoint>, IEnumerable
{
    public IEnumerator<TrajectoryPoint> GetEnumerator()
    {
        return trajectoryPoints.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public struct TrajectoryPoint
    {
        public Vector3 position;
        public Vector3 velocity;
        public TrajectoryPoint(Vector3 position, Vector3 velocity)
        {
            this.position = position;
            this.velocity = velocity;
        }
    }

    readonly List<TrajectoryPoint> trajectoryPoints = new();

    TrajectoryPoint lowestPoint = new(new(0, float.MaxValue, 0), Vector3.zero);
    TrajectoryPoint highestPoint = new(new(0, float.MinValue, 0), Vector3.zero);

    public int Count => ((ICollection<TrajectoryPoint>)trajectoryPoints).Count;

    public bool IsReadOnly => ((ICollection<TrajectoryPoint>)trajectoryPoints).IsReadOnly;

    public TrajectoryPoint this[int index]
    {
        get
        {
            if (index < 0 || index >= trajectoryPoints.Count)
                throw new IndexOutOfRangeException($"Trajectory index {index} is out of range (Count: {trajectoryPoints.Count}).");
            return trajectoryPoints[index];
        }
    }

    public TrajectoryPoint GetClosestPoint(Vector3 position)
    {
        float minDistance = float.MaxValue;
        TrajectoryPoint closestPoint = trajectoryPoints[0];
        foreach (var point in trajectoryPoints)
        {
            float distance = Vector3.Distance(point.position, position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = point;
            }
        }
        return closestPoint;
    }

    /// <summary>
    /// Finds the point whose height is closest to height and returns it.
    /// As the trajectory is nearly parabolic, there can be multiple points at some height.
    /// This method returns the first such point from the end of the trajectory.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if the supplied height is on the trajectory but no suitable point could be found.</exception>
    public TrajectoryPoint GetPointAtHeight(float height)
    {
        if (height >= highestPoint.position.y)
        {
            return highestPoint;
        }
        else if (height <= lowestPoint.position.y)
        {
            return lowestPoint;
        }


        for (int i = trajectoryPoints.Count - 1; i >= 0; i--)
        {
            // going from the endpoint of the trajectory, the height goes up, so check for first point that is above the height
            if (trajectoryPoints[i].position.y >= height)
            {
                return trajectoryPoints[i];
            }
        }

        throw new ArgumentException("No closest point could be found.");
    }

    /// <summary>
    /// Finds the point whose velocity is closest to the supplied direction and returns it.
    /// </summary>
    public TrajectoryPoint GetPointWithDirection(Vector3 direction)
    {
        float minAngle = float.MaxValue;
        TrajectoryPoint closestPoint = trajectoryPoints[0];
        foreach (var point in trajectoryPoints)
        {
            float angle = Vector3.Angle(point.velocity, direction);
            if (angle < minAngle)
            {
                minAngle = angle;
                closestPoint = point;
            }
        }
        return closestPoint;
    }
    
    public void Add(Vector3 position, Vector3 velocity)
    {
        ((ICollection<TrajectoryPoint>)this).Add(new TrajectoryPoint(position, velocity));
    }
    

    void ICollection<TrajectoryPoint>.Add(TrajectoryPoint point)
    {
        if (point.position.y > highestPoint.position.y)
        {
            highestPoint = point;
        }

        if (point.position.y < lowestPoint.position.y)
        {
            lowestPoint = point;
        }

        trajectoryPoints.Add(point);
    }

    public void Clear()
    {
        ((ICollection<TrajectoryPoint>)trajectoryPoints).Clear();
    }

    public bool Contains(TrajectoryPoint item)
    {
        return ((ICollection<TrajectoryPoint>)trajectoryPoints).Contains(item);
    }

    public void CopyTo(TrajectoryPoint[] array, int arrayIndex)
    {
        ((ICollection<TrajectoryPoint>)trajectoryPoints).CopyTo(array, arrayIndex);
    }

    public bool Remove(TrajectoryPoint item)
    {
        return ((ICollection<TrajectoryPoint>)trajectoryPoints).Remove(item);
    }
}

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
        public static float CalculateExitSpeed(float initSpeed, float distance, float slopeAngle = 0, float timeStep = timeStep)
        {
            // TODO handle when the speed is not enough to travel the distance
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
                return CalculateExitSpeed(initSpeed, segmentLength);
            }
            
            SlopeChange slopeChange = TerrainManager.Instance.ActiveSlope;
            float slopeAngle = Mathf.Abs(slopeChange.Angle);

            // TODO simulate the same conditions as in placeobstacle(takeoff) method, where the conditions work
            Debug.Log($"startpoint before slope start: {slopeChange.IsBeforeStart(startPoint)}, " +
                $"endPoint before slope start: {slopeChange.IsBeforeStart(endPoint)}, " +
                $"startPoint on slope: {slopeChange.IsOnActivePartOfSlope(startPoint)}, " +
                $"endPoint on slope: {slopeChange.IsOnActivePartOfSlope(endPoint)}, " +
                $"startPoint after slope: {slopeChange.IsAfterSlope(startPoint)}, " +
                $"endPoint after slope: {slopeChange.IsAfterSlope(endPoint)}");

            // the position to measure is before the slope start
            if (slopeChange.IsBeforeStart(startPoint) && slopeChange.IsBeforeStart(endPoint))
            {
                Debug.Log("Last line element and position to measure are before the slope start.");
                // if the slope starts before the start point and ends before the end point, calculate the speed at the start point
                segmentLength = Vector3.Distance(startPoint, endPoint);
                return CalculateExitSpeed(initSpeed, segmentLength);
            }
            // the position to measure is on the slope, but start point is before the slope start            
            else if (slopeChange.IsBeforeStart(startPoint) && slopeChange.IsOnActivePartOfSlope(endPoint))
            {
                Debug.Log("Last line element is before the slope start, but position to measure is on the slope.");
                segmentLength = Vector3.Distance(startPoint, slopeChange.Start);
                initSpeed = CalculateExitSpeed(initSpeed, segmentLength);
                startPoint = slopeChange.Start;
                startPoint.y = 0;
                endPoint.y = 0;
                segmentLength = slopeChange.GetSlopeLengthFromXZDistance(Vector3.Distance(startPoint, endPoint));
                return CalculateExitSpeed(initSpeed, segmentLength, slopeAngle);
            }
            // the position to measure is on the slope and the start point is on the slope as well
            else if (lastLineElement.GetSlopeChange() == slopeChange && slopeChange.IsOnActivePartOfSlope(endPoint))
            {
                Debug.Log("Last line element and position to measure are on the slope.");
                startPoint.y = 0;
                endPoint.y = 0;
                segmentLength = slopeChange.GetSlopeLengthFromXZDistance(Vector3.Distance(startPoint, endPoint));
                return CalculateExitSpeed(initSpeed, segmentLength, slopeAngle);
            }
            // the position to measure is after slope and the start point is on the slope
            else if (lastLineElement.GetSlopeChange() == slopeChange && slopeChange.IsAfterSlope(endPoint))
            {
                Debug.Log("Last line element is on the slope, but position to measure is after the slope.");
                Vector3 slopeEnd = slopeChange.GetFinishedEndPoint();
                segmentLength = Vector3.Distance(startPoint, slopeEnd);
                initSpeed = CalculateExitSpeed(initSpeed, segmentLength, slopeAngle);
                startPoint = slopeEnd;
                endPoint.y = 0;
                segmentLength = Vector3.Distance(startPoint, endPoint);
                return CalculateExitSpeed(initSpeed, segmentLength);
            }
            // start point is before the slope and position to measure is after the slope
            else if (slopeChange.IsBeforeStart(startPoint) && slopeChange.IsAfterSlope(endPoint))
            {
                Debug.Log("Last line element is before the slope start, but position to measure is after the slope.");
                segmentLength = Vector3.Distance(startPoint, slopeChange.Start);
                initSpeed = CalculateExitSpeed(initSpeed, segmentLength);
                segmentLength = slopeChange.GetSlopeLengthFromXZDistance(slopeChange.Length);
                initSpeed = CalculateExitSpeed(initSpeed, segmentLength, slopeAngle);
                startPoint = slopeChange.GetFinishedEndPoint();
                endPoint.y = startPoint.y;
                segmentLength = Vector3.Distance(startPoint, endPoint);
                return CalculateExitSpeed(initSpeed, segmentLength);
            }
            // both the position to measure and the start point are after the slope            
            else
            {
                Debug.Log("Last line element and position to measure are after the slope.");
                startPoint.y = 0;
                endPoint.y = 0;
                segmentLength = Vector3.Distance(startPoint, endPoint);
                return CalculateExitSpeed(initSpeed, segmentLength);
            }            
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
            return Mathf.Sqrt(Mathf.Pow(takeoff.EntrySpeed, 2) - 2 * Gravity * takeoff.GetHeight());
        }   
        
        

        /// <summary>
        /// Calculates the flight trajectory of a takeoff.
        /// </summary>
        /// <param name="timeStep">How long are the time intervals between the samples on the trajectory in seconds.</param>
        public static Trajectory GetFlightTrajectory(TakeoffBase takeoff, float timeStep = timeStep)
        {
            Vector3 position = takeoff.GetTransitionEnd();
            float exitSpeed = takeoff.GetExitSpeed();
            Vector3 velocity = takeoff.GetTakeoffDirection() * exitSpeed;

            Trajectory results = new()
            {
                { position, velocity }
            };

            // Air resistance calculation factors
            float airResistanceFactor = 0.5f * AirDensity * FrontalArea * AirDragCoefficient / RiderBikeMass;           



            while (position.y >= TerrainManager.GetHeightAt(position))
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
                results.Add(position, velocity);
            }

            return results;
        }

        
        
    }
}