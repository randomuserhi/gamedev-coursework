using System;
using UnityEngine;

namespace Player {
    [RequireComponent(typeof(CharacterController2D))]
    [RequireComponent(typeof(PlayerInputSystem))]
    public class PlayerController : MonoBehaviour {
        public enum LocomotionState {
            Grounded,
            Airborne,
            Slide,
            Crouch,
            WallSlide
        }

        private PlayerInputSystem inputSystem;
        private CharacterController2D controller;
        private Rigidbody2D rb;

        [SerializeField] public LocomotionState state = LocomotionState.Airborne;

        private void Start() {
            controller = GetComponent<CharacterController2D>();
            inputSystem = GetComponent<PlayerInputSystem>();
            rb = GetComponent<Rigidbody2D>();

            controller.gravity = gravity;
        }

        private ContactPoint2D[] contacts = new ContactPoint2D[16];

        private float dt;
        private void FixedUpdate() {
            dt = Time.fixedDeltaTime;

            input = inputSystem.movement.ReadValue<Vector2>();
            jump = inputSystem.jump.ReadValue<float>();

            switch (state) {
                case LocomotionState.Grounded:
                case LocomotionState.Airborne:
                case LocomotionState.Crouch:
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
                case LocomotionState.Airborne:
                case LocomotionState.Crouch:
                case LocomotionState.Slide:
                    if (controller.Grounded) {
                        EnterState(LocomotionState.Grounded);
                    } else if (!controller.Grounded) {
                        if (controller.slip) {
                            EnterState(LocomotionState.Slide);
                        } else {
                            EnterState(LocomotionState.Airborne);
                        }
                    }
                    break;
            }

            switch (state) {
                case LocomotionState.Grounded: Update_Grounded(); break;
                case LocomotionState.Airborne: Update_Airborne(); break;
                case LocomotionState.Slide: Update_Slide(); break;
                case LocomotionState.WallSlide: Update_WallSlide(); break;
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
        [SerializeField] private float cayoteTime = 0.2f;
        [SerializeField] private float maxJumpCosAngle = 0.17364f;
        [SerializeField] private float maxSlopeCosAngle = 0.5f;

        [Header("State")]
        [SerializeField] private bool fromJump = false;
        [SerializeField] private float canJump = 0;
        [SerializeField] private Vector2 wallNormal = Vector2.zero;

        [Header("Inputs")]
        [SerializeField] private Vector2 input;
        [SerializeField] private float jump;

        private void ExitState() {
            switch (state) {
                case LocomotionState.Airborne:
                    controller.gravity = gravity;
                    break;
                case LocomotionState.WallSlide: Exit_WallSlide(); break;
            }
        }

        private void EnterState(LocomotionState state) {
            if (this.state == state) return;

            ExitState();
            this.state = state;

            switch (state) {
                case LocomotionState.Grounded: Enter_Grounded(); break;
            }
        }

        #region Slide State

        private void Update_Slide() {
            if (rb.velocity.x < 0) {
                facingRight = false;
            } else if (rb.velocity.x > 0) {
                facingRight = true;
            }

            // horizontal movement
            if (Vector3.Dot(new Vector2(input.x, 0), controller.SurfaceNormal) >= 0) {
                rb.velocity += input.x * acceleration * Vector2.right;
            }
        }

        #endregion

        #region Grounded State

        private void Enter_Grounded() {
            canJump = cayoteTime;
            wallNormal = Vector2.zero;
        }

        private void Update_Grounded() {
            Vector2 perp = -Vector2.Perpendicular(controller.SurfaceNormal).normalized;

            // Check slope
            if (Vector3.Dot(Vector2.up, controller.SurfaceNormal) < maxSlopeCosAngle) {
                EnterState(LocomotionState.Slide);
            }

            // horizontal movement
            float a = acceleration;
            if (input.x != 0 && Mathf.Sign(input.x) != Mathf.Sign(rb.velocity.x)) {
                a *= 2;
            }
            rb.velocity += input.x * a * perp;

            // friction
            float speed = Vector3.Project(rb.velocity, perp).magnitude;
            if (speed != 0f) {
                float drop = speed * friction * dt;
                rb.velocity *= Mathf.Max(speed - drop, 0f) / speed; // Scale the velocity based on friction.
            }

            // Jumping
            if (jump != 0 && canJump > 0) {
                Vector3 newVelocity = rb.velocity * Vector2.right + jumpVel * Vector2.up;
                if (Vector3.Dot(newVelocity.normalized, controller.SurfaceNormal) >= maxJumpCosAngle) {
                    rb.velocity *= new Vector2(1, 0);
                    rb.velocity += jumpVel * Vector2.up;
                    controller.Airborne = true;
                    fromJump = true;
                    canJump = 0;
                    rb.position = controller.bottom;
                }
            }
        }

        #endregion

        #region WallSlide State

        private void Exit_WallSlide() {
            canJump = cayoteTime;
        }

        private void Update_WallSlide() {
            // wall slide
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(controller.surfaceLayerMask);
            int numberOfContacts = rb.GetContacts(filter, contacts);
            bool inWallSlide = false;
            for (var i = 0; i < numberOfContacts; i++) {
                ContactPoint2D contact = contacts[i];

                if (Vector2.Dot(new Vector2(input.x, 0).normalized, contact.normal) == -1) {
                    inWallSlide = true;
                    wallNormal = contact.normal;
                    break;
                }
            }

            if (!inWallSlide) {
                EnterState(LocomotionState.Airborne);
                return;
            }

            // friction
            float speed = rb.velocity.y;
            if (speed != 0f) {
                float drop = speed * friction * dt;
                rb.velocity *= Mathf.Max(speed - drop, 0f) / speed;
            }

            rb.velocity += Vector2.down * gravity * 0.5f * dt;
        }

        #endregion

        #region Airborne State

        private void Update_Airborne() {
            if (canJump > 0) {
                canJump -= dt;
            }

            // wall slide
            if (input.x != 0) {
                ContactFilter2D filter = new ContactFilter2D();
                filter.SetLayerMask(controller.surfaceLayerMask);
                int numberOfContacts = rb.GetContacts(filter, contacts);
                for (var i = 0; i < numberOfContacts; i++) {
                    ContactPoint2D contact = contacts[i];

                    if (Vector2.Dot(new Vector2(input.x, 0).normalized, contact.normal) == -1) {
                        EnterState(LocomotionState.WallSlide);
                    }
                }
            }

            // horizontal movement
            float a = acceleration;
            if (input.x != 0 && Mathf.Sign(input.x) != Mathf.Sign(rb.velocity.x)) {
                a *= 2;
            }
            rb.velocity += Vector2.right * input.x * a;

            // drag
            Vector2 speed = new Vector2(Mathf.Abs(rb.velocity.x), Mathf.Abs(rb.velocity.y));
            Vector2 drop = speed * drag * Time.fixedDeltaTime;
            rb.velocity *= new Vector2(
                rb.velocity.x != 0f ? Mathf.Max(speed.x - drop.x, 0) / speed.x : 1f,
                rb.velocity.y != 0f ? Mathf.Max(speed.y - drop.y, 0) / speed.y : 1f
            );

            // extend jump
            if (rb.velocity.y > 0f && jump != 0 && fromJump) {
                controller.gravity = gravity;
            } else {
                controller.gravity = fallGravity;
                if (fromJump) {
                    rb.velocity *= new Vector2(1f, 0.5f);
                    fromJump = false;
                }
            }

            // cayote jump
            if (jump != 0 && canJump > 0 && !fromJump && (
                wallNormal == Vector2.zero ||
                Vector2.Dot(new Vector2(input.x, 0).normalized, wallNormal) == 1
                )
            ) {
                Vector3 newVelocity = rb.velocity * Vector2.right + jumpVel * Vector2.up;
                if (Vector3.Dot(newVelocity.normalized, controller.SurfaceNormal) >= maxJumpCosAngle) {
                    rb.velocity *= new Vector2(1, 0);
                    rb.velocity += jumpVel * Vector2.up;
                    controller.Airborne = true;
                    fromJump = true;
                    canJump = 0;
                    rb.position = controller.bottom;
                }
            }
        }

        #endregion
    }
}
