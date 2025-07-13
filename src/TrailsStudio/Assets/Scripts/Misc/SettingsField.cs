using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Misc
{    
    public class SettingsField<T>
    {
        public readonly string playerPrefKey;
        public readonly string displayName;
        public readonly string description;
        public readonly string unit;
        public readonly T defaultValue;
        //private T cachedValue;

        public SettingsField(string playerPrefKey, string displayName, string description, T defaultValue, string unit)
        {
            this.playerPrefKey = playerPrefKey;
            this.displayName = displayName;
            this.description = description;
            this.defaultValue = defaultValue;
            this.unit = unit;
            //this.cachedValue = GetValue();
        }

        public void ResetToDefault()
        {
            SetValue(defaultValue);
        }

        public T GetValue()
        {
            if (typeof(T) == typeof(int))
            {
                return (T)(object)PlayerPrefs.GetInt(playerPrefKey, Convert.ToInt32(defaultValue));
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)PlayerPrefs.GetFloat(playerPrefKey, Convert.ToSingle(defaultValue));
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)PlayerPrefs.GetString(playerPrefKey, defaultValue.ToString());
            }
            else if (typeof(T) == typeof(bool))
            {
                return (T)(object)(PlayerPrefs.GetInt(playerPrefKey, Convert.ToBoolean(defaultValue) ? 1 : 0) == 1);
            }
            else
            {
                throw new NotSupportedException($"Type {typeof(T)} is not supported for SettingsField.");
            }
        }

        //public T GetCachedValue() => cachedValue;

        public void SetValue(T value)
        {
            //cachedValue = value;

            if (typeof(T) == typeof(int))
            {
                PlayerPrefs.SetInt(playerPrefKey, Convert.ToInt32(value));
            }
            else if (typeof(T) == typeof(float))
            {
                PlayerPrefs.SetFloat(playerPrefKey, Convert.ToSingle(value));
            }
            else if (typeof(T) == typeof(string))
            {
                PlayerPrefs.SetString(playerPrefKey, value.ToString());
            }
            else if (typeof(T) == typeof(bool))
            {
                PlayerPrefs.SetInt(playerPrefKey, Convert.ToBoolean(value) ? 1 : 0);
            }
            else
            {
                throw new NotSupportedException($"Type {typeof(T)} is not supported for SettingsField.");
            }
        }

        public static implicit operator T(SettingsField<T> field) => field.GetValue();

        public override string ToString() => GetValue().ToString() + unit;

    }

}
