using System.Collections.Generic;
using Misc;

namespace TerrainEditing.Slope
{
    public static class SlopeSettings
    {
        public static readonly SettingsField<float> MaxHeightDifference = new SettingsField<float>(
            "slope_max_height_difference",
            "Maximum Height Difference",
            "The maximum height difference the slope can have.",
            10f,
            "m"
        );

        public static readonly SettingsField<float> MinHeightDifference = new SettingsField<float>(
            "slope_min_height_difference",
            "Minimum Height Difference", 
            "The minimum height difference the slope can have.",
            -10f,
            "m"
        );

        public static readonly SettingsField<float> MaxLength = new SettingsField<float>(
            "slope_max_length",
            "Maximum Slope Length",
            "The maximum length the slope can have.",
            100f,
            "m"
        );

        public static readonly SettingsField<float> MinLength = new SettingsField<float>(
            "slope_min_length",
            "Minimum Slope Length",
            "The minimum length the slope can have.",
            1f,
            "m"
        );

        public static readonly SettingsField<float> MinSlopeAngleDeg = new SettingsField<float>(
            "slope_min_slope_angle_deg",
            "Minimum Slope Angle",
            "The minimum slope angle in degrees.",
            5f,
            "°"
        );

        public static readonly SettingsField<float> MaxSlopeAngleDeg = new SettingsField<float>(
            "slope_max_slope_angle_deg",
            "Maximum Slope Angle",
            "The maximum slope angle in degrees.",
            25f,
            "°"
        );

        public static readonly SettingsField<float> MinBuildDistance = new SettingsField<float>(
            "slope_min_build_distance",
            "Minimum Build Distance",
            "The minimum distance between the last line element and the slope start.",
            0f,
            "m"
        );

        public static readonly SettingsField<float> MaxBuildDistance = new SettingsField<float>(
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
                MaxHeightDifference,
                MinHeightDifference,
                MaxLength,
                MinLength,
                MinSlopeAngleDeg,
                MaxSlopeAngleDeg,
                MinBuildDistance,
                MaxBuildDistance
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
