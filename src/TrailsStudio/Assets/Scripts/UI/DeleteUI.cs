
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.States;
using Assets.Scripts.Builders;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Assets.Scripts.Managers;

namespace Assets.Scripts.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class DeleteUI : MonoBehaviour
    {
        private VisualElement root;

        private Button cancelButton;
        private Button deleteButton;
        private Button deleteSlopeButton;

        [SerializeField]
        Material dirtMaterial;

        [SerializeField]
        Material canDeleteMaterial;

        [SerializeField]
        Material cantDeleteMaterial;        

        private bool _deleteSlopeButtonEnabled = false;
        public bool DeleteSlopeButtonEnabled
        {
            get => _deleteSlopeButtonEnabled;
            set
            {
                _deleteSlopeButtonEnabled = value;

                if (isActiveAndEnabled)
                {
                    UIManager.ToggleButton(deleteSlopeButton, value);
                }

            }
        }

        ILineElement selectedObstacle = null;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;

            cancelButton = root.Q<Button>("CancelButton");
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);  

            deleteButton = root.Q<Button>("DeleteButton");
            deleteButton.RegisterCallback<ClickEvent>(DeleteClicked);            

            deleteSlopeButton = root.Q<Button>("DeleteSlopeButton");
            deleteSlopeButton.RegisterCallback<ClickEvent>(DeleteSlopeClicked);

            deleteButton.SetEnabled(false);

            DeleteSlopeButtonEnabled = _deleteSlopeButtonEnabled;

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
            deleteSlopeButton.UnregisterCallback<ClickEvent>(DeleteSlopeClicked);
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
                CameraManager.Instance.DetailedView(Line.Instance.line[index - 1]);

                Line.Instance.DestroyLineElementsFromIndex(index);

                selectedObstacle = null;
                deleteButton.SetEnabled(false);

                if (isLanding)
                {
                    // after landing deletion, go back to landing positioning state immediately
                    StateController.Instance.ChangeState(new LandingPositioningState());
                }
                // if the first obstacle after roll-in is deleted
                else if (index == 1)
                {
                    // nothing else can be deleted, go back to default state
                    StateController.Instance.ChangeState(new DefaultState());
                }
            }            
        }

        private void DeleteSlopeClicked(ClickEvent evt)
        {            
            TerrainManager.Instance.ActiveSlope.Delete();
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
            return takeoff.GetIndex() >= Line.Instance.line.Count - 2;
        }

        static bool CanDelete(Landing landing)
        {
            return landing.GetIndex() == Line.Instance.line.Count - 1;
        }

        private void SelectObstacle(ILineElement obstacle)
        {
            selectedObstacle = obstacle;
            obstacle.GetTransform().GetComponent<MeshRenderer>().material = canDeleteMaterial;
            deleteButton.SetEnabled(true);
        }

        private void OnObstacleClick(GameObject obstacle)
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            ResetSelectedObstacle();

            if (obstacle.TryGetComponent<TakeoffMeshGenerator>(out _))
            {
                Takeoff takeoff = obstacle.GetComponent<Takeoff>();
                if (CanDelete(takeoff))
                {
                    SelectObstacle(takeoff);                    
                }

            }
            else if (obstacle.TryGetComponent<LandingMeshGenerator>(out _))
            {
                Landing landing = obstacle.GetComponent<Landing>();
                if (CanDelete(landing))
                {
                    SelectObstacle(landing);
                }
            }
        }

        
        private void OnObstacleMouseOver(GameObject obstacle)
        {
            if (obstacle.TryGetComponent<TakeoffMeshGenerator>(out _))
            {
                HandleTakeoffMouseover(obstacle);
            }
            else if (obstacle.TryGetComponent<LandingMeshGenerator>(out _))
            {
                HandleLandingMouseover(obstacle);
            }
        }

        private void HandleTakeoffMouseover(GameObject takeoffObject)
        {
            Takeoff takeoff = takeoffObject.GetComponent<Takeoff>();
            
            if (CanDelete(takeoff))
            {
                takeoffObject.GetComponent<MeshRenderer>().material = canDeleteMaterial;
            }
            else
            {
                takeoffObject.GetComponent<MeshRenderer>().material = cantDeleteMaterial;
            }
        }

        private void HandleLandingMouseover(GameObject landingObject)
        {
            Landing landing = landingObject.GetComponent<Landing>();            

            if (CanDelete(landing))
            {
                landingObject.GetComponent<MeshRenderer>().material = canDeleteMaterial;
            }
            else
            {
                landingObject.GetComponent<MeshRenderer>().material = cantDeleteMaterial;
            }
        }        

        private void OnObstacleMouseExit(GameObject obstacle)
        {
            if (obstacle.TryGetComponent<Takeoff>(out var takeoff) && (ILineElement)takeoff != selectedObstacle)
            {
                obstacle.GetComponent<MeshRenderer>().material = dirtMaterial;
            }
            else if (obstacle.TryGetComponent<Landing>(out var landing) && (ILineElement)landing != selectedObstacle)
            {
                obstacle.GetComponent<MeshRenderer>().material = dirtMaterial;
            }
        }        
    }
}