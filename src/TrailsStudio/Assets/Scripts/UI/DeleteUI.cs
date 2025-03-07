
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.States;
using Assets.Scripts.Builders;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class DeleteUI : MonoBehaviour
    {
        private VisualElement root;

        private Button cancelButton;
        private Button deleteButton;

        public Material dirtMaterial;

        public Material canDeleteMaterial;
        public Material cantDeleteMaterial;

        private bool canDeleteMouseOverObstacle = false;
        private ILineElement mouseOverObstacle = null;
        private ILineElement selectedObstacle = null;

        private void Initialize()
        {
            root = GetComponent<UIDocument>().rootVisualElement;

            cancelButton = root.Q<Button>("CancelButton");
            cancelButton.RegisterCallback<ClickEvent>(CancelClicked);
            
            // Prevent triggering general onclick behavior when clicking a button
            cancelButton.RegisterCallback<MouseEnterEvent>((evt) => InputSystem.actions.FindAction("Click").performed -= OnClick);
            cancelButton.RegisterCallback<MouseLeaveEvent>((evt) => InputSystem.actions.FindAction("Click").performed += OnClick);


            deleteButton = root.Q<Button>("DeleteButton");
            deleteButton.RegisterCallback<ClickEvent>(DeleteClicked);
            deleteButton.RegisterCallback<MouseEnterEvent>((evt) => InputSystem.actions.FindAction("Click").performed -= OnClick);
            deleteButton.RegisterCallback<MouseLeaveEvent>((evt) => InputSystem.actions.FindAction("Click").performed += OnClick);

            deleteButton.SetEnabled(false);

            InputSystem.actions.FindAction("Select").performed += OnClick;
        }

        // Use this for initialization
        void Start()
        {
            Initialize();   
        }

        private void OnEnable()
        {
            Initialize();

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
                bool isLanding = selectedObstacle is LandingMeshGenerator.Landing;
                
                // change camera target to the new last obstacle
                CameraManager.Instance.DetailedView(Line.Instance.line[index - 1]);

                Line.Instance.DestroyLineElementAt(index);

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


        private void OnClick(InputAction.CallbackContext context)
        {
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
            if (mouseOverObstacle != takeoffMesh.takeoff && mouseOverObstacle != null && mouseOverObstacle != selectedObstacle)
            {
                mouseOverObstacle.GetTransform().GetComponent<MeshRenderer>().material = dirtMaterial;
            }
            mouseOverObstacle = takeoffMesh.takeoff;
            if (takeoffMesh.takeoff.GetIndex() >= Line.Instance.line.Count - 2)
            {
                mouseOverObstacle = takeoffMesh.takeoff;
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
            if (mouseOverObstacle != landingMesh.landing && mouseOverObstacle != null && mouseOverObstacle != selectedObstacle)
            {
                mouseOverObstacle.GetTransform().GetComponent<MeshRenderer>().material = dirtMaterial;
            }
            mouseOverObstacle = landingMesh.landing;
            if (landingMesh.landing.GetIndex() == Line.Instance.line.Count - 1)
            {
                mouseOverObstacle.GetTransform().GetComponent<MeshRenderer>().material = canDeleteMaterial;
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