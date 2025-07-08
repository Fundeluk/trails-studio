using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Builders.Slope
{
    public static class SlopeConstants
    {
        public static float MAX_HEIGHT_DIFFERENCE = 10;
        public static float MIN_HEIGHT_DIFFERENCE = -10;
        public static float MAX_LENGTH = 100;
        public static float MIN_LENGTH = 1;

        public static float MIN_SLOPE_ANGLE_DEG = 5f; // Minimum slope angle in degrees
        public static float MAX_SLOPE_ANGLE_DEG = 25f; // Maximum slope angle in degrees

        // the minimum and maximum distances between the last line element and new obstacle
        [Header("Build bounds")]
        [Tooltip("The minimum distance between the last line element and the new obstacle.")]
        public static float MIN_BUILD_DISTANCE = 0;
        [Tooltip("The maximum distance between the last line element and the new obstacle.")]
        public static float MAX_BUILD_DISTANCE = 30;
    }
}
