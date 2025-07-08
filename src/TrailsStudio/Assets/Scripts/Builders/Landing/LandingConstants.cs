using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class LandingConstants
{
    public static float MIN_SLOPE_DEG = 30;
    public static float MAX_SLOPE_DEG = 70;

    public static float MIN_HEIGHT = 1;
    public static float MAX_HEIGHT = 6;

    public static float MIN_WIDTH = 2;
    public static float MAX_WIDTH = 7;

    public static float MIN_THICKNESS = 1;
    public static float MAX_THICKNESS = 2.5f;

    public static float MAX_ANGLE_BETWEEN_TRAJECTORY_AND_LANDING_DEG = 45f;

    [Tooltip("The minimum distance between the last line element and the new obstacle.")]
    public static float MIN_DISTANCE_FROM_TAKEOFF = 0.5f;
    [Tooltip("The maximum distance between the last line element and the new obstacle.")]
    public static float MAX_DISTANCE_FROM_TAKEOFF = 15;

    [Tooltip("The minimum distance after the landing where the rideout area must be free of obstacles.")]
    public static float RIDEOUT_CLEARANCE_DISTANCE = 5f;
}

