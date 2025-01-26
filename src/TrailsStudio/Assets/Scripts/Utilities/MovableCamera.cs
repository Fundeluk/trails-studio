using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Assets.Scripts.Utilities
{
    [RequireComponent(typeof(CharacterController))]
    public class MovableCamera : MonoBehaviour
    {        
        public float speed = 2f;

        public CharacterController controller;

        InputAction moveAction;
        InputAction hoverAction;

        private void Start()
        {
            moveAction = InputSystem.actions.FindAction("Move");
            hoverAction = InputSystem.actions.FindAction("Float");
        }

        private void FixedUpdate()
        {
            Vector2 moveValue = moveAction.ReadValue<Vector2>();
            float hoverValue = hoverAction.ReadValue<float>();

            Vector3 move = new(moveValue.x, hoverValue, moveValue.y);

            controller.Move(speed * Time.deltaTime * move);

            if (move != Vector3.zero)
            {
                transform.forward = move;
            }

        }        
    }
}