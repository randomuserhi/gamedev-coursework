using UnityEngine;

namespace Player {
    [RequireComponent(typeof(CharacterController2D))]
    [RequireComponent(typeof(PlayerInputSystem))]
    public class PlayerController : MonoBehaviour {
        public enum LocomotionState {
            Default,
            Crouch
        }

        private PlayerInputSystem inputSystem;
        private CharacterController2D controller;
        private Rigidbody2D rb;

        private LocomotionState state = LocomotionState.Default;

        private void Start() {
            controller = GetComponent<CharacterController2D>();
            inputSystem = GetComponent<PlayerInputSystem>();
            rb = GetComponent<Rigidbody2D>();
        }

        private float dt;
        private void FixedUpdate() {
            dt = Time.fixedDeltaTime;

            switch (state) {
                case LocomotionState.Default:
                    UpdateState_Default();
                    break;
            }
        }

        #region Default State

        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float acceleration;
        [SerializeField] private float maxAcceleration;

        private void UpdateState_Default() {
            Vector2 input = inputSystem.movement.ReadValue<Vector2>();

            float s = 10;
            if (Mathf.Sign(input.x) != Mathf.Sign(rb.velocity.x)) {
                s *= 1 + Mathf.Clamp(Mathf.Abs(rb.velocity.x) / 16, 0, 2);
            }
            rb.velocity -= input.x * s * Vector2.Perpendicular(controller.SurfaceNormal);
            rb.velocity *= new Vector2(0.7f, 1);
        }

        #endregion
    }
}
