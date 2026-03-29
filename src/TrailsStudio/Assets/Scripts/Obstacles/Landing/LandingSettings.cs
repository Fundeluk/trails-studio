using System.Collections.Generic;
using Misc;

namespace Obstacles.Landing
{
    public static class LandingSettings
    {
        public static readonly SettingsField<float> MinSlopeDeg = new(
            "landing_min_slope_deg",
            "Minimum Landing Slope",
            "Minimal slope the landing can have in degrees.",
            30f,
            "°"
        );

        public static readonly SettingsField<float> MaxSlopeDeg = new(
            "landing_max_slope_deg",
            "Maximum Landing Slope",
            "Maximum slope the landing can have in degrees.",
            70f,
            "°"
        );

        public static readonly SettingsField<float> MinHeight = new(
            "landing_min_height",
            "Minimum Landing Height",
            "The minimum height of the landing.",
            0.75f,
            "m"
        );

        public static readonly SettingsField<float> MaxHeight = new(
            "landing_max_height",
            "Maximum Landing Height",
            "The maximum height of the landing.",
            6f,
            "m"
        );

        public static readonly SettingsField<float> MinWidth = new(
            "landing_min_width",
            "Minimum Landing Width",
            "The minimum width of the landing.",
            2f,
            "m"
        );

        public static readonly SettingsField<float> MaxWidth = new(
            "landing_max_width",
            "Maximum Landing Width",
            "The maximum width of the landing.",
            6f,
            "m"
        );

        public static readonly SettingsField<float> MinThickness = new(
            "landing_min_thickness",
            "Minimum Landing Thickness",
            "The minimum thickness of the landing.",
            0.5f,
            "m"
        );

        public static readonly SettingsField<float> MaxThickness = new(
            "landing_max_thickness",
            "Maximum Landing Thickness",
            "The maximum thickness of the landing.",
            2f,
            "m"
        );

        public static readonly SettingsField<float> MaxAngleBetweenTrajectoryAndLandingDeg = new(
            "landing_max_angle_between_trajectory_and_landing_deg",
            "Maximum Trajectory Landing Angle",
            "The maximum angle between the trajectory and the landing's heading in degrees.",
            45f,
            "°"
        );

        public static readonly SettingsField<float> MinDistanceFromTakeoff = new(
            "landing_min_distance_from_takeoff",
            "Minimum Distance From Takeoff",
            "The minimum distance between the last line element and the new obstacle.",
            0.5f,
            "m"
        );

        public static readonly SettingsField<float> MaxDistanceFromTakeoff = new(
            "landing_max_distance_from_takeoff",
            "Maximum Distance From Takeoff",
            "The maximum distance between the last line element and the new obstacle.",
            15f,
            "m"
        );

        public static readonly SettingsField<float> RideoutClearanceDistance = new(
            "landing_rideout_clearance_distance",
            "Rideout Clearance Distance",
            "The minimum distance after the landing where the rideout area must be free of obstacles.",
            5f,
            "m"
        );

        public static List<SettingsField<float>> GetAllSettings()
        {
            return new List<SettingsField<float>>
            {
                MinSlopeDeg,
                MaxSlopeDeg,
                MinHeight,
                MaxHeight,
                MinWidth,
                MaxWidth,
                MinThickness,
                MaxThickness,
                MaxAngleBetweenTrajectoryAndLandingDeg,
                MinDistanceFromTakeoff,
                MaxDistanceFromTakeoff,
                RideoutClearanceDistance
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

