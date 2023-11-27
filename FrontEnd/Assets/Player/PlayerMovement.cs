using UnityEngine;

class SecondOrderDynamics {
    private float xp;
    private float k1, k2, k3;

    public SecondOrderDynamics(float f = 0, float z = 0, float r = 0, float x0 = 0) {
        init(f, z, r, x0);
    }

    public void init(float f, float z, float r, float x0 = 0) {
        k1 = z / (Mathf.PI * f);
        k2 = 1 / ((2 * Mathf.PI * f) * (2 * Mathf.PI * f));
        k3 = r * z / (2 * Mathf.PI * f);

        xp = x0;
    }

    public float Solve(float T, float y, float yd, float x, float? xd = null) {
        if (xd == null) {
            xd = (x - xp) / T;
            xp = x;
        }
        float k2_stable = Mathf.Max(k2, 1.1f * (T * T / 4 + T * k1 / 2));
        //y = y + T * yd;
        //yd = yd + T * (x + k3 * xd.Value - y - k1 * yd) / k2_stable;
        //return y;
        //return yd;
        return T * (x + k3 * xd.Value - y - k1 * yd) / k2_stable;
    }
}

namespace Player {
    public class PlayerMovement : MonoBehaviour {
        [System.NonSerialized]
        public Rigidbody2D rb;

        public enum State {
            movement,
            stance,
            attack,
            recovery,
            dash,
            stunned
        }
        public State state = State.movement;

        public Vector2 gravity = new Vector2(0, -1);

        public Vector2 input = Vector2.zero;
        public Vector2 attackDir = Vector2.zero;

        public bool facingRight = true;

        private bool prevSticky;
        private bool stickyTransition;
        public bool sticky;
        private bool prevGrounded;
        private bool groundedTransition;
        public bool grounded;
        public bool isAirborne;
        private SecondOrderDynamics groundSpring = new SecondOrderDynamics(5f, 0.5f, 0f, 0f);

        public bool canJump = true;
        public bool airJump = true;
        public float isAirborneTimer = 0;

        public float floatHeight = 0.7f;

        public float speed = 5;
        public float friction = 0.7f;

        private float dashCooldown = 0;
        private float dashTimer = 0;
        private int dashAirborne = 0;
        private bool readyToDash = false;

        private float atkTimer = 0;
        private int slashAirborne = 0;
        private bool readyToAtk = false;

        private float groundVel = 0f;

        void Start() {
            rb = GetComponent<Rigidbody2D>();
        }

        private RaycastHit2D[] groundHits = new RaycastHit2D[3];
        void FixedUpdate() {
            // TODO(randomuserhi): Lerp rotation in direction of gravity so that legs always face down etc...
            // TODO(randomuserhi): Documentation
            // TODO(randomuserhi): Use SecondOrderDynamics to approach target velocity and clamp its value (velocity, max acceleration) essentially.
            // TODO(randomuserhi): Scale acceleration by change in velocity (going left, then right is fast transition)
            // TODO(randomuserhi): cayote time
            // TODO(randomuserhi): Update all calculations that assume gravity is down in negative y direction

            float width = 1f;
            RaycastHit2D hit = new RaycastHit2D();
            for (int i = 0; i < groundHits.Length; ++i) {
                groundHits[i] = Physics2D.Raycast(
                    transform.position + new Vector3(-width / 2f + width / Mathf.Max(2, groundHits.Length - 1) * i, 0),
                    gravity,
                    1f + floatHeight * 1.5f,
                    LayerMask.GetMask("surface")
                    );
                if (hit.collider == null || (groundHits[i].collider != null && groundHits[i].distance > 0.5f && groundHits[i].distance < hit.distance && Vector3.Dot(gravity, groundHits[i].normal) < 0)) {
                    hit = groundHits[i];
                }
            }
            sticky = hit.collider != null;
            stickyTransition = prevSticky != sticky;
            prevSticky = sticky;

            if (sticky) {
                grounded = hit.distance <= 1f + floatHeight + 0.2f;
            } else {
                grounded = false;
            }
            groundedTransition = prevGrounded != grounded;
            prevGrounded = grounded;

            if (!sticky && stickyTransition) {
                rb.velocity = new Vector3(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -1f, Mathf.Infinity));
            }
            if (sticky && stickyTransition) {
                groundVel = rb.velocity.y;
            }
            if (grounded && groundedTransition) {
                isAirborne = false;
            }
            if (isAirborne && grounded) {
                if (isAirborneTimer < 0.1f) {
                    isAirborneTimer += Time.fixedDeltaTime;
                } else {
                    isAirborne = false;
                }
            }

            if (grounded) {
                dashAirborne = 0;
                slashAirborne = 0;
            }

            rb.velocity += gravity;

            input.x = 0;
            if (Input.GetKey(KeyCode.A)) {
                input.x += -1;
            }
            if (Input.GetKey(KeyCode.D)) {
                input.x += 1;
            }
            input.y = 0;
            if (Input.GetKey(KeyCode.W)) {
                input.y += 1;
            }
            if (Input.GetKey(KeyCode.S)) {
                input.y += -1;
            }

