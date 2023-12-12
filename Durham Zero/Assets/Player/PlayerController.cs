using System.Collections.Generic;
using UnityEngine;

namespace Player {
    [RequireComponent(typeof(CharacterController2D))]
    [RequireComponent(typeof(ScalfRig))]
    public class PlayerController : MonoBehaviour {
        public enum LocomotionState {
            Grounded,
            Airborne,
            Slide,
            WallSlide,
            Dash,
            Dead,
            Respawning,
        }

        private CharacterController2D controller;
        private Rigidbody2D rb;
        private ScalfRig scalf;

        [SerializeField] public LocomotionState state = LocomotionState.Airborne;

        private void Start() {
            controller = GetComponent<CharacterController2D>();
            scalf = GetComponent<ScalfRig>();
            rb = GetComponent<Rigidbody2D>();
        }

        private ContactPoint2D[] contacts = new ContactPoint2D[16];

        public Vector3 respawnPoint;

        private HashSet<GameObject> active = new HashSet<GameObject>();
        private HashSet<GameObject> used = new HashSet<GameObject>();
        private void OnTriggerEnter2D(Collider2D collision) {
            int layer = 1 << collision.gameObject.layer;
            if (layer == LayerMask.GetMask("end")) {

            } else if (layer == LayerMask.GetMask("checkpoint")) {
                if (!active.Contains(collision.gameObject)) {
                    respawnPoint = collision.transform.GetChild(0).transform.position;
                    active.Add(collision.gameObject);
                }
            } else if (layer == LayerMask.GetMask("hurtbox")) {
                Dead();
            } else if (state != LocomotionState.Grounded && layer == LayerMask.GetMask("dashcharge") && !used.Contains(collision.gameObject)) {
                canDash = 0;
                used.Add(collision.gameObject);
                EffectLibrary.SpawnEffect(9, collision.transform.position);
                collision.gameObject.SetActive(false);
            }
        }

        public void Alive() {
            state = LocomotionState.Airborne;

            foreach (GameObject go in used) {
                go.SetActive(true);
                AnimatedEffect e = EffectLibrary.SpawnEffect(9, go.transform.position);
                e.reverse = true;
            }
            used.Clear();

            transform.position = respawnPoint;
            scalf.SetPosition(respawnPoint);
            controller.gravity = gravity;
            controller.active = true;
            controller.rb.velocity = Vector2.zero;
        }

        public void Dead() {
            state = LocomotionState.Dead;
            controller.active = false;
            controller.rb.velocity = Vector2.zero;
            controller.gravity = 0;
        }

