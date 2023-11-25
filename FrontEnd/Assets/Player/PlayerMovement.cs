using UnityEngine;

namespace Player {
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

    public class PlayerMovement : MonoBehaviour {
        [System.NonSerialized]
        public Rigidbody2D rb;

        public Vector2 gravity = new Vector2(0, -1);

        public bool facingRight = true;

        public bool sticky;
        private bool prevGrounded;
        private bool groundedTransition;
        public bool grounded;
        public bool isAirborne;
        private SecondOrderDynamics groundSpring = new SecondOrderDynamics(5f, 0.5f, 0f, 0f);

        public bool canJump = true;
        public float isAirborneTimer = 0;

        public float floatHeight = 0.7f;

        public float speed = 10f;
        public float friction = 0.6f;

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

            float width = 1f;
            RaycastHit2D hit = new RaycastHit2D();
            for (int i = 0; i < groundHits.Length; ++i) {
                groundHits[i] = Physics2D.Raycast(
                    transform.position + new Vector3(-width / 2f + width / Mathf.Max(2, groundHits.Length - 1) * i, 0),
                    gravity,
                    1f + floatHeight * 1.5f,
                    LayerMask.GetMask("surface")
                    );
                if (hit.collider == null || (groundHits[i].collider != null && groundHits[i].distance > 0 && groundHits[i].distance < hit.distance && Vector3.Dot(gravity, groundHits[i].normal) < 0)) {
                    hit = groundHits[i];
                }
            }
            sticky = hit.collider != null;

            if (sticky) {
                grounded = hit.distance <= 1f + floatHeight + 0.2f;
            } else {
                grounded = false;
            }
            groundedTransition = prevGrounded != grounded;
            prevGrounded = grounded;

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

            rb.velocity += gravity;

            Movement();

            if (sticky && !isAirborne) {
                rb.velocity += new Vector2(0, groundSpring.Solve(Time.fixedDeltaTime, hit.distance, rb.velocity.y, 1f + floatHeight - 0.2f));
            }
        }

        void Movement() {
            if (Input.GetAxis("Horizontal") > 0) {
                facingRight = true;
            } else if (Input.GetAxis("Horizontal") < 0) {
                facingRight = false;
            }

            float s = speed;
            if (Mathf.Sign(Input.GetAxis("Horizontal")) != Mathf.Sign(rb.velocity.x)) {
                s *= 1 + Mathf.Clamp(Mathf.Abs(rb.velocity.x) / 3, 0, 2);
            }
            rb.velocity += new Vector2(Input.GetAxis("Horizontal") * s, 0);
            rb.velocity *= new Vector2(friction, 1);

            if (!isAirborne) {
                canJump = true;
            }
            if (Input.GetAxis("Vertical") != 0 && grounded && canJump) {
                rb.velocity *= new Vector2(1, 0);
                //rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, 0));
                rb.velocity += 20 * -gravity.normalized;
                canJump = false;
                isAirborneTimer = 0;
                isAirborne = true;
            }
        }
    }
}
