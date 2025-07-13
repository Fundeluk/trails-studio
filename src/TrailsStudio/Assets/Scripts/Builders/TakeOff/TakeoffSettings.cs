using Assets.Scripts.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public static class TakeoffSettings
{
    public static SettingsField<float> MIN_BUILD_DISTANCE = new(
        "takeoff_min_build_distance",
        "Minimum Takeoff Build Distance",
        "The minimum distance between the takeoff and the line element before it.",
        1f,
        "m"
    );

    public static SettingsField<float> MAX_BUILD_DISTANCE = new(
        "takeoff_max_build_distance",
        "Maximum Takeoff Build Distance",
        "The maximum distance between the takeoff and the line element before it.",
        30f,
        "m"
    );

    public static SettingsField<float> MIN_RADIUS = new(
        "takeoff_min_radius",
        "Minimum Takeoff Radius",
        "The minimum radius of the takeoff.",
        1f,
        "m"
    );

    public static SettingsField<float> MAX_RADIUS = new(
        "takeoff_max_radius",
        "Maximum Takeoff Radius",
        "The maximum radius of the takeoff.",
        10f,
        "m"
    );

    public static SettingsField<float> MIN_HEIGHT = new(
        "takeoff_min_height",
        "Minimum Takeoff Height",
        "The minimum height of the takeoff.",
        0.75f,
        "m"
    );

    public static SettingsField<float> MAX_HEIGHT = new(
        "takeoff_max_height",
        "Maximum Takeoff Height",
        "The maximum height of the takeoff.",
        4f,
        "m"
    );

    public static SettingsField<float> MAX_CARVE_ANGLE_DEG = new(
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
            MIN_BUILD_DISTANCE,
            MAX_BUILD_DISTANCE,
            MIN_RADIUS,
            MAX_RADIUS,
            MIN_HEIGHT,
            MAX_HEIGHT,
            MAX_CARVE_ANGLE_DEG
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