        private AnimatedEffect sweat;
        private bool initialized = false;
        private float dt;
        private void FixedUpdate() {
            if (!initialized) {
                initialized = true;
                Alive();
            }

            dt = Time.fixedDeltaTime;

            if (sweat != null) {
                float dir = facingRight ? 1 : -1;
                sweat.transform.position = controller.center + new Vector2(0, 0.7f);
                sweat.flip = !facingRight;
            }

            if (state == LocomotionState.Dead) {
                return;
            }

            // Wall jump, lock x dir
            if (lockDirX > 0) {
                lockDirX -= dt;
                if (wallNormal != Vector2.zero) {
                    input.x = Mathf.Sign(wallNormal.x);
                }
                if (state != LocomotionState.Airborne && state != LocomotionState.WallSlide) {
                    lockDirX = 0;
                }
            }

            // crouching
            if (isCrouching) {
                controller.size.y = crouchHeight;
            } else {
                controller.size.y = 1.5f;
            }

            controller.maxSlopeCosAngle = maxSlopeCosAngle;

            if (jump == 0) {
                jumpReleased = true;
            }
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
            } else {
                dashGrounded = false;
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
                    if (!isCrouching) {
                        if (input.x < 0f) {
                            facingRight = false;
                        } else if (input.x > 0f) {
                            facingRight = true;
                        }
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
        [SerializeField] private float friction = 20f;
        [SerializeField] private float crouchFriction = 1f;
        [SerializeField] private Vector2 drag = new Vector2(1f, 1f);

        [SerializeField] private float gravity = 15f;
        [SerializeField] private float fallGravity = 20f;
        [SerializeField] private float fallThreshold = 1f;
        [SerializeField] private float jumpVel = 7.5f;
        [SerializeField] private float cayoteTime = 0.2f;
        [SerializeField] private float maxJumpCosAngle = 0.17364f;
        [SerializeField] private float maxSlopeCosAngle = 0.5f;

        [SerializeField] private float wallJumpDirLock = 0.25f;

        [SerializeField] private float maxDashSlopeCosAngle = -0.5f;
        [SerializeField] private float dashCooldown = 0.7f;
        [SerializeField] private float superDashCooldown = 0.3f;
        [SerializeField] private float dashDuration = 0.17f;
        [SerializeField] private float dashSpeed = 5f;
        [SerializeField] public float maxAirSpeed = 13f;

        [Header("State")]
        [SerializeField] public bool facingRight = true;
        [SerializeField] public bool isCrouching = false;

        [SerializeField] private bool fromJump = false;
        [SerializeField] private float canJump = 0;
        [SerializeField] private bool jumpReleased = true;
        [SerializeField] private Vector2 wallNormal = Vector2.zero;
        [SerializeField] private float lockDirX = 0f;

        [SerializeField] private float superDashTimer = 0f;
        [SerializeField] private Vector2 dashDir = Vector2.zero;
        [SerializeField] public float canDash = 0;
        [SerializeField] private bool dashGrounded = false;
        [SerializeField] private bool prevDashGrounded = false;
        [SerializeField] private bool canSuperDash = false;
        [SerializeField] private bool dashReleased = true;
        [SerializeField] private float dashTimer = 0;

        [SerializeField] public float decelerationTimer = 0;

        [Header("Inputs")]
        [SerializeField] public Vector2 input;
        [SerializeField] public float jump;
        [SerializeField] public float dash;

        private bool TriggerJump() {
            bool success = false;
            Vector3 newVelocity = rb.velocity * Vector2.right + jumpVel * Vector2.up;
            if (Vector3.Dot(newVelocity.normalized, controller.SurfaceNormal) >= maxJumpCosAngle) {
                success = true;
                rb.velocity *= new Vector2(1, 0);
                rb.velocity += jumpVel * Vector2.up;
                controller.Airborne = true;
                fromJump = true;
                canJump = 0;
                rb.position = controller.bottom;
                jumpReleased = false;
            }

            JumpSmoke();
            return success;
        }

        private void ExitState() {
            switch (state) {
                case LocomotionState.Airborne:
                    controller.gravity = gravity;
                    break;
                case LocomotionState.WallSlide: Exit_WallSlide(); break;
                case LocomotionState.Dash: Exit_Dash(); break;
                case LocomotionState.Grounded: Exit_Grounded(); break;
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

        private bool cantDashTimer = false;
        private void Enter_Dash() {
            dashGrounded = false;

            // crouch dash
            isCrouching = controller.Grounded &&
                ((facingRight && input.x > 0) ||
                (!facingRight && input.x < 0) ||
                input.x == 0) &&
                input.y < 0f;
            if (isCrouching) {
                controller.size.y = crouchHeight;
            } else {
                controller.size.y = 1.3f; // TODO(randomuserhi): Make a setting variable
            }

            canDash = dashCooldown;
            dashTimer = dashDuration;
            dashDir = Vector2.zero;
            if (input.x > 0f) {
                dashDir.x = 1;
            } else if (input.x < 0f) {
                dashDir.x = -1;
            }
            if (input.y > 0f) {
                dashDir.y = 1;
            } else if (input.y < 0f) {
                dashDir.y = -1;
            }
            dashDir.Normalize();
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
            cantDashTimer = superDashTimer > 0;
            canSuperDash = superDashTimer <= 0 && ((controller.Grounded && dashDir.y < 0) || !controller.Grounded);
            if (controller.Grounded && dashDir.y < 0) {
                if (dashDir.x != 0) {
                    if (dashDir.x > 0) {
                        dashDir = Vector2.right;
                    } else {
                        dashDir = Vector2.left;
                    }
                } else {
                    if (facingRight) {
                        dashDir = Vector2.right;
                    } else {
                        dashDir = Vector2.left;
                    }
                }
            }

            controller.active = false;

            DashEffect(dashDir);
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

                        LandSmoke();
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
                        rb.velocity *= new Vector3(0.8f + 0.8f * (1f - dashTimer / dashDuration), 1f);
                    } else if (cantDashTimer) {
                        sweat = EffectLibrary.SpawnEffect(8, controller.center + new Vector2(0, 0.7f));
                        canDash = dashCooldown;
                    }

                    TriggerJump();

                    EnterState(LocomotionState.Airborne);
                    return;
                }
            } else {
                RaycastHit2D groundHit = controller.groundHit();
                if (groundHit.collider != null) {
                    if (groundHit.distance <= controller.hoverHeight + 0.05f) {
                        if (Vector3.Dot(rb.velocity, groundHit.normal) <= 0) {
                            EnterState(LocomotionState.Grounded);
                            return;
                        }
                    }
                }
                EnterState(LocomotionState.Airborne);
                return;
            }
        }

        private void Exit_Dash() {
            isCrouching = controller.Grounded &&
                ((facingRight && input.x > 0) ||
                (!facingRight && input.x < 0) ||
                input.x == 0) &&
                input.y < 0f;
            if (!isCrouching) {
                controller.size.y = 1.5f;
            }
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

        private void Exit_Grounded() {
            isCrouching = false;
        }

        private void Enter_Grounded() {
            canJump = cayoteTime;
            foreach (GameObject go in used) {
                go.SetActive(true);
                AnimatedEffect e = EffectLibrary.SpawnEffect(9, go.transform.position);
                e.reverse = true;
            }
            used.Clear();
            wallNormal = Vector2.zero;

            LandSmoke();
        }

        private void Update_Grounded() {
            Vector2 perp = -Vector2.Perpendicular(controller.SurfaceNormal).normalized;

            // crouching
            isCrouching =
                ((facingRight && input.x > 0) ||
                (!facingRight && input.x < 0) ||
                input.x == 0) &&
                input.y < 0f;

            // Check slope
            if (Vector3.Dot(Vector2.up, controller.SurfaceNormal) < maxSlopeCosAngle) {
                EnterState(LocomotionState.Slide);
            }

            // horizontal movement
            if (!isCrouching) {
                float a = acceleration;
                if (input.x != 0 && Mathf.Sign(input.x) != Mathf.Sign(rb.velocity.x)) {
                    a *= 2;
                }
                rb.velocity += input.x * a * perp;
            }

            // friction
            float speed = Vector3.Project(rb.velocity, perp).magnitude;
            if (speed != 0f) {
                float drop = speed * (isCrouching ? crouchFriction : friction) * dt;
                rb.velocity *= Mathf.Max(speed - drop, 0f) / speed; // Scale the velocity based on friction.
            }

            // Jumping
            if (jump != 0 && canJump > 0 && jumpReleased) {
                decelerationTimer = 0f;

                TriggerJump();
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
                float height = contact.point.y - controller.center.y;
                if (height > 0.3f && Vector2.Dot(new Vector2(input.x, 0).normalized, contact.normal) == -1) {
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

            // Jumping
            if (jump != 0 && jumpReleased) {
                decelerationTimer = 0f;

                if (TriggerJump()) {
                    lockDirX = wallJumpDirLock;
                }
            }
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
                    float height = contact.point.y - controller.center.y;
                    if (height > 0.3f && Vector2.Dot(new Vector2(input.x, 0).normalized, contact.normal) == -1) {
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
            float s = Mathf.Clamp(speed.x - maxAirSpeed, 0, float.PositiveInfinity);
            Vector2 d = new Vector2(
                Mathf.Clamp(drag.x / s, 0.5f, drag.x),
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

                TriggerJump();
            }
        }

        #endregion

        #region Effects

        private void DashEffect(Vector2 dashDir) {
            if (dashDir.x != 0 && dashDir.y != 0) {
                AnimatedEffect e = EffectLibrary.SpawnEffect(4, controller.center);
                e.flip = !((dashDir.x > 0 && dashDir.y > 0) || (dashDir.x < 0 && dashDir.y < 0));
            } else if (dashDir.x != 0) {
                EffectLibrary.SpawnEffect(3, controller.center);
            } else {
                AnimatedEffect e = EffectLibrary.SpawnEffect(3, controller.center);
                e.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 90));
            }
        }

        private void LandSmoke() {
            if (Mathf.Abs(rb.velocity.x) > 0.2) {
                AnimatedEffect e = EffectLibrary.SpawnEffect(1, rb.position + new Vector2(facingRight ? -0.25f : 0.25f, 0f));
                e.flip = !facingRight;
            } else {
                EffectLibrary.SpawnEffect(2, rb.position);
            }
        }

        private void JumpSmoke() {
            if (state != LocomotionState.WallSlide) {
                EffectLibrary.SpawnEffect(2, rb.position);
            } else {
                if (wallNormal.x > 0) {
                    AnimatedEffect e = EffectLibrary.SpawnEffect(1, rb.position + new Vector2(-0.4f, 0f));
                    e.flip = true;
                } else {
                    EffectLibrary.SpawnEffect(1, rb.position + new Vector2(0.4f, 0f));
                }
            }
        }

        #endregion
    }
}
