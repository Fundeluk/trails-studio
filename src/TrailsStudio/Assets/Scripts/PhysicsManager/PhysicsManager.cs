using System;
using System.Collections;
using System.Collections.Generic;
using LineSystem;
using Misc;
using Obstacles;
using Obstacles.Landing;
using Obstacles.TakeOff;
using TerrainEditing;
using TerrainEditing.Slope;
using UnityEngine;
using static PhysicsManager.PhysicsSettings;

namespace PhysicsManager
{
    public class Trajectory :IReadOnlyCollection<Trajectory.TrajectoryPoint>
    {
        public IEnumerator<TrajectoryPoint> GetEnumerator()
        {
            return trajectoryPoints.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [Serializable]
        public struct TrajectoryPoint : IEquatable<TrajectoryPoint>
        {
            public Vector3 position;
            public Vector3 velocity;
            public TrajectoryPoint(Vector3 position, Vector3 velocity)
            {
                this.position = position;
                this.velocity = velocity;
            }

            public bool Equals(TrajectoryPoint other)
            {
                return position.Equals(other.position) && velocity.Equals(other.velocity);
            }

            public override bool Equals(object obj)
            {
                return obj is TrajectoryPoint other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(position, velocity);
            }
        }

        readonly LinkedList<TrajectoryPoint> trajectoryPoints = new();

        LinkedListNode<TrajectoryPoint> lowestPoint = new(new TrajectoryPoint(new(0, float.MaxValue, 0), Vector3.zero));
        LinkedListNode<TrajectoryPoint> highestPoint = new(new TrajectoryPoint(new(0, float.MinValue, 0), Vector3.zero));
        public LinkedListNode<TrajectoryPoint> Apex => highestPoint;

        public int Count => ((ICollection<TrajectoryPoint>)trajectoryPoints).Count;

        public bool IsReadOnly => ((ICollection<TrajectoryPoint>)trajectoryPoints).IsReadOnly;    

        public TrajectoryPoint GetClosestPoint(Vector3 position)
        {
            float minDistance = float.MaxValue;
            TrajectoryPoint closestPoint = trajectoryPoints.First.Value;
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

        public TrajectoryPoint GetPointWithSimilarVelocity(Vector3 velocity)
        {
            float minAngle = float.MaxValue;
            TrajectoryPoint closestPoint = trajectoryPoints.First.Value;
            foreach (var point in trajectoryPoints)
            {
                float angle = Vector3.Angle(point.velocity, velocity);
                if (angle < minAngle)
                {
                    minAngle = angle;
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
        /// <remarks>If the height param is greater than the trajectory highest point or lower than the lowest point, returns null.</remarks>
        /// <exception cref="ArgumentException">Thrown if the supplied height is on the trajectory but no suitable point could be found.</exception>
        public LinkedListNode<TrajectoryPoint> GetPointAtHeight(float height)
        {
            if (height >= highestPoint.Value.position.y)
            {
                InternalDebug.Log("requested height is above the highest point of the trajectory, returning null.");
                return null;
            }
            else if (height <= lowestPoint.Value.position.y)
            {
                InternalDebug.Log("requested height is below the lowest point of the trajectory, returning null.");
                return null;
            }       

            foreach (var point in Reverse())
            {
                if (point.Value.position.y >= height)
                {
                    return point;
                }
            }

            throw new ArgumentException("No closest point could be found.");
        }

        public IEnumerable<LinkedListNode<TrajectoryPoint>> Reverse()
        {
            LinkedListNode<TrajectoryPoint> current = trajectoryPoints.Last;
            while (current != null)
            {
                yield return current;
                current = current.Previous;
            }
        }

        public void RemoveLast()
        {
            trajectoryPoints.RemoveLast();
        }

        /// <summary>
        /// Finds the point whose velocity is closest to the supplied direction and returns it.
        /// </summary>
        public LinkedListNode<TrajectoryPoint> GetPointWithDirection(Vector3 direction)
        {
            float minAngle = float.MaxValue;
            LinkedListNode<TrajectoryPoint> closestPoint = trajectoryPoints.First;
            foreach (var point in Reverse())
            {
                float angle = Vector3.Angle(point.Value.velocity, direction);
                if (angle < minAngle)
                {
                    minAngle = angle;
                    closestPoint = point;
                }
            }
            return closestPoint;
        }

        public void RemoveTrajectoryPointsAfter(LinkedListNode<TrajectoryPoint> node)
        {
            if (node.List != trajectoryPoints)
            {
                throw new ArgumentException("The node does not belong to this trajectory.");
            }

            while (node.Next != null)
            {
                RemoveLast();
            }
        }

        public void Add(Vector3 position, Vector3 velocity)
        {
            Add(new TrajectoryPoint(position, velocity));
        }
    

        void Add(TrajectoryPoint point)
        {
            var node = trajectoryPoints.AddLast(point);
            if (point.position.y > highestPoint.Value.position.y)
            {
                highestPoint = node;
            }

            if (point.position.y < lowestPoint.Value.position.y)
            {
                lowestPoint = node;
            }
        }

        public void Clear()
        {
            trajectoryPoints.Clear();
        }

        public bool Contains(TrajectoryPoint item)
        {
            return trajectoryPoints.Contains(item);
        }

        public void CopyTo(TrajectoryPoint[] array, int arrayIndex)
        {
            trajectoryPoints.CopyTo(array, arrayIndex);
        }    
    }

    public class InsufficientSpeedException : Exception
    {
        public InsufficientSpeedException() { }

        public InsufficientSpeedException(string message, Exception inner) : base(message, inner) { }

        public InsufficientSpeedException(string message) : base(message) { }
    }

    public class PhysicsManager : Singleton<PhysicsManager>
    {
        private const float TIME_STEP = 0.05f;

        /// <summary>
        /// Calculates the final speed of a rider after riding some distance.
        /// </summary>
        /// <param name="initSpeed">Initial speed in m/s</param>
        /// <param name="slopeAngle">Angle of the slope in radians, if on flat ground, enter 0</param>
        /// <param name="distance">Distance in m</param>
        /// <param name="exitSpeed">Output parameter for the final speed in m/s</param>
        /// <returns>True if it is possible to travel the distance, false if not</returns>
        public static bool TryCalculateExitSpeed(float initSpeed, float distance, out float exitSpeed, float slopeAngle = 0, float timeStep = TIME_STEP)
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

                if (speed <= 0f)
                {
                    exitSpeed = 0;
                    return false; // rider stops before reaching the distance
                }

                // advance position along slope
                traveled += speed * timeStep;
            }
            
            exitSpeed = speed;

            return true;
        }       

        /// <summary>
        /// Calculates the speed of a rider at a specific position.
        /// </summary>
        /// <remarks>
        /// There should not be any <see cref="ILineElement"/>s between lastLineElement and end point.
        /// </remarks>
        /// <param name="lastLineElement"><see cref="ILineElement"/> from which the speed is calculated.</param>        
        /// <returns>Speed at end point in m/s OR null in case the position is not reachable.</returns>
        public static bool TryGetSpeedAtPosition(ILineElement lastLineElement, Vector3 endPoint, out float speed)
        {
            float initSpeed = lastLineElement.GetExitSpeed();

            float segmentLength;

            Vector3 startPoint = lastLineElement.GetEndPoint();

            if (TerrainManager.Instance.ActiveSlope == null)
            {
                segmentLength = Vector3.Distance(startPoint, endPoint);

                if (TryCalculateExitSpeed(initSpeed, segmentLength, out float exitSpeed))
                {
                    speed = exitSpeed;
                    return true;
                }
                else
                {
                    speed = 0;
                    return false; // the position is not reachable
                }
            }
            
            SlopeChange slopeChange = TerrainManager.Instance.ActiveSlope;

            // in physics calculations, a positive angle signifies a downwards slope, but the slopeChange.Angle is positive for an upwards slope
            // so we need to negate the angle to get the correct slope direction
            float slopeAngle = -slopeChange.Angle;
            Vector3 slopeDir = slopeChange.LastRideDirection;
            
            // the position to measure is before the slope start
            if (slopeChange.IsBeforeStart(startPoint, slopeDir) && slopeChange.IsBeforeStart(endPoint, slopeDir))
            {
                // if the slope starts before the start point and ends before the end point, calculate the speed at the start point
                segmentLength = Vector3.Distance(startPoint, endPoint);
                if (TryCalculateExitSpeed(initSpeed, segmentLength, out float exitSpeed))
                {
                    speed = exitSpeed;
                    return true;
                }
                else
                {
                    speed = 0;
                    return false; // the position is not reachable
                }
            }
            // the position to measure is on the slope, but start point is before the slope start            
            else if (slopeChange.IsBeforeStart(startPoint, slopeDir) && slopeChange.IsOnActivePartOfSlope(endPoint, slopeDir))
            {
                segmentLength = Vector3.Distance(startPoint, slopeChange.Start);

                if (!TryCalculateExitSpeed(initSpeed, segmentLength, out initSpeed))
                {
                    speed = 0;
                    return false; // the position is not reachable
                }

                startPoint = slopeChange.Start;
                startPoint.y = 0;
                endPoint.y = 0;
                segmentLength = slopeChange.GetSlopeLengthFromXZDistance(Vector3.Distance(startPoint, endPoint));

                if (TryCalculateExitSpeed(initSpeed, segmentLength, out float exitSpeed, slopeAngle))
                {
                    speed = exitSpeed;
                    return true;
                }
                else
                {
                    speed = 0;
                    return false; // the position is not reachable
                }
            }
            // the position to measure is on the slope and the start point is on the slope as well
            else if (lastLineElement.GetSlopeChange() == slopeChange && slopeChange.IsOnActivePartOfSlope(endPoint, slopeDir))
            {
                startPoint.y = 0;
                endPoint.y = 0;
                segmentLength = slopeChange.GetSlopeLengthFromXZDistance(Vector3.Distance(startPoint, endPoint));

                if (TryCalculateExitSpeed(initSpeed, segmentLength, out float exitSpeed, slopeAngle))
                {
                    speed = exitSpeed;
                    return true;
                }
                else
                {
                    speed = 0;
                    return false; // the position is not reachable
                }
            }
            // the position to measure is after slope and the start point is on the slope
            else if (lastLineElement.GetSlopeChange() == slopeChange && slopeChange.IsAfterSlope(endPoint, slopeDir))
            {
                Vector3 slopeEnd = slopeChange.GetFinishedEndPoint(slopeDir);
                segmentLength = Vector3.Distance(startPoint, slopeEnd);

                if (!TryCalculateExitSpeed(initSpeed, segmentLength, out initSpeed, slopeAngle))
                {
                    speed = 0;
                    return false; // the position is not reachable
                }

                startPoint = slopeEnd;
                endPoint.y = 0;
                segmentLength = Vector3.Distance(startPoint, endPoint);

                if (TryCalculateExitSpeed(initSpeed, segmentLength, out float exitSpeed))
                {
                    speed = exitSpeed;
                    return true;
                }
                else
                {
                    speed = 0;
                    return false; // the position is not reachable
                }
            }
            // start point is before the slope and position to measure is after the slope
            else if (slopeChange.IsBeforeStart(startPoint, slopeDir) && slopeChange.IsAfterSlope(endPoint, slopeDir))
            {
                segmentLength = Vector3.Distance(startPoint, slopeChange.Start);

                if (!TryCalculateExitSpeed(initSpeed, segmentLength, out initSpeed))
                {
                    speed = 0;
                    return false; // the position is not reachable
                }

                segmentLength = slopeChange.GetSlopeLengthFromXZDistance(slopeChange.Length);

                if (!TryCalculateExitSpeed(initSpeed, segmentLength, out initSpeed, slopeAngle))
                {
                    speed = 0;
                    return false; // the position is not reachable
                }

                startPoint = slopeChange.GetFinishedEndPoint(slopeDir);
                endPoint.y = startPoint.y;
                segmentLength = Vector3.Distance(startPoint, endPoint);

                if (TryCalculateExitSpeed(initSpeed, segmentLength, out float exitSpeed))
                {
                    speed = exitSpeed;
                    return true;
                }
                else
                {
                    speed = 0;
                    return false; // the position is not reachable
                }
            }
            // both the position to measure and the start point are after the slope            
            else
            {
                startPoint.y = 0;
                endPoint.y = 0;
                segmentLength = Vector3.Distance(startPoint, endPoint);

                if (TryCalculateExitSpeed(initSpeed, segmentLength, out float exitSpeed))
                {
                    speed = exitSpeed;
                    return true;
                }
                else
                {
                    speed = 0;
                    return false; // the position is not reachable
                }
            }            
        }

        public static float MsToKmh(float speed)
        {
            return speed * 3.6f;
        }

        public static float KmhToMs(float speed)
        {
            return speed / 3.6f;
        }

        /// <summary>
        /// Calculates the speed at which the rider exits the takeoff transition in m/s.
        /// </summary>  
        /// <remarks>This calculation is being used while the takeoff is being placed. That can happen on a slope,
        /// and before that placement is confirmed, the angle of the ground underneath the takeoff is retrieved
        /// using the angle between <see cref="ObstacleBase{T}.GetRideDirection"/> and a flat plane</remarks>
        public static float GetExitSpeed(TakeoffBase takeoff, float timeStep = TIME_STEP)
        {
            float entrySpeed = takeoff.EntrySpeed;

            if (entrySpeed <= 0)
            {
                return 0;
            }

            // Calculate slope angle based on ride direction, as GetSlopeChange() may not be set yet
            Vector3 rideDirXz = Vector3.ProjectOnPlane(takeoff.GetRideDirection(), Vector3.up).normalized;
            float slopeAngleDeg = Vector3.SignedAngle(rideDirXz, takeoff.GetRideDirection(), -Vector3.Cross(Vector3.up, takeoff.GetRideDirection()));
            float slopeAngleRad = Mathf.Deg2Rad * slopeAngleDeg; // Convert to rad for easier understanding

            float rad270degrees = Mathf.Deg2Rad * 270f; // 270 degrees in radians

            float radius = takeoff.GetRadius();
            float endAngleRad = takeoff.GetEndAngle();
            float speed = entrySpeed;

            // Simulate rider traveling up the curved transition
            float angleTraveledRad = 0;
            float verticalRiseTraveled = 0;


            while (angleTraveledRad < endAngleRad)
            {
                // Calculate angle step based on current speed and radius
                float angleStepRad = speed * timeStep / radius;

                // Prevent overshooting the total angle
                if (angleTraveledRad + angleStepRad > endAngleRad)
                    angleStepRad = endAngleRad - angleTraveledRad;                

                angleTraveledRad += angleStepRad;

                // Current angle of surface relative to horizontal
                float currentSurfaceAngle = angleTraveledRad + slopeAngleRad;

                // Arc length traveled in this step
                float arcLength = radius * angleStepRad;

                // Vertical rise in this step

                // as the rise is calculated from 270 degrees (to copy the takeoffs transition), it has to be shifted upward by radius
                // to prevent negative values
                float verticalRiseAngleRad = rad270degrees + slopeAngleRad + angleTraveledRad;
                float verticalRise = radius * Mathf.Sin(verticalRiseAngleRad) + radius - verticalRiseTraveled;
                verticalRiseTraveled += verticalRise;

                // Energy lost to gravity
                float gravityEnergy = Gravity * RiderBikeMass * verticalRise;

                // Normal force includes weight component and centripetal force
                float normalForce = RiderBikeMass * (Gravity * Mathf.Cos(currentSurfaceAngle) +
                                                     (speed * speed) / radius);

                float frictionLoss = RollingDragCoefficient * normalForce * arcLength;
                // Air resistance
                float dragForce = 0.5f * AirDensity * AirDragCoefficient * FrontalArea * speed * speed;
                float dragLoss = dragForce * arcLength;
                // Net energy change
                float netEnergyChange = -gravityEnergy - frictionLoss - dragLoss;

                // Update speed using energy equation
                float speedSquaredChange = 2 * netEnergyChange / RiderBikeMass;
                if (speed * speed + speedSquaredChange >= 0)
                {
                    speed = Mathf.Sqrt(speed * speed + speedSquaredChange);
                }
                else
                {
                    // Rider doesn't have enough speed to complete the transition
                    return 0;
                }
            }

            return speed;
        }


        /// <summary>
        /// Calculates the speed at which the rider exits the landing in m/s.
        /// </summary>   
        public static float GetExitSpeed(LandingBase landing, Trajectory.TrajectoryPoint contactPoint, float timeStep = TIME_STEP)
        {
            Vector3 rideDirXz = Vector3.ProjectOnPlane(landing.GetRideDirection(), Vector3.up).normalized;
            float slopeAngleDeg = Vector3.SignedAngle(rideDirXz, landing.GetRideDirection(), -Vector3.Cross(Vector3.up, landing.GetRideDirection()));
            float slopeAngle = Mathf.Deg2Rad * slopeAngleDeg; // Convert to degrees for easier understanding

            float landingAngle = landing.GetSlopeAngle();
            float landingAngleAdjustedForSlope = landingAngle - slopeAngle;
            
            float transitionRadius = landing.GetRadius();
            
            Vector3 velocity = contactPoint.velocity;

            // here an exception is thrown if the rider does not have enough speed to exit the landing, because that should not happen
            // as landing is always sloped downwards, the rider should always have enough speed to exit
            if (!TryCalculateExitSpeed(velocity.magnitude, landing.GetSlopeLength(), out float speed, landingAngleAdjustedForSlope))
            {
                return 0;
            }

            float angleTraveled = 0;
            float angle270Rad = 270 * Mathf.Deg2Rad; // 270 degrees in radians

            float verticalDropTraveled = 0;

            float targetAngle = landingAngleAdjustedForSlope;

            while (angleTraveled < targetAngle)
            {
                // Calculate angle step based on current speed and radius
                float angleStep = speed * timeStep / transitionRadius;

                // Prevent overshooting the total angle
                if (angleTraveled + angleStep > targetAngle)
                    angleStep = targetAngle - angleTraveled;

                // Safety check - if step is too small, break out
                if (angleStep < 0.0001f)
                {
                    break;
                }

                angleTraveled += angleStep;

                // Current angle of surface relative to horizontal
                float currentSurfaceAngle = targetAngle - angleTraveled;

                // Arc length traveled in this step
                float arcLength = transitionRadius * angleStep;

                // Vertical drop in this step
                float transitionAngleRad = angle270Rad - targetAngle + angleTraveled;
                float verticalDrop = transitionRadius * Mathf.Sin(transitionAngleRad) + transitionRadius - verticalDropTraveled;
                verticalDropTraveled += verticalDrop;

                // Energy gained from gravity
                float gravityEnergy = Gravity * RiderBikeMass * verticalDrop;

                // Normal force includes weight component and centripetal force
                float normalForce = RiderBikeMass * (Gravity * Mathf.Cos(currentSurfaceAngle) +
                                                     (speed * speed) / transitionRadius);

                // Friction loss
                float frictionLoss = RollingDragCoefficient * normalForce * arcLength;

                // Air resistance
                float dragForce = 0.5f * AirDensity * AirDragCoefficient * FrontalArea * speed * speed;
                float dragLoss = dragForce * arcLength;

                // Net energy change
                float netEnergyChange = gravityEnergy - frictionLoss - dragLoss;

                // Update speed using energy equation
                float speedSquaredChange = 2 * netEnergyChange / RiderBikeMass;
                if (speed * speed + speedSquaredChange >= 0)
                {
                    speed = Mathf.Sqrt(speed * speed + speedSquaredChange);
                }
                else
                {
                    return 0;
                }
            }

            return speed;
        }
        
        /// <summary>
        /// Calculates the flight trajectory of a takeoff.
        /// </summary>
        /// <param name="normalizedAngle">The normalized angle of the curve from which the rider takes off. 0 for straight jump, -1/1 for -/+<see cref="TakeoffBase.GetMaxCarveAngle"/></param>
        /// <param name="timeStep">How long are the time intervals between the samples on the trajectory in seconds.</param>
        public static Trajectory GetFlightTrajectory(TakeoffBase takeoff, float normalizedAngle = 0, float timeStep = TIME_STEP)
        {
            Vector3 rideDirNormal = -Vector3.Cross(takeoff.GetRideDirection().normalized, Vector3.up).normalized;

            normalizedAngle = Mathf.Clamp(normalizedAngle, -1f, 1f);
            Vector3 position = takeoff.GetTransitionEnd() + rideDirNormal * takeoff.GetWidth() / 2 * normalizedAngle;

            float exitSpeed = takeoff.GetExitSpeed();
            Vector3 velocity = takeoff.GetTakeoffDirection(normalizedAngle).normalized * exitSpeed;

            Trajectory trajectory = new();
            
            // Air resistance calculation factors
            float airResistanceFactor = 0.5f * AirDensity * FrontalArea * AirDragCoefficient / RiderBikeMass;

            while (IsPositionValid(position))
            {
                trajectory.Add(position, velocity);

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
            }

            return trajectory;

            bool IsPositionValid(Vector3 pos)
            {
                bool isNotColliding;
                CoordinateStateHolder terrainState = TerrainManager.Instance.GetTerrainStateAt(pos);
                
                // if terrain is free, it can get changed under the position
                if (terrainState.GetState() == CoordinateState.Free)
                {
                    isNotColliding = true;
                }
                // if terrain is occupied, check if we are above the occupying element
                else if (terrainState is OccupiedCoordinateState occupiedState)
                {
                    ILineElement element = occupiedState.OccupyingElement;

                    // omit checking when above the takeoff we are computing the trajectory for
                    if (takeoff.GetTransform() == element.GetTransform())
                    {
                        return true;
                    }
                    
                    isNotColliding = pos.y + Mathf.Epsilon >= element.GetTransform().position.y + element.GetHeight();
                    
                    InternalDebug.Log($"Trajectory point {pos} on occupied position, is lower than occupying element {element.GetType()} (position {element.GetTransform().position}, height {element.GetHeight()}): {!isNotColliding}");
                }
                // if terrain is height set, check if we are above the terrain
                else /* (terrainState.GetState() == CoordinateState.HeightSet)*/
                {
                    isNotColliding = pos.y + Mathf.Epsilon >= TerrainManager.Instance.GetHeightAt(pos);
                    InternalDebug.Log($"Trajectory on heightSet position, is higher than terrain: {!isNotColliding}");
                }
                
                
                // make a bound on how far down the trajectory can go
                bool isNotTooFarDown = pos.y + Mathf.Epsilon >= takeoff.GetTransform().position.y - 2f;
                
                return isNotColliding && isNotTooFarDown;
            }
        }        
    }
}