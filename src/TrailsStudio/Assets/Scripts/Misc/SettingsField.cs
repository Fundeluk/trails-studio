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
        
        private T cachedValue;
        private bool isInitialized;

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
            if (isInitialized)
            {
                return cachedValue;
            }
            else if (typeof(T) == typeof(int))
            {
                cachedValue = (T)(object)PlayerPrefs.GetInt(playerPrefKey, Convert.ToInt32(defaultValue));
            }            
            else if (typeof(T) == typeof(float))
            {
                cachedValue = (T)(object)PlayerPrefs.GetFloat(playerPrefKey, Convert.ToSingle(defaultValue));
            }            
            else if (typeof(T) == typeof(string))
            {
                cachedValue = (T)(object)PlayerPrefs.GetString(playerPrefKey, defaultValue.ToString());
            }            
            else if (typeof(T) == typeof(bool))
            {
                cachedValue = (T)(object)(PlayerPrefs.GetInt(playerPrefKey, Convert.ToBoolean(defaultValue) ? 1 : 0) == 1);
            }
            else
            {
                throw new NotSupportedException($"Type {typeof(T)} is not supported for SettingsField.");
            }
            
            isInitialized = true;
            return cachedValue;
        }
        
        public void SetValue(T value)
        {
            cachedValue = value;
            isInitialized = true;

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
            
            PlayerPrefs.Save();
        }

        public static implicit operator T(SettingsField<T> field) => field.GetValue();

        public override string ToString() => GetValue().ToString() + Unit;

    }

}
