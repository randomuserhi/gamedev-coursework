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
            WallSlide,
            Dash
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

            controller.maxSlopeCosAngle = maxSlopeCosAngle;

            input = inputSystem.movement.ReadValue<Vector2>();
            jump = inputSystem.jump.ReadValue<float>();
            if (jump == 0) {
                jumpReleased = true;
            }
            dash = inputSystem.dash.ReadValue<float>();
            if (dash == 0) {
                dashReleased = true;
            }
            if (decelerationTimer > 0) {
                if (!controller.Grounded) {
                    // drag
                    Vector2 speed = new Vector2(Mathf.Abs(rb.velocity.x), Mathf.Abs(rb.velocity.y));
                    Vector2 drop = speed * new Vector2(20f, speed.y > maxAirSpeed ? 20f : 0f) * Time.fixedDeltaTime;
                    rb.velocity *= new Vector2(
                        rb.velocity.x != 0f ? Mathf.Max(speed.x - drop.x, 0) / speed.x : 1f,
                        rb.velocity.y != 0f ? Mathf.Max(speed.y - drop.y, 0) / speed.y : 1f
                    );
                }
                decelerationTimer -= dt;
            }

            // Check dash cooldown reset when you land
            RaycastHit2D groundHit = controller.groundHit();
            if (groundHit.collider != null) {
                if (groundHit.distance <= controller.hoverHeight + 0.05f) {
                    if (Vector3.Dot(rb.velocity, groundHit.normal) <= 0) {
                        dashGrounded = true;
                    }
                } else if (dashGrounded) {
                    dashGrounded = false;
                }
            }
            if (dashGrounded && dashGrounded != prevDashGrounded) {
                canDash = 0;
            }
            prevDashGrounded = dashGrounded;

            // Check super dash cooldown
            if (state != LocomotionState.Dash && superDashTimer > 0) {
                superDashTimer -= dt;
            }

            // Can enter dash state from any state
            if (state != LocomotionState.Dash && dash != 0 && canDash <= 0 && dashReleased) {
                EnterState(LocomotionState.Dash);
                canDash = dashCooldown;
                dashReleased = false;
            } else if (canDash > 0) {
                canDash -= dt;
                if (!dashGrounded) {
                    if (canDash < 0.001) {
                        canDash = 0.001f;
                    }
                }
            }

            switch (state) {
                case LocomotionState.Grounded:
                case LocomotionState.Airborne:
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
                case LocomotionState.Dash: Update_Dash(); break;
            }
        }

        [Header("Settings")]
        [SerializeField] private float acceleration = 5f;

        [SerializeField] private float crouchHeight = 0.7f;

        // TODO(randomuserhi): Tie friction to a surface
        [SerializeField] private float friction = 1f;
        [SerializeField] private Vector2 drag = new Vector2(1f, 1f);
        [NonSerialized] public bool facingRight = true;

        [SerializeField] private float gravity = 15f;
        [SerializeField] private float fallGravity = 20f;
        [SerializeField] private float fallThreshold = 1f;
        [SerializeField] private float jumpVel = 7.5f;
        [SerializeField] private float cayoteTime = 0.2f;
        [SerializeField] private float maxJumpCosAngle = 0.17364f;
        [SerializeField] private float maxSlopeCosAngle = 0.5f;

        [SerializeField] private float maxDashSlopeCosAngle = -0.5f;
        [SerializeField] private float dashCooldown = 0.7f;
        [SerializeField] private float superDashCooldown = 0.3f;
        [SerializeField] private float dashDuration = 0.17f;
        [SerializeField] private float dashSpeed = 5f;
        [SerializeField] private float maxAirSpeed = 13f;

        [Header("State")]
        [SerializeField] private bool fromJump = false;
        [SerializeField] private float canJump = 0;
        [SerializeField] private bool jumpReleased = true;
        [SerializeField] private Vector2 wallNormal = Vector2.zero;

        [SerializeField] private float superDashTimer = 0f;
        [SerializeField] private Vector2 dashDir = Vector2.zero;
        [SerializeField] private float canDash = 0;
        [SerializeField] private bool dashGrounded = false;
        [SerializeField] private bool prevDashGrounded = false;
        [SerializeField] private bool canSuperDash = false;
        [SerializeField] private bool dashReleased = true;
        [SerializeField] private float dashTimer = 0;

        [SerializeField] private float decelerationTimer = 0;

        [Header("Inputs")]
        [SerializeField] private Vector2 input;
        [SerializeField] private float jump;
        [SerializeField] private float dash;
        [SerializeField] private float attack;

        private void ExitState() {
            switch (state) {
                case LocomotionState.Airborne:
                    controller.gravity = gravity;
                    break;
                case LocomotionState.WallSlide: Exit_WallSlide(); break;
                case LocomotionState.Dash: Exit_Dash(); break;
            }
        }

        private void EnterState(LocomotionState state) {
            if (this.state == state) return;

            ExitState();
            this.state = state;

            switch (state) {
                case LocomotionState.Grounded: Enter_Grounded(); break;
                case LocomotionState.Dash: Enter_Dash(); break;
            }
        }

        #region Dash State

        private void Enter_Dash() {
            controller.size.y = 1.3f; // TODO(randomuserhi): Make a setting variable

            canDash = dashCooldown;
            dashTimer = dashDuration;
            dashDir = input.normalized;
            if (dashDir == Vector2.zero) {
                if (facingRight) {
                    dashDir = Vector2.right;
                } else {
                    dashDir = Vector2.left;
                }
            }

            if (rb.velocity.x < maxAirSpeed) {
                decelerationTimer = -1;
            } else {
                decelerationTimer = 0;
            }

            // Verify velocity doesnt clip
            canSuperDash = superDashTimer <= 0 && ((controller.Grounded && dashDir.y < 0) || !controller.Grounded);
            if (controller.Grounded && dashDir.y < 0) {
                if (dashDir.x > 0) {
                    dashDir = Vector2.right;
                } else {
                    dashDir = Vector2.left;
                }
            }

            controller.active = false;
        }

        private void Update_Dash() {
            controller.gravity = 0;

            if (dashTimer >= 0) {
                RaycastHit2D hit = Physics2D.BoxCast(controller.center, controller.size, 0, rb.velocity.normalized, rb.velocity.magnitude * dt, controller.surfaceLayerMask);
                if (hit.collider != null) {
                    if (Vector2.Dot(rb.velocity.normalized, hit.normal) >= maxDashSlopeCosAngle) {
                        dashDir = Vector3.Project(dashDir, Vector2.Perpendicular(hit.normal)).normalized;
                        rb.position = hit.centroid - new Vector2(0, controller.size.y / 2f) + hit.normal * 0.05f;
                        canJump = cayoteTime;
                    }
                }
                if (dashDir == Vector2.up) {
                    dashDir *= 0.7f;
                }
                rb.velocity = Mathf.Max(rb.velocity.magnitude, dashSpeed) * dashDir;
                dashTimer -= dt;

                if (canJump > 0) {
                    canJump -= dt;
                }

                if (jump != 0 && canJump > 0 && jumpReleased) {
                    if (canSuperDash) {
                        superDashTimer = superDashCooldown;
                        decelerationTimer = 0;
                        rb.velocity *= new Vector3(0.8f + 0.5f * (1f - dashTimer / dashDuration), 1f);
                    } else {
                        canDash = dashCooldown;
                    }

                    Vector3 newVelocity = rb.velocity * Vector2.right + jumpVel * Vector2.up;
                    if (Vector3.Dot(newVelocity.normalized, controller.SurfaceNormal) >= maxJumpCosAngle) {
                        rb.velocity *= new Vector2(1, 0);
                        rb.velocity += jumpVel * Vector2.up;
                        controller.Airborne = true;
                        fromJump = true;
                        canJump = 0;
                        rb.position = controller.bottom;
                        jumpReleased = false;
                    }

                    EnterState(LocomotionState.Airborne);
                    return;
                }
            } else {
                EnterState(LocomotionState.Airborne);
            }
        }

        private void Exit_Dash() {
            controller.size.y = 1.5f;
            controller.gravity = fallGravity;
            controller.active = true;
            if (decelerationTimer < 0) {
                decelerationTimer = 0.1f;
            }
        }

        #endregion

        #region Slide State

        private void Update_Slide() {
            if (rb.velocity.x < 0) {
                facingRight = false;
            } else if (rb.velocity.x > 0) {
                facingRight = true;
            }

            // horizontal movement
            if (Vector3.Dot(new Vector2(input.x, 0), controller.SurfaceNormal) >= 0) {
                EnterState(LocomotionState.Airborne);
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
            if (jump != 0 && canJump > 0 && jumpReleased) {
                decelerationTimer = 0f;

                Vector3 newVelocity = rb.velocity * Vector2.right + jumpVel * Vector2.up;
                if (Vector3.Dot(newVelocity.normalized, controller.SurfaceNormal) >= maxJumpCosAngle) {
                    rb.velocity *= new Vector2(1, 0);
                    rb.velocity += jumpVel * Vector2.up;
                    controller.Airborne = true;
                    fromJump = true;
                    canJump = 0;
                    rb.position = controller.bottom;
                    jumpReleased = false;
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
            if (Mathf.Sign(input.x) != Mathf.Sign(rb.velocity.x) || Mathf.Abs(rb.velocity.x) < maxAirSpeed) {
                rb.velocity += Vector2.right * input.x * a;
            }

            // drag
            Vector2 speed = new Vector2(Mathf.Abs(rb.velocity.x), Mathf.Abs(rb.velocity.y));
            float s = speed.x - maxAirSpeed;
            Vector2 d = new Vector2(
                speed.x > maxAirSpeed ? Mathf.Clamp(drag.x / (Mathf.Pow(s, 1.5f)), 0.5f, drag.x) : drag.x,
                drag.y
            );
            Vector2 drop = speed * d * Time.fixedDeltaTime;
            rb.velocity *= new Vector2(
                rb.velocity.x != 0f ? Mathf.Max(speed.x - drop.x, 0) / speed.x : 1f,
                rb.velocity.y != 0f ? Mathf.Max(speed.y - drop.y, 0) / speed.y : 1f
            );

            // extend jump
            if (rb.velocity.y > fallThreshold && jump != 0 && fromJump) {
                controller.gravity = gravity;
            } else {
                controller.gravity = fallGravity;
                if (fromJump) {
                    rb.velocity *= new Vector2(1f, 0.5f);
                    fromJump = false;
                }
            }

            // cayote jump
            if (jump != 0 && canJump > 0 && !fromJump && jumpReleased && (
                wallNormal == Vector2.zero ||
                Vector2.Dot(new Vector2(input.x, 0).normalized, wallNormal) == 1
                )
            ) {
                decelerationTimer = 0f;

                Vector3 newVelocity = rb.velocity * Vector2.right + jumpVel * Vector2.up;
                if (Vector3.Dot(newVelocity.normalized, controller.SurfaceNormal) >= maxJumpCosAngle) {
                    rb.velocity *= new Vector2(1, 0);
                    rb.velocity += jumpVel * Vector2.up;
                    controller.Airborne = true;
                    fromJump = true;
                    canJump = 0;
                    rb.position = controller.bottom;
                    jumpReleased = false;
                }
            }
        }

        #endregion
    }
}
