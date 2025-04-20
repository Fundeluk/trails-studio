using System.Collections;
using System;
using UnityEngine;
using Assets.Scripts.Utilities;
using Assets.Scripts.Builders;
using UnityEngine.InputSystem;
using System.Collections.Generic;


/// <summary>
/// Handles mouse events for GameObjects tagged with <see cref="Line.LINE_ELEMENT_TAG"/> tag
/// and with <see cref="Collider"/>s on the line.
/// </summary>
public class LineMouseEventHandler : Singleton<LineMouseEventHandler>
{
    readonly Dictionary<int, Action<GameObject>> _mouseOverEventDelegates = new();
    public event Action<GameObject> OnMouseOverEvent
    {
        add
        {
            _mouseOverEventDelegates.Add(value.GetHashCode(), value);
            subscribeCounter++;
        }
        remove
        {
            _mouseOverEventDelegates.Remove(value.GetHashCode());
            subscribeCounter--;
        }
    }

    readonly Dictionary<int, Action<GameObject>> _mouseExitEventDelegates = new();
    public event Action<GameObject> OnMouseExitEvent
    {
        add
        {
            _mouseExitEventDelegates.Add(value.GetHashCode(), value);
            subscribeCounter++;
        }
        remove
        {
            _mouseExitEventDelegates.Remove(value.GetHashCode());
            subscribeCounter--;
        }
    }

    readonly Dictionary<int, Action<GameObject>> _mouseClickEventDelegates = new();
    public event Action<GameObject> OnMouseClickEvent
    {
        add
        {
            _mouseClickEventDelegates.Add(value.GetHashCode(), value);
            subscribeCounter++;
        }
        remove
        {
            _mouseClickEventDelegates.Remove(value.GetHashCode());
            subscribeCounter--;
        }
    }

    GameObject mouseOverObject;

    TagHandle lineElementTag;

    int subscribeCounter = 0;

    InputAction selectAction;

    private void Start()
    {
        lineElementTag = TagHandle.GetExistingTag(Line.LINE_ELEMENT_TAG);
        selectAction = InputSystem.actions.FindAction("Select");
    }

    private void InvokeMouseOver(GameObject gameObject)
    {
        mouseOverObject = gameObject;
        foreach (var action in _mouseOverEventDelegates.Values)
        {
            action?.Invoke(gameObject);
        }
    }

    private void InvokeMouseExit(GameObject gameObject)
    {
        foreach (var action in _mouseExitEventDelegates.Values)
        {
            action?.Invoke(gameObject);
        }
        mouseOverObject = null;
    }

    private void InvokeMouseClick(GameObject gameObject)
    {
        foreach (var action in _mouseClickEventDelegates.Values)
        {
            action?.Invoke(gameObject);
        }
    }   

    // Update is called once per frame
    void Update()
    {
        if (subscribeCounter == 0)
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (hitObject.CompareTag(lineElementTag))
            {
                if (mouseOverObject != null && hitObject != mouseOverObject)
                {
                    
                    InvokeMouseExit(mouseOverObject);
                }

                InvokeMouseOver(hitObject);
                    
                if (selectAction.triggered)
                {
                    InvokeMouseClick(hitObject);
                }
            }                
            else if (mouseOverObject != null)
            {                
                InvokeMouseExit(mouseOverObject);
            }
        }
        else if (mouseOverObject != null)
        {
            InvokeMouseExit(mouseOverObject);
        }
    }
}
