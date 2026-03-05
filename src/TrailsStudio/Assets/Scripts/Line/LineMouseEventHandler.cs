using System;
using System.Collections.Generic;
using Managers;
using Misc;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LineSystem
{
    /// <summary>
    /// Handles mouse events for GameObjects with a component derived from <see cref="ILineElement"/>
    /// and with a <see cref="Collider"/>.
    /// </summary>
    public class LineMouseEventHandler : Singleton<LineMouseEventHandler>
    {
        private readonly Dictionary<int, Action<ILineElement>> mouseOverEventDelegates = new();
        public event Action<ILineElement> OnMouseOverEvent
        {
            add
            {
                mouseOverEventDelegates.Add(value.GetHashCode(), value);
                subscribeCounter++;
            }
            remove
            {
                mouseOverEventDelegates.Remove(value.GetHashCode());
                subscribeCounter--;
            }
        }

        private readonly Dictionary<int, Action<ILineElement>> mouseExitEventDelegates = new();
        public event Action<ILineElement> OnMouseExitEvent
        {
            add
            {
                mouseExitEventDelegates.Add(value.GetHashCode(), value);
                subscribeCounter++;
            }
            remove
            {
                mouseExitEventDelegates.Remove(value.GetHashCode());
                subscribeCounter--;
            }
        }

        private readonly Dictionary<int, Action<ILineElement>> mouseClickEventDelegates = new();
        public event Action<ILineElement> OnMouseClickEvent
        {
            add
            {
                mouseClickEventDelegates.Add(value.GetHashCode(), value);
                subscribeCounter++;
            }
            remove
            {
                mouseClickEventDelegates.Remove(value.GetHashCode());
                subscribeCounter--;
            }
        }

        private ILineElement mouseOverObstacle;

        private int subscribeCounter;

        private InputAction selectAction;

        private void Start()
        {
            selectAction = InputSystem.actions.FindAction("Select");
        }


        private void InvokeMouseOver(ILineElement obstacle)
        {
            mouseOverObstacle = obstacle;
            foreach (var action in mouseOverEventDelegates.Values)
            {
                action?.Invoke(obstacle);
            }
        }

        private void InvokeMouseExit(ILineElement obstacle)
        {
            foreach (var action in mouseExitEventDelegates.Values)
            {
                action?.Invoke(obstacle);
            }
            mouseOverObstacle = null;
        }

        private void InvokeMouseClick(ILineElement obstacle)
        {
            foreach (var action in mouseClickEventDelegates.Values)
            {
                action?.Invoke(obstacle);
            }
        }   

        // Update is called once per frame
        private void Update()
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

        private static void Outline(ILineElement lineElement)
        {
            lineElement.AddOutline();
        }

        private static void RemoveOutline(ILineElement lineElement)
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
}