            if (Input.GetKey(KeyCode.J) && !readyToDash && state != State.attack && state != State.recovery) {
                state = State.stance;
                readyToAtk = true;
            } else if (Input.GetKey(KeyCode.I) && !readyToAtk && state != State.attack && state != State.recovery && state != State.dash && dashCooldown <= 0f) {
                state = State.stance;
                readyToDash = true;
            } else {
                if (readyToAtk && (input.x != 0 || input.y != 0)) {
                    state = State.attack;
                    readyToAtk = false;
                    atkTimer = 0;
                    if (input.x > 0) {
                        facingRight = true;
                    } else if (input.x < 0) {
                        facingRight = false;
                    }

                    attackDir = Vector2.zero;
                    if (input.x > 0) {
                        attackDir.x = 1;
                    } else if (input.x < 0) {
                        attackDir.x = -1;
                    }

                    if (input.y > 0) {
                        attackDir.y = 1;
                        isAirborneTimer = 0;
                        isAirborne = true;
                    } else if (input.y < 0) {
                        attackDir.y = -1;
                    }

                    float upward = attackDir.y * 15f;
                    if (input.y > 0) {
                        upward /= ++slashAirborne;
                    }
                    if (rb.velocity.x == 0 || Mathf.Sign(rb.velocity.x) == Mathf.Sign(attackDir.x)) {
                        rb.velocity += new Vector2(attackDir.x * 10f, 0);
                        /*if (sticky && rb.velocity.y > 0) {
                            rb.velocity += new Vector2(0, attackDir.y * 10f);
                        } else*/ // TODO(randomuserhi): fix power jump (jump and attacking at same time)
                        if (slashAirborne < 2) {
                            rb.velocity = new Vector2(rb.velocity.x, upward);
                        }
                    } else {
                        rb.velocity = new Vector2(attackDir.x * 20f, upward);
                    }
                } else if (readyToDash && (input.x != 0 || input.y != 0)) {
                    state = State.dash;
                    readyToDash = false;
                    dashTimer = 0;
                    if (input.x > 0) {
                        facingRight = true;
                    } else if (input.x < 0) {
                        facingRight = false;
                    }

                    attackDir = Vector2.zero;
                    if (input.x > 0) {
                        attackDir.x = 1;
                    } else if (input.x < 0) {
                        attackDir.x = -1;
                    }

                    if (input.y > 0) {
                        attackDir.y = 1;
                        isAirborneTimer = 0;
                        isAirborne = true;
                    } else if (input.y < 0) {
                        attackDir.y = -1;
                    }

                    float upward = attackDir.y * 15f;
                    if (input.y > 0) {
                        upward /= ++dashAirborne;
                    }
                    if (rb.velocity.x == 0 || Mathf.Sign(rb.velocity.x) == Mathf.Sign(attackDir.x)) {
                        rb.velocity += new Vector2(attackDir.x * 15f, 0);
                        rb.velocity = new Vector2(rb.velocity.x, upward);
                    } else {
                        rb.velocity = new Vector2(attackDir.x * 20f, upward);
                    }
                } else if (state != State.attack && state != State.recovery && state != State.dash) {
                    readyToAtk = false;
                    readyToDash = false;
                    state = State.movement;
                }
            }
            if (state == State.attack) {
                if (atkTimer > 0.13f) {
                    state = State.recovery;
                    atkTimer = 0;
                }
                atkTimer += Time.fixedDeltaTime;
            } else if (state == State.recovery) {
                if (atkTimer > 0.13f) {
                    state = State.movement;
                    atkTimer = 0;
                }
                atkTimer += Time.fixedDeltaTime;
            } else if (state == State.dash) {
                if (dashTimer > 0.13f) {
                    state = State.movement;
                    dashTimer = 0;
                    dashCooldown = 0.3f;
                }
                dashTimer += Time.fixedDeltaTime;
            }
            if (dashCooldown > 0) {
                dashCooldown -= Time.fixedDeltaTime;
            }

            Movement();

            if (state != State.movement) {
                if (grounded) {
                    rb.velocity *= new Vector2(0.95f, 1);
                }
            }

            if (sticky && !isAirborne) {
                rb.velocity += new Vector2(0, groundSpring.Solve(Time.fixedDeltaTime, hit.distance, rb.velocity.y, 1f + floatHeight - 0.2f));
            }
        }

        private bool jumped = false;
        void Movement() {
            if (state == State.movement) {
                if (input.x > 0) {
                    facingRight = true;
                } else if (input.x < 0) {
                    facingRight = false;
                }

                // TODO(randomuserhi): make velocity be parallel to surface player is standing on (i have its normal from the ray cast hit)
                float s = speed;
                if (Mathf.Sign(input.x) != Mathf.Sign(rb.velocity.x)) {
                    s *= 1 + Mathf.Clamp(Mathf.Abs(rb.velocity.x) / 16, 0, 2);
                }
                rb.velocity += new Vector2(input.x * s, 0);
                rb.velocity *= new Vector2(friction, 1);
            }

            if (state == State.movement || state == State.stance) {
                if (!isAirborne) {
                    canJump = true;
                    airJump = true;
                }
                if (Input.GetAxis("Jump") == 0) {
                    jumped = false;
                }
                if (Input.GetAxis("Jump") > 0 && ((grounded && canJump) || airJump) && !jumped) {
                    jumped = true;
                    rb.velocity *= new Vector2(1, 0);
                    //rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, 0));
                    rb.velocity += 17 * -gravity.normalized;
                    if (grounded && canJump) {
                        canJump = false;
                    } else {
                        airJump = false;
                    }
                    isAirborneTimer = 0;
                    isAirborne = true;
                }
            }
        }
    }
}
