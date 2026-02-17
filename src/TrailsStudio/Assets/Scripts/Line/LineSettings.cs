using Assets.Scripts.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class LineSettings
{
    public static SettingsField<float> MIN_EXIT_SPEED_MS = new SettingsField<float>(
        "line_min_exit_speed_ms",
        "Minimum Exit Speed",
        "The minimum speed at which a rider can exit line elements in meters per second.",
        4.167f, // 15 km/h converted to m/s (15 / 3.6)
        "m/s"
    );

    public static SettingsField<float> MAX_EXIT_SPEED_MS = new SettingsField<float>(
        "line_max_exit_speed_ms",
        "Maximum Exit Speed",
        "The maximum speed at which a rider can exit line elements in meters per second.",
        19.444f, // 70 km/h converted to m/s (70 / 3.6)
        "m/s"
    );

    public static List<SettingsField<float>> GetAllSettings()
    {
        return new List<SettingsField<float>>()
        {
            MIN_EXIT_SPEED_MS,
            MAX_EXIT_SPEED_MS
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

