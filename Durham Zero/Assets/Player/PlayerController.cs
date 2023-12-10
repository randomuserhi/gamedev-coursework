using UnityEngine;

namespace Player {
    [RequireComponent(typeof(CharacterController2D))]
    [RequireComponent(typeof(PlayerInputSystem))]
    public class PlayerController : MonoBehaviour {
        public enum LocomotionState {
            Walk
        }

        private PlayerInputSystem inputSystem;
        private CharacterController2D controller;
        private LocomotionState state;

        private void Start() {
            controller = GetComponent<CharacterController2D>();
            inputSystem = GetComponent<PlayerInputSystem>();
        }

        private void FixedUpdate() {
            Rigidbody2D rb = controller.rb;

            Vector2 input = inputSystem.movement.ReadValue<Vector2>();

            float s = 10;
            if (Mathf.Sign(input.x) != Mathf.Sign(rb.velocity.x)) {
                s *= 1 + Mathf.Clamp(Mathf.Abs(rb.velocity.x) / 16, 0, 2);
            }
            rb.velocity -= input.x * s * Vector2.Perpendicular(controller.SurfaceNormal);
            rb.velocity *= new Vector2(0.7f, 1);
        }
    }
}
