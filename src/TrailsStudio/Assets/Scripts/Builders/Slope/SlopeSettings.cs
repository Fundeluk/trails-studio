using Assets.Scripts.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Builders.Slope
{
    public static class SlopeSettings
    {
        public static SettingsField<float> MAX_HEIGHT_DIFFERENCE = new SettingsField<float>(
            "slope_max_height_difference",
            "Maximum Height Difference",
            "The maximum height difference the slope can have.",
            10f,
            "m"
        );

        public static SettingsField<float> MIN_HEIGHT_DIFFERENCE = new SettingsField<float>(
            "slope_min_height_difference",
            "Minimum Height Difference", 
            "The minimum height difference the slope can have.",
            -10f,
            "m"
        );

        public static SettingsField<float> MAX_LENGTH = new SettingsField<float>(
            "slope_max_length",
            "Maximum Slope Length",
            "The maximum length the slope can have.",
            100f,
            "m"
        );

        public static SettingsField<float> MIN_LENGTH = new SettingsField<float>(
            "slope_min_length",
            "Minimum Slope Length",
            "The minimum length the slope can have.",
            1f,
            "m"
        );

        public static SettingsField<float> MIN_SLOPE_ANGLE_DEG = new SettingsField<float>(
            "slope_min_slope_angle_deg",
            "Minimum Slope Angle",
            "The minimum slope angle in degrees.",
            5f,
            "°"
        );

        public static SettingsField<float> MAX_SLOPE_ANGLE_DEG = new SettingsField<float>(
            "slope_max_slope_angle_deg",
            "Maximum Slope Angle",
            "The maximum slope angle in degrees.",
            25f,
            "°"
        );

        public static SettingsField<float> MIN_BUILD_DISTANCE = new SettingsField<float>(
            "slope_min_build_distance",
            "Minimum Build Distance",
            "The minimum distance between the last line element and the slope start.",
            0f,
            "m"
        );

        public static SettingsField<float> MAX_BUILD_DISTANCE = new SettingsField<float>(
            "slope_max_build_distance",
            "Maximum Build Distance",
            "The maximum distance between the last line element and the slope start.",
            30f,
            "m"
        );

        public static List<SettingsField<float>> GetAllSettings()
        {
            return new List<SettingsField<float>>
            {
                MAX_HEIGHT_DIFFERENCE,
                MIN_HEIGHT_DIFFERENCE,
                MAX_LENGTH,
                MIN_LENGTH,
                MIN_SLOPE_ANGLE_DEG,
                MAX_SLOPE_ANGLE_DEG,
                MIN_BUILD_DISTANCE,
                MAX_BUILD_DISTANCE
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
