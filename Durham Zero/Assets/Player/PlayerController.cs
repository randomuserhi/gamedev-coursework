using System;
using UnityEngine;

namespace Player {
    [RequireComponent(typeof(CharacterController2D))]
    [RequireComponent(typeof(PlayerInputSystem))]
    public class PlayerController : MonoBehaviour {
        public enum LocomotionState {
            Grounded,
            Airborne,
            Crouch
        }

        private PlayerInputSystem inputSystem;
        private CharacterController2D controller;
        private Rigidbody2D rb;

        [SerializeField] private LocomotionState state = LocomotionState.Grounded;

        private void Start() {
            controller = GetComponent<CharacterController2D>();
            inputSystem = GetComponent<PlayerInputSystem>();
            rb = GetComponent<Rigidbody2D>();

            controller.gravity = gravity;
        }

        private float dt;
        private void FixedUpdate() {
            dt = Time.fixedDeltaTime;

            switch (state) {
                case LocomotionState.Grounded:
                case LocomotionState.Airborne:
                case LocomotionState.Crouch:
                    if (controller.Grounded && controller.GroundedTransition) {
                        EnterState(LocomotionState.Grounded);
                    } else if (!controller.Grounded && controller.GroundedTransition) {
                        EnterState(LocomotionState.Airborne);
                    }

                    Vector2 input = inputSystem.movement.ReadValue<Vector2>();
                    if (input.x < 0f) {
                        facingRight = false;
                    } else if (input.x > 0f) {
                        facingRight = true;
                    }
                    break;
            }

            switch (state) {
                case LocomotionState.Grounded:
                    Update_Grounded();
                    break;
                case LocomotionState.Airborne:
                    Update_Airborne();
                    break;
            }
        }

        [Header("Settings")]
        [SerializeField] private float acceleration = 5f;

        // TODO(randomuserhi): Tie friction to a surface
        [SerializeField] private float friction = 1f;
        [SerializeField] private Vector2 drag = new Vector2(1f, 1f);
        [NonSerialized] public bool facingRight = true;

        [SerializeField] private float gravity = 15f;
        [SerializeField] private float fallGravity = 20f;
        [SerializeField] private float jumpVel = 7.5f;

        [Header("State")]
        [SerializeField] private bool fromJump = false;
        [SerializeField] private bool canJump = false;

        private void ExitState() {
            switch (state) {
                case LocomotionState.Airborne:
                    controller.gravity = gravity;
                    break;
            }
        }

        private void EnterState(LocomotionState state) {
            ExitState();
            this.state = state;

            switch (state) {
                case LocomotionState.Grounded:
                    Enter_Grounded();
                    break;
            }
        }

        #region Grounded State

        private void Enter_Grounded() {
            canJump = true;
        }

        private void Update_Grounded() {
            Vector2 input = inputSystem.movement.ReadValue<Vector2>();
            Vector2 perp = -Vector2.Perpendicular(controller.SurfaceNormal).normalized;

            // horizontal movement
            float a = acceleration;
            if (input.x != 0 && Mathf.Sign(input.x) != Mathf.Sign(rb.velocity.x)) {
                a *= 2;
            }
            rb.velocity += input.x * a * perp;

            // friction
            float speed = Vector3.Project(rb.velocity, perp).magnitude;
            if (speed != 0f) {
                float drop = speed * friction * Time.fixedDeltaTime;
                rb.velocity *= Mathf.Max(speed - drop, 0f) / speed; // Scale the velocity based on friction.
            }

            // Jumping
            float jump = inputSystem.jump.ReadValue<float>();
            if (jump != 0 && canJump) {
                rb.velocity *= new Vector2(1, 0);
                rb.velocity += jumpVel * Vector2.up;
                controller.Airborne = true;
                fromJump = true;
                canJump = false;
                rb.position = controller.center;
            }
        }

        #endregion

        #region Airborne State

        private void Update_Airborne() {
            Vector2 input = inputSystem.movement.ReadValue<Vector2>();
            Vector2 perp = -Vector2.Perpendicular(controller.SurfaceNormal).normalized;

            // horizontal movement
            float a = acceleration;
            if (input.x != 0 && Mathf.Sign(input.x) != Mathf.Sign(rb.velocity.x)) {
                a *= 2;
            }
            rb.velocity += input.x * a * perp;

            // drag
            Vector2 speed = new Vector2(Mathf.Abs(rb.velocity.x), Mathf.Abs(rb.velocity.y));
            Vector2 drop = speed * drag * Time.fixedDeltaTime;
            rb.velocity *= new Vector2(
                rb.velocity.x != 0f ? Mathf.Max(speed.x - drop.x, 0) / speed.x : 1f,
                rb.velocity.y != 0f ? Mathf.Max(speed.y - drop.y, 0) / speed.y : 1f
            );

            float jump = inputSystem.jump.ReadValue<float>();
            if (rb.velocity.y > 0f && jump != 0 && fromJump) {
                controller.gravity = gravity;
            } else {
                controller.gravity = fallGravity;
                if (fromJump) {
                    rb.velocity *= new Vector2(1f, 0.5f);
                    fromJump = false;
                }
            }
        }

        #endregion
    }
}
