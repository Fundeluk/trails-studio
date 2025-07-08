using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public static class TakeoffConstants
{
    // the minimum and maximum distances between the last line element and new obstacle
    [Header("Build distance limits")]
    [Tooltip("The minimum distance between the last line element and the new obstacle.")]
    public static float MIN_BUILD_DISTANCE = 1;
    [Tooltip("The maximum distance between the last line element and the new obstacle.")]
    public static float MAX_BUILD_DISTANCE = 30;

    public static float MIN_RADIUS = 1;
    public static float MAX_RADIUS = 10;

    public static float MIN_HEIGHT = 0.75f;
    public static float MAX_HEIGHT = MAX_RADIUS;

    public static float MAX_CARVE_ANGLE_DEG = 45f; // Maximum carve angle in degrees
}

