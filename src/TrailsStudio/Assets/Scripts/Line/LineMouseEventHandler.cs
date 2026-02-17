using System.Collections;
using System;
using UnityEngine;
using Assets.Scripts.Utilities;
using Assets.Scripts.Builders;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Assets.Scripts.Managers;


/// <summary>
/// Handles mouse events for GameObjects with a component derived from <see cref="ILineElement"/>
/// and with a <see cref="Collider"/>.
/// </summary>
public class LineMouseEventHandler : Singleton<LineMouseEventHandler>
{
    readonly Dictionary<int, Action<ILineElement>> _mouseOverEventDelegates = new();
    public event Action<ILineElement> OnMouseOverEvent
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

    readonly Dictionary<int, Action<ILineElement>> _mouseExitEventDelegates = new();
    public event Action<ILineElement> OnMouseExitEvent
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

    readonly Dictionary<int, Action<ILineElement>> _mouseClickEventDelegates = new();
    public event Action<ILineElement> OnMouseClickEvent
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

    ILineElement mouseOverObstacle;

    int subscribeCounter = 0;

    InputAction selectAction;

    private void Start()
    {
        selectAction = InputSystem.actions.FindAction("Select");
    }


    private void InvokeMouseOver(ILineElement obstacle)
    {
        mouseOverObstacle = obstacle;
        foreach (var action in _mouseOverEventDelegates.Values)
        {
            action?.Invoke(obstacle);
        }
    }

    private void InvokeMouseExit(ILineElement obstacle)
    {
        foreach (var action in _mouseExitEventDelegates.Values)
        {
            action?.Invoke(obstacle);
        }
        mouseOverObstacle = null;
    }

    private void InvokeMouseClick(ILineElement obstacle)
    {
        foreach (var action in _mouseClickEventDelegates.Values)
        {
            action?.Invoke(obstacle);
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
            if (Line.TryGetLineElementFromGameObject(hitObject, out var lineElement) && !StudioUIManager.IsPointerOverUI)
            {
                if (mouseOverObstacle != null && lineElement != mouseOverObstacle)
                {
                    
                    InvokeMouseExit(mouseOverObstacle);
                }

                InvokeMouseOver(lineElement);
                    
                if (selectAction.triggered)
                {
                    InvokeMouseClick(lineElement);
                }
            }                
            else if (mouseOverObstacle != null)
            {                
                InvokeMouseExit(mouseOverObstacle);
            }
        }
        else if (mouseOverObstacle != null)
        {
            InvokeMouseExit(mouseOverObstacle);
        }
    }

    void Outline(ILineElement lineElement)
    {
        lineElement.AddOutline();
    }

    void RemoveOutline(ILineElement lineElement)
    {
        lineElement.RemoveOutline();
    }

    public void EnableObstacleOutlining()
    {
        OnMouseOverEvent += Outline;
        OnMouseExitEvent += RemoveOutline;
    }

    public void DisableObstacleOutlining()
    {
        OnMouseOverEvent -= Outline;
        OnMouseExitEvent -= RemoveOutline;
    }
}
