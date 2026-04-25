using LineSystem;
using Managers;
using Obstacles.Landing;
using Obstacles.TakeOff;
using TerrainEditing;
using TerrainEditing.Slope;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace States
{
    public class DeleteState : State
    {
        private ILineElement selectedObstacle = null;
        private DeleteUI deleteUI;

        protected override void OnEnter()
        {
            CameraManager.Instance.SplineCamView();
            
            StudioUIManager.Instance.ShowUI(StudioUIManager.Instance.deleteUI);
            deleteUI = StudioUIManager.Instance.deleteUI.GetComponent<DeleteUI>();

            deleteUI.OnCancelClicked += HandleCancelClicked;
            deleteUI.OnDeleteClicked += HandleDeleteClicked;

            LineMouseEventHandler.Instance.OnMouseClickEvent += OnObstacleClick;
            LineMouseEventHandler.Instance.OnMouseOverEvent += OnObstacleMouseOver;
            LineMouseEventHandler.Instance.OnMouseExitEvent += OnObstacleMouseExit;

            deleteUI.ToggleDeleteButton(false);
            selectedObstacle = null;
        }
        
        protected override void OnExit()
        {
            if (deleteUI != null)
            {
                deleteUI.OnCancelClicked -= HandleCancelClicked;
                deleteUI.OnDeleteClicked -= HandleDeleteClicked;
            }

            if (LineMouseEventHandler.Instance != null)
            {
                LineMouseEventHandler.Instance.OnMouseClickEvent -= OnObstacleClick;
                LineMouseEventHandler.Instance.OnMouseOverEvent -= OnObstacleMouseOver;
                LineMouseEventHandler.Instance.OnMouseExitEvent -= OnObstacleMouseExit;
            }

            ResetSelectedObstacle();
        }

        private void HandleCancelClicked()
        {
            StateController.Instance.ChangeState(new IdleState());
        }

        private void HandleDeleteClicked()
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
                else if (Line.Instance.Count <= 1)
                {
                    // nothing else can be deleted, go back to default state
                    StateController.Instance.ChangeState(new IdleState());
                }

                SlopeChange slope = TerrainManager.Instance.ActiveSlope;
                if (slope != null && !slope.IsBuiltOn)
                {
                    StudioUIManager.Instance.GetSidebar().DeleteButtonEnabled = false;
                    StateController.Instance.ChangeState(new IdleState());
                }
            }            
        }        

        private void ResetSelectedObstacle()
        {
            if (selectedObstacle == null) return;
            
            selectedObstacle.GetTransform().GetComponent<MeshRenderer>().material = deleteUI.DirtMaterial;
            selectedObstacle = null;
        }

        private bool CanDelete(Takeoff takeoff)
        {
            SlopeChange slope = TerrainManager.Instance.ActiveSlope;
            bool noSlopeInterferes = slope == null || slope.IsBuiltOn;
            return takeoff.GetIndex() >= Line.Instance.Count - 2 && noSlopeInterferes;
        }

        private bool CanDelete(Landing landing)
        {
            SlopeChange slope = TerrainManager.Instance.ActiveSlope;
            bool noSlopeInterferes = slope == null || slope.IsBuiltOn;
            return landing.GetIndex() == Line.Instance.Count - 1 && noSlopeInterferes;
        }

        private void SelectObstacle(ILineElement obstacle)
        {
            selectedObstacle = obstacle;
            obstacle.GetTransform().GetComponent<MeshRenderer>().material = deleteUI.CanDeleteMaterial;
            deleteUI.ToggleDeleteButton(true);
        }

        private void OnObstacleClick(ILineElement obstacle)
        {
            if (StudioUIManager.IsPointerOverUI) return;

            ResetSelectedObstacle();

            switch (obstacle)
            {
                case Takeoff takeoff when CanDelete(takeoff):
                    SelectObstacle(takeoff);
                    break;
                case Landing landing when CanDelete(landing):
                    SelectObstacle(landing);
                    break;
            }
        }
        
        private void OnObstacleMouseOver(ILineElement obstacle)
        {
            if (obstacle is Takeoff takeoff)
            {
                takeoff.GetComponent<MeshRenderer>().material = CanDelete(takeoff) ? deleteUI.CanDeleteMaterial : deleteUI.CantDeleteMaterial;
            }            
            else if (obstacle is Landing landing)
            {
                landing.GetComponent<MeshRenderer>().material = CanDelete(landing) ? deleteUI.CanDeleteMaterial : deleteUI.CantDeleteMaterial;
            }
        }          

        private void OnObstacleMouseExit(ILineElement obstacle)
        {
            switch (obstacle)
            {
                case Takeoff takeoff when obstacle != selectedObstacle:
                    takeoff.GetComponent<MeshRenderer>().material = deleteUI.DirtMaterial;
                    break;
                case Landing landing when obstacle != selectedObstacle:
                    landing.GetComponent<MeshRenderer>().material = deleteUI.DirtMaterial;
                    break;
            }
        }
    }
}
