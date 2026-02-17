using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public static class ButtonExtensions
    {
        /// <summary>
        /// Toggles a button's enabled state and applies/removes the "sidebar-button__disabled" USS class.
        /// </summary>
        /// <param name="button">The button to toggle.</param>
        /// <param name="enable">The desired enabled state.</param>
        public static void Toggle(this Button button, bool enable)
        {
            if (button == null)
            {
                Debug.LogWarning("Button is null, cannot toggle.");
                return;
            }

            if (button.enabledSelf == enable)
                return;

            if (enable)
            {
                button.RemoveFromClassList("sidebar-button__disabled");
            }
            else
            {
                button.AddToClassList("sidebar-button__disabled");
            }

            button.SetEnabled(enable);
        }
    }
}
