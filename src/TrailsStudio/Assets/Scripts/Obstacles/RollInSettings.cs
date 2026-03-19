using System.Collections.Generic;
using Misc;

namespace Obstacles
{
    public static class RollInSettings
    {
        public static readonly SettingsField<float> MinHeight = new(
            "roll-in_min_height",
            "Minimum Roll-in Height",
            "The minimum height of the roll-in.",
            2f,
            "m"
        );

        public static readonly SettingsField<float> MaxHeight = new(
            "roll-in_max_height",
            "Maximum Roll-in Height",
            "The maximum height of the roll-in.",
            10f,
            "m"
        );
        
        public static readonly SettingsField<float> MinAngleDeg = new(
            "roll-in_min_angle",
            "Maximum Carve Angle",
            "The minimum angle of the roll-in.",
            30f,
            "°"
        );
        
        public static readonly SettingsField<float> MaxAngleDeg = new(
            "roll-in_max_angle",
            "Maximum Carve Angle",
            "The maximum angle of the roll-in.",
            70f,
            "°"
        );
        
        public static List<SettingsField<float>> GetAllSettings()
        {
            return new List<SettingsField<float>>
            {
                MinHeight,
                MaxHeight,
                MinAngleDeg,
                MaxAngleDeg
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