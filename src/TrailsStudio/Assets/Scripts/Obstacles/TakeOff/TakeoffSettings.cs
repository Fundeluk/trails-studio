using System.Collections.Generic;
using Misc;

namespace Obstacles.TakeOff
{
    public static class TakeoffSettings
    {
        public static readonly SettingsField<float> MinBuildDistance = new(
            "takeoff_min_build_distance",
            "Minimum Takeoff Build Distance",
            "The minimum distance between the takeoff and the line element before it.",
            1f,
            "m"
        );

        public static readonly SettingsField<float> MaxBuildDistance = new(
            "takeoff_max_build_distance",
            "Maximum Takeoff Build Distance",
            "The maximum distance between the takeoff and the line element before it.",
            30f,
            "m"
        );

        public static readonly SettingsField<float> MinRadius = new(
            "takeoff_min_radius",
            "Minimum Takeoff Radius",
            "The minimum radius of the takeoff.",
            1f,
            "m"
        );

        public static readonly SettingsField<float> MaxRadius = new(
            "takeoff_max_radius",
            "Maximum Takeoff Radius",
            "The maximum radius of the takeoff.",
            10f,
            "m"
        );

        public static readonly SettingsField<float> MinHeight = new(
            "takeoff_min_height",
            "Minimum Takeoff Height",
            "The minimum height of the takeoff.",
            0.75f,
            "m"
        );

        public static readonly SettingsField<float> MaxHeight = new(
            "takeoff_max_height",
            "Maximum Takeoff Height",
            "The maximum height of the takeoff.",
            4f,
            "m"
        );

        public static readonly SettingsField<float> MaxCarveAngleDeg = new(
            "takeoff_max_carve_angle",
            "Maximum Carve Angle",
            "The maximum angle at which the rider can exit the takeoff into the side.",
            45f,
            "°"
        );

        public static List<SettingsField<float>> GetAllSettings()
        {
            return new List<SettingsField<float>>
            {
                MinBuildDistance,
                MaxBuildDistance,
                MinRadius,
                MaxRadius,
                MinHeight,
                MaxHeight,
                MaxCarveAngleDeg
            };
        }

        public static void ResetToDefaults()
        {
            foreach (var setting in GetAllSettings())
            {
                setting.ResetToDefault();
            }
        }
    }
}

