
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

        public Material dirtMaterial;

        public Material canDeleteMaterial;
        public Material cantDeleteMaterial;

        private bool canDeleteMouseOverObstacle = false;
        private ILineElement mouseOverObstacle = null;
        private ILineElement selectedObstacle = null;

        private bool isMouseOverUI = false;

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

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;

            cancelButton = root.Q<Button>("CancelButton");
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);

            // Prevent triggering general onclick behavior when clicking a button
            //cancelButton.RegisterCallback<MouseEnterEvent>((evt) => InputSystem.actions.FindAction("Click").performed -= OnClick);
            //cancelButton.RegisterCallback<MouseLeaveEvent>((evt) => InputSystem.actions.FindAction("Click").performed += OnClick);


            deleteButton = root.Q<Button>("DeleteButton");
            deleteButton.RegisterCallback<ClickEvent>(DeleteClicked);
            //deleteButton.RegisterCallback<MouseEnterEvent>((evt) => InputSystem.actions.FindAction("Click").performed -= OnClick);
            //deleteButton.RegisterCallback<MouseLeaveEvent>((evt) => InputSystem.actions.FindAction("Click").performed += OnClick);

            deleteSlopeButton = root.Q<Button>("DeleteSlopeButton");
            deleteSlopeButton.RegisterCallback<ClickEvent>(DeleteSlopeClicked);

            deleteButton.SetEnabled(false);

            InputSystem.actions.FindAction("Select").performed += OnClick;

            DeleteSlopeButtonEnabled = _deleteSlopeButtonEnabled;

            mouseOverObstacle = null;
            selectedObstacle = null;            
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction("Select").performed -= OnClick;

            if (mouseOverObstacle != null)
            {
                mouseOverObstacle.GetTransform().GetComponent<MeshRenderer>().material = dirtMaterial;
                mouseOverObstacle = null;
            }

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

                mouseOverObstacle = null;
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


        private void OnClick(InputAction.CallbackContext context)
        {
            if (isMouseOverUI)
            {
                return;
            }
            
            if (mouseOverObstacle != null)
            {
                Debug.Log("mouseover obstacle index: " + mouseOverObstacle.GetIndex());
            }

            if (selectedObstacle != null)
            {
                selectedObstacle.GetTransform().GetComponent<MeshRenderer>().material = dirtMaterial;
            }

            if (canDeleteMouseOverObstacle)
            {
                selectedObstacle = mouseOverObstacle;
                deleteButton.SetEnabled(true);
            }
            else
            {
                selectedObstacle = null;
                deleteButton.SetEnabled(false);
            }
        }

        private void OnTakeoffMouseover(TakeoffMeshGenerator takeoffMesh)
        {
            Takeoff takeoff = takeoffMesh.gameObject.GetComponent<Takeoff>();

            if (mouseOverObstacle != (ILineElement)takeoff && mouseOverObstacle != null && mouseOverObstacle != selectedObstacle)
            {
                mouseOverObstacle.GetTransform().GetComponent<MeshRenderer>().material = dirtMaterial;
            }

            mouseOverObstacle = takeoff;

            if (takeoff.GetIndex() >= Line.Instance.line.Count - 2)
            {
                takeoffMesh.GetComponent<MeshRenderer>().material = canDeleteMaterial;
                canDeleteMouseOverObstacle = true;
            }
            else
            {
                takeoffMesh.GetComponent<MeshRenderer>().material = cantDeleteMaterial;
                canDeleteMouseOverObstacle = false;
            }
        }

        private void OnLandingMouseover(LandingMeshGenerator landingMesh)
        {
            Landing landing = landingMesh.gameObject.GetComponent<Landing>();

            if (mouseOverObstacle != (ILineElement)landing && mouseOverObstacle != null && mouseOverObstacle != selectedObstacle)
            {
                mouseOverObstacle.GetTransform().GetComponent<MeshRenderer>().material = dirtMaterial;
            }

            mouseOverObstacle = landing;

            if (landing.GetIndex() == Line.Instance.line.Count - 1)
            {
                landingMesh.GetComponent<MeshRenderer>().material = canDeleteMaterial;
                canDeleteMouseOverObstacle = true;
            }
            else
            {
                landingMesh.GetComponent<MeshRenderer>().material = cantDeleteMaterial;
                canDeleteMouseOverObstacle = false;
            }
        }

        private void OnOtherMouseover()
        {
            if (mouseOverObstacle != null && mouseOverObstacle != selectedObstacle)
            {
                mouseOverObstacle.GetTransform().GetComponent<MeshRenderer>().material = dirtMaterial;
            }
            mouseOverObstacle = null;
            canDeleteMouseOverObstacle = false;
        }

        private void FixedUpdate()
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                isMouseOverUI = true;
                return;
            }
            else
            {
                isMouseOverUI = false;
            }

                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.TryGetComponent<TakeoffMeshGenerator>(out var takeoffMesh))
                {
                    OnTakeoffMouseover(takeoffMesh);                    
                }
                else if (hit.collider.gameObject.TryGetComponent<LandingMeshGenerator>(out var landingMesh))
                {
                    OnLandingMouseover(landingMesh);
                }
                else
                {
                    OnOtherMouseover();
                }
            }
        }
    }
}