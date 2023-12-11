using System;
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
                    Update_Default();
                    break;
            }
        }

        #region Default State

        [SerializeField] private float acceleration = 5f;

        // TODO(randomuserhi): Tie friction to a surface
        [SerializeField] private float friction = 1f;
        [SerializeField] private Vector2 drag = new Vector2(1f, 1f);
        [NonSerialized] public bool facingRight = true;

        private void Enter_Default() {
            state = LocomotionState.Default;
        }

        private void Update_Default() {
            Vector2 input = inputSystem.movement.ReadValue<Vector2>();
            Vector2 perp = -Vector2.Perpendicular(controller.SurfaceNormal).normalized;

            if (input.x < 0f) {
                facingRight = false;
            } else if (input.x > 0f) {
                facingRight = true;
            }

            // horizontal movement
            float a = acceleration;
            if (input.x != 0 && Mathf.Sign(input.x) != Mathf.Sign(rb.velocity.x)) {
                a *= 2;
            }
            rb.velocity += input.x * a * perp;

            if (controller.Grounded) {
                // friction
                float speed = Vector3.Project(rb.velocity, perp).magnitude;
                if (speed != 0f) {
                    float drop = speed * friction * Time.fixedDeltaTime;
                    rb.velocity *= Mathf.Max(speed - drop, 0f) / speed; // Scale the velocity based on friction.
                }
            } else {
                // drag
                Vector2 speed = new Vector2(Mathf.Abs(rb.velocity.x), Mathf.Abs(rb.velocity.y));
                Vector2 drop = speed * drag * Time.fixedDeltaTime;
                rb.velocity *= new Vector2(
                    rb.velocity.x != 0f ? Mathf.Max(speed.x - drop.x, 0) / speed.x : 1f,
                    rb.velocity.y != 0f ? Mathf.Max(speed.y - drop.y, 0) / speed.y : 1f
                );
            }
        }

        #endregion
    }
}
