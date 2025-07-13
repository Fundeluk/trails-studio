
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.States;
using Assets.Scripts.Builders;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Assets.Scripts.Managers;
using Unity.VisualScripting;

namespace Assets.Scripts.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class DeleteUI : MonoBehaviour
    {
        private VisualElement root;

        private Button cancelButton;
        private Button deleteButton;

        [SerializeField]
        Material dirtMaterial;

        [SerializeField]
        Material canDeleteMaterial;

        [SerializeField]
        Material cantDeleteMaterial;

        

        ILineElement selectedObstacle = null;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;

            cancelButton = root.Q<Button>("CancelButton");
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);  

            deleteButton = root.Q<Button>("DeleteButton");
            deleteButton.RegisterCallback<ClickEvent>(DeleteClicked);
            
            deleteButton.SetEnabled(false);

            selectedObstacle = null; 
            
            LineMouseEventHandler.Instance.OnMouseClickEvent += OnObstacleClick;
            LineMouseEventHandler.Instance.OnMouseOverEvent += OnObstacleMouseOver;
            LineMouseEventHandler.Instance.OnMouseExitEvent += OnObstacleMouseExit;
        }

        private void OnDisable()
        {
            LineMouseEventHandler.Instance.OnMouseClickEvent -= OnObstacleClick;
            LineMouseEventHandler.Instance.OnMouseOverEvent -= OnObstacleMouseOver;
            LineMouseEventHandler.Instance.OnMouseExitEvent -= OnObstacleMouseExit;

            if (selectedObstacle != null)
            {
                selectedObstacle.GetTransform().GetComponent<MeshRenderer>().material = dirtMaterial;
                selectedObstacle = null;
            }

            cancelButton.UnregisterCallback<ClickEvent>(CancelClicked);
            deleteButton.UnregisterCallback<ClickEvent>(DeleteClicked);
        }

        private void CancelClicked(ClickEvent evt)
        {
            StateController.Instance.ChangeState(new DefaultState());
        }

        private void DeleteClicked(ClickEvent evt)
        {
            if (selectedObstacle != null)
            {
                int index = selectedObstacle.GetIndex();
                bool isLanding = selectedObstacle is Landing;
                
                // change camera target to the new last obstacle
                CameraManager.Instance.DetailedView(Line.Instance[index - 1]);

                Line.Instance.DestroyLineElementsFromIndex(index);

                selectedObstacle = null;                

                if (isLanding)
                {
                    // after landing deletion, go back to landing positioning state immediately
                    StateController.Instance.ChangeState(new LandingBuildState());
                }
                // if there is nothing else to delete
                else if (Line.Instance.Count <= 1)
                {
                    // nothing else can be deleted, go back to default state
                    StateController.Instance.ChangeState(new DefaultState());
                }

                SlopeChange slope = TerrainManager.Instance.ActiveSlope;
                if (slope != null && !slope.IsBuiltOn)
                {
                    StudioUIManager.Instance.GetSidebar().DeleteButtonEnabled = false;
                    StateController.Instance.ChangeState(new DefaultState());
                }
            }            
        }        
            

        void ResetSelectedObstacle()
        {
            if (selectedObstacle != null)
            {
                selectedObstacle.GetTransform().GetComponent<MeshRenderer>().material = dirtMaterial;
                selectedObstacle = null;
            }
        }

        static bool CanDelete(Takeoff takeoff)
        {
            SlopeChange slope = TerrainManager.Instance.ActiveSlope;
            bool noSlopeInterferes = slope == null || slope.IsBuiltOn;
            return takeoff.GetIndex() >= Line.Instance.Count - 2 && noSlopeInterferes;
        }

        static bool CanDelete(Landing landing)
        {
            SlopeChange slope = TerrainManager.Instance.ActiveSlope;
            bool noSlopeInterferes = slope == null || slope.IsBuiltOn;
            return landing.GetIndex() == Line.Instance.Count - 1 && noSlopeInterferes;
        }

        private void SelectObstacle(ILineElement obstacle)
        {
            selectedObstacle = obstacle;
            obstacle.GetTransform().GetComponent<MeshRenderer>().material = canDeleteMaterial;
            deleteButton.SetEnabled(true);
        }

        private void OnObstacleClick(ILineElement obstacle)
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            ResetSelectedObstacle();

            if (obstacle is Takeoff takeoff && CanDelete(takeoff))
            {                
                SelectObstacle(takeoff);
            }
            else if (obstacle is Landing landing && CanDelete(landing))
            {                
                SelectObstacle(landing);                
            }
        }

        
        private void OnObstacleMouseOver(ILineElement obstacle)
        {
            if (obstacle is Takeoff takeoff)
            {
                if (CanDelete(takeoff))
                {
                    takeoff.GetComponent<MeshRenderer>().material = canDeleteMaterial;
                }
                else
                {
                    takeoff.GetComponent<MeshRenderer>().material = cantDeleteMaterial;
                }
            }            
            else if (obstacle is Landing landing)
            {
                if (CanDelete(landing))
                {
                    landing.GetComponent<MeshRenderer>().material = canDeleteMaterial;
                }
                else
                {
                    landing.GetComponent<MeshRenderer>().material = cantDeleteMaterial;
                }
            }
        }          

        private void OnObstacleMouseExit(ILineElement obstacle)
        {
            if (obstacle is Takeoff takeoff && obstacle != selectedObstacle)
            {
                takeoff.GetComponent<MeshRenderer>().material = dirtMaterial;
            }
            else if (obstacle is Landing landing && obstacle != selectedObstacle)
            {
                landing.GetComponent<MeshRenderer>().material = dirtMaterial;
            }
        }        
    }
}