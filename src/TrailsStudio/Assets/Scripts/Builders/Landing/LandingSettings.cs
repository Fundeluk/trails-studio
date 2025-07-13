using Assets.Scripts.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class LandingSettings
{
    public static SettingsField<float> MIN_SLOPE_DEG = new SettingsField<float>(
        "landing_min_slope_deg",
        "Minimum Landing Slope",
        "Minimal slope the landing can have in degrees.",
        35f,
        "°"
    );

    public static SettingsField<float> MAX_SLOPE_DEG = new SettingsField<float>(
        "landing_max_slope_deg",
        "Maximum Landing Slope",
        "Maximum slope the landing can have in degrees.",
        70f,
        "°"
    );

    public static SettingsField<float> MIN_HEIGHT = new SettingsField<float>(
        "landing_min_height",
        "Minimum Landing Height",
        "The minimum height of the landing.",
        0.75f,
        "m"
    );

    public static SettingsField<float> MAX_HEIGHT = new SettingsField<float>(
        "landing_max_height",
        "Maximum Landing Height",
        "The maximum height of the landing.",
        6f,
        "m"
    );

    public static SettingsField<float> MIN_WIDTH = new SettingsField<float>(
        "landing_min_width",
        "Minimum Landing Width",
        "The minimum width of the landing.",
        2f,
        "m"
    );

    public static SettingsField<float> MAX_WIDTH = new SettingsField<float>(
        "landing_max_width",
        "Maximum Landing Width",
        "The maximum width of the landing.",
        6f,
        "m"
    );

    public static SettingsField<float> MIN_THICKNESS = new SettingsField<float>(
        "landing_min_thickness",
        "Minimum Landing Thickness",
        "The minimum thickness of the landing.",
        0.5f,
        "m"
    );

    public static SettingsField<float> MAX_THICKNESS = new SettingsField<float>(
        "landing_max_thickness",
        "Maximum Landing Thickness",
        "The maximum thickness of the landing.",
        2f,
        "m"
    );

    public static SettingsField<float> MAX_ANGLE_BETWEEN_TRAJECTORY_AND_LANDING_DEG = new SettingsField<float>(
        "landing_max_angle_between_trajectory_and_landing_deg",
        "Maximum Trajectory Landing Angle",
        "The maximum angle between the trajectory and the landing's heading in degrees.",
        45f,
        "°"
    );

    public static SettingsField<float> MIN_DISTANCE_FROM_TAKEOFF = new SettingsField<float>(
        "landing_min_distance_from_takeoff",
        "Minimum Distance From Takeoff",
        "The minimum distance between the last line element and the new obstacle.",
        0.5f,
        "m"
    );

    public static SettingsField<float> MAX_DISTANCE_FROM_TAKEOFF = new SettingsField<float>(
        "landing_max_distance_from_takeoff",
        "Maximum Distance From Takeoff",
        "The maximum distance between the last line element and the new obstacle.",
        15f,
        "m"
    );

    public static SettingsField<float> RIDEOUT_CLEARANCE_DISTANCE = new SettingsField<float>(
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
            MIN_SLOPE_DEG,
            MAX_SLOPE_DEG,
            MIN_HEIGHT,
            MAX_HEIGHT,
            MIN_WIDTH,
            MAX_WIDTH,
            MIN_THICKNESS,
            MAX_THICKNESS,
            MAX_ANGLE_BETWEEN_TRAJECTORY_AND_LANDING_DEG,
            MIN_DISTANCE_FROM_TAKEOFF,
            MAX_DISTANCE_FROM_TAKEOFF,
            RIDEOUT_CLEARANCE_DISTANCE
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

