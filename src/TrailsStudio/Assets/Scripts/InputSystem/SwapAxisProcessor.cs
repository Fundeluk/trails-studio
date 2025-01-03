using UnityEngine;
using System;
using UnityEditor;
using UnityEngine.InputSystem;


[Serializable]
public class SwapAxisProcessor : InputProcessor<Vector2>
{
    [Tooltip("send x-axis to y-axis and vise versa")]
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
        Debug.Log("Initializing SwapAxisProcessor");
        InputSystem.RegisterProcessor<SwapAxisProcessor>();
    }
}
