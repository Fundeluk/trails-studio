using System.Collections.Generic;
using Misc;

namespace PhysicsManager
{
    public static class PhysicsSettings
    {
        /// <summary>
        /// The frontal area of a rider on a BMX in square meters. Used for aerodynamic calculations.
        /// </summary>
        /// <remarks>
        /// Default value sourced from https://link.springer.com/article/10.1007/s12283-017-0234-1.
        /// </remarks>
        public static readonly SettingsField<float> FrontalArea = new(
            "frontal_area_m2",
            "Frontal area of a rider",
            "The frontal area of a rider on a BMX in square meters. Used for aerodynamic calculations.",
            0.5f,
            "m^2"
        );

        /// <summary>
        /// The rolling drag coefficient of the rider and bike. Used for calculating rolling resistance.
        /// </summary>
        /// <remarks>
        /// Default value sourced from https://energiazero.org/cartelle/risparmio_energetico//rolling%20friction%20and%20rolling%20resistance.pdf.
        /// </remarks>
        public static readonly SettingsField<float> RollingDragCoefficient = new(
            "rolling_drag_coefficient",
            "Rolling Drag Coefficient",
            "The rolling drag coefficient of the rider and bike. Used for calculating rolling resistance.",
            0.008f,
            ""
        );

        
        /// <summary>
        /// The aerodynamic drag coefficient.
        /// </summary>
        /// <remarks>
        /// Based on https://www.engineeringtoolbox.com/drag-coefficient-d_627.html
        /// </remarks>
        public static readonly SettingsField<float> AirDragCoefficient = new(
            "air_drag_coefficient",
            "Air Drag Coefficient",
            "The aerodynamic drag coefficient.",
            1f,
            ""
        );

        /// <summary>
        /// Air density in kg/m^3.
        /// </summary>
        public static readonly SettingsField<float> AirDensity = new(
            "air_density",
            "Air Density",
            "Air density in kg/m^3. This is used for aerodynamic calculations.",
            1.2f,
            "kg/m^3"
        );

        /// <summary>
        /// Force of gravity in m/s^2.
        /// </summary>
        public static readonly SettingsField<float> Gravity = new(
            "gravity",
            "Gravity",
            "Force of gravity in m/s^2.",
            9.81f,
            "m/s^2"
        );

        /// <summary>
        /// Mass of the rider and the bike in kg.
        /// </summary>
        public static readonly SettingsField<float> RiderBikeMass = new(
            "rider_bike_mass",
            "Rider & Bike Mass",
            "Mass of the rider and the bike in kg.",
            100f,
            "kg"
        );

        public static List<SettingsField<float>> GetAllSettings()
        {
            return new List<SettingsField<float>>
            {
                FrontalArea,
                RollingDragCoefficient,
                AirDragCoefficient,
                AirDensity,
                Gravity,
                RiderBikeMass,
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