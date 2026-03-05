using System;
using UnityEngine;

namespace Misc
{    
    public class SettingsField<T>
    {
        private readonly string playerPrefKey;
        private readonly T defaultValue;
        
        public readonly string DisplayName;
        public readonly string Description;
        public readonly string Unit;

        public SettingsField(string playerPrefKey, string displayName, string description, T defaultValue, string unit)
        {
            this.playerPrefKey = playerPrefKey;
            this.DisplayName = displayName;
            this.Description = description;
            this.defaultValue = defaultValue;
            this.Unit = unit;
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
            
            if (typeof(T) == typeof(float))
            {
                return (T)(object)PlayerPrefs.GetFloat(playerPrefKey, Convert.ToSingle(defaultValue));
            }
            
            if (typeof(T) == typeof(string))
            {
                return (T)(object)PlayerPrefs.GetString(playerPrefKey, defaultValue.ToString());
            }
            
            if (typeof(T) == typeof(bool))
            {
                return (T)(object)(PlayerPrefs.GetInt(playerPrefKey, Convert.ToBoolean(defaultValue) ? 1 : 0) == 1);
            }
            
            throw new NotSupportedException($"Type {typeof(T)} is not supported for SettingsField.");
        }
        
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

        public override string ToString() => GetValue().ToString() + Unit;

    }

}
