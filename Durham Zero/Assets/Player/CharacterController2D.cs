using Deep.Math;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterController2D : MonoBehaviour {
    private Rigidbody2D rb;

    // TODO(randomuserhi): Documentation on character controller:
    //                     - Sticky state -> character region below where they try to keep themselves stuck to the ground
    //                     - How sticky state and grounded state differ
    //                     - Management of slopes

    // Height at which controller hovers above ground
    public float hoverHeight = 0.25f;
    public float stickyHeight = 1f;
    // Size of controller (including hover height)
    public Vector2 size = new Vector2(1, 2);
    // Gravity of controller
    public Vector2 gravity = Vector2.down;
    // Layer mask for surfaces controller can stand on
    public LayerMask surfaceLayerMask = Physics2D.AllLayers;

    private SecondOrderDynamics hoverSpring = new SecondOrderDynamics(5f, 0.5f, 0f, 0f);

# if UNITY_EDITOR
    [SerializeField] private bool grounded = false;
#else
    private bool grounded = false;
#endif
    private bool prevGrounded = false;
    private bool groundedTransition = false;
    public bool Grounded { get => grounded; }
    // Did the grounded state transition to a different value
    public bool GroundedTransition { get => groundedTransition; }

#if UNITY_EDITOR
    [SerializeField] private bool sticky = false;
#else
    private bool sticky = false;
#endif
    private bool prevSticky = false;
    // Did the sticky state transition to a different value
    private bool stickyTransition = false;

#if UNITY_EDITOR
    [SerializeField] private bool isAirborne = false;
#else
    private bool isAirborne = false;
#endif

    private Vector3 surfaceNormal = Vector3.zero;
    public Vector3 SurfaceNormal { get => surfaceNormal; }

    private void Start() {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }

    private RaycastHit2D[] groundHits = new RaycastHit2D[3];
    private void HandleGrounded() {
        if (gravity == Vector2.zero) {
            grounded = false;
            prevGrounded = false;
            groundedTransition = false;

            sticky = false;
            prevSticky = false;
            stickyTransition = false;
            return;
        }

        float width = size.x;
        float height = size.y;

        if (hoverHeight > stickyHeight) {
            Debug.LogWarning("hoverHeight should not be larger than the stickyHeight of the controller.");
        }

        // Cast rays along base of character downward to detect ground surfaces within sticky range
        RaycastHit2D hit = new RaycastHit2D();
        for (int i = 0; i < groundHits.Length; ++i) {
            Vector3 point = transform.position + new Vector3(
                -width / 2f + width / Mathf.Max(2, groundHits.Length - 1) * i,
                0
                );
            groundHits[i] = Physics2D.Raycast(point, gravity, stickyHeight, surfaceLayerMask);
            if (hit.collider == null || (
                groundHits[i].collider != null &&
                groundHits[i].distance < hit.distance &&
                Vector3.Dot(gravity, groundHits[i].normal) < 0)
            ) {
                hit = groundHits[i];
            }
        }

        // Update sticky state
        sticky = hit.collider != null;
        stickyTransition = prevSticky != sticky;
        prevSticky = sticky;

        // Check grounded state given sticky state
        if (sticky) {
            grounded = hit.distance <= hoverHeight + 0.05f;
        } else {
            grounded = false;
            isAirborne = false;
        }
        groundedTransition = prevGrounded != grounded;
        prevGrounded = grounded;

        if (grounded) {
            surfaceNormal = hit.normal;

            if (groundedTransition) {
                isAirborne = false;
            }
        }

        // When transitioning out of sticky state, clamp the Y-Velocity to prevent velocity from the
        // downward pull of the sticky spring from launching the character downwards.
        //
        // Most notably a problem when falling off a downward slope:
        // 1) Downward slope pulls character downwards to maintain hover height
        // 2) When falling off slope, character maintains downward velocity from the aforementioned pull
        // 3) Character is launched downwards
        if (!sticky && stickyTransition) {
            rb.velocity = new Vector3(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -1f, Mathf.Infinity));
        }

        if (sticky && !isAirborne) {
            rb.velocity += new Vector2(0, hoverSpring.Solve(dt, hit.distance, rb.velocity.y, hoverHeight));
        }
    }

    private float dt;
    private void FixedUpdate() {
        dt = Time.fixedDeltaTime;

        HandleGrounded();

        if (!grounded) {
            rb.velocity += gravity * dt;
        }
    }
}