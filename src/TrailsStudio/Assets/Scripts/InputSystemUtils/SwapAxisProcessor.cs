using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InputSystemUtils
{
    [Serializable]
    public class SwapAxisProcessor : InputProcessor<Vector2>
    {
        [Tooltip("send x-axis to y-axis and vice versa")]
        public bool swapAxis = true;

        public override Vector2 Process(Vector2 value, InputControl control)
        {
            return swapAxis ? new(value.y, value.x) : value;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod]
        public static void Init()
        {
            InternalDebug.Log("Initializing SwapAxisProcessor");
            InputSystem.RegisterProcessor<SwapAxisProcessor>();
        }
    }
}
