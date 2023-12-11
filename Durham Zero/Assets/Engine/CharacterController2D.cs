using Deep.Math;
using Deep.Physics;
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class CharacterController2D : DeepMonoBehaviour {
    [NonSerialized] public Rigidbody2D rb;
    private BoxCollider2D box;

    // TODO(randomuserhi): Documentation on character controller:
    //                     - Sticky state -> character region below where they try to keep themselves stuck to the ground
    //                     - How sticky state and grounded state differ
    //                     - Management of slopes
    //                     - Sticky and Airborne state -> whilst airborne, character won't attempt to stick to surfaces
    //                     - Bottom and collider accounts for hover height

    // Height at which controller hovers above ground
    public float hoverHeight = 0.25f;
    public float stickyHeight = 1f;
    // Size of controller (including hover height)
    public Vector2 size = new Vector2(1, 2);
    // Gravity of controller
    public float gravity = 10;
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
    [SerializeField] private bool airborne = false;
#else
    private bool airborne = false;
#endif
    private float airborneTimer = 0f;
    public bool Airborne {
        get => airborne;
        set {
            airborne = value;
            grounded = !value;
            if (value) {
                airborneTimer = 0;
            }
        }
    }

    private Vector3 surfaceNormal = Vector3.zero;
    public Vector3 SurfaceNormal { get => surfaceNormal; }

    private Vector2 _bottom = Vector2.zero;
    public Vector2 bottom { get => _bottom; }

    public Vector2 center { get => _bottom + new Vector2(0, size.y / 2f); }

    protected override void Start() {
        base.Start();

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        box = GetComponent<BoxCollider2D>();
    }

    private RaycastHit2D hit;
    private RaycastHit2D[] groundHits = new RaycastHit2D[5];
    private void HandleGrounded() {
        Vector2 gravity = Vector2.down * this.gravity;
        if (gravity == Vector2.zero) {
            grounded = false;
            prevGrounded = false;
            groundedTransition = false;

            sticky = false;
            prevSticky = false;
            stickyTransition = false;
            return;
        }

        if (hoverHeight > stickyHeight) {
            Debug.LogWarning("hoverHeight should not be larger than the stickyHeight of the controller.");
        }

        float width = size.x;

        hit = new RaycastHit2D();
        for (int i = 0; i < groundHits.Length; ++i) {
            Vector3 point = transform.position + new Vector3(
                -width / 2f + width / Mathf.Max(2, groundHits.Length - 1) * i,
                0f
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
            if (!grounded) {
                grounded = hit.distance <= hoverHeight + 0.05f && Vector3.Dot(rb.velocity, gravity) > 0;
            }
        } else {
            grounded = false;
            airborne = true;
        }
        groundedTransition = prevGrounded != grounded;
        prevGrounded = grounded;

        if (grounded) {
            surfaceNormal = hit.normal;

            if (groundedTransition || Vector3.Dot(rb.velocity, gravity) > 0) {
                airborne = false;
            }
            if (airborne) {
                // Reset airborne state if we have been grounded for a long time 
                // -> safety precaution if airborne fails to reset.
                if (airborneTimer < 1f) {
                    airborneTimer += Time.fixedDeltaTime;
                } else {
                    airborne = false;
                }
            }
        } else {
            surfaceNormal = Vector2.up;
        }

        // When transitioning out of sticky state, clamp the Y-Velocity to prevent velocity from the
        // downward pull of the sticky spring from launching the character downwards.
        //
        // Most notably a problem when falling off a downward slope:
        // 1) Downward slope pulls character downwards to maintain hover height
        // 2) When falling off slope, character maintains downward velocity from the aforementioned pull
        // 3) Character is launched downwards
        if (!sticky && stickyTransition && Vector3.Dot(rb.velocity, gravity) > 0) {
            rb.velocity = new Vector3(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -1f, Mathf.Infinity));
        }

        if (sticky && !airborne) {
            rb.velocity += new Vector2(0, hoverSpring.Solve(dt, hit.distance, rb.velocity.y, hoverHeight - 0.05f));
        }
    }

    private float dt;
    public override void LateFixedUpdate() {
        dt = Time.fixedDeltaTime;

        HandleGrounded();

        // Calcuate bounding box size relative to ground
        Vector2 _size = size;
        if (grounded) {
            _size.y -= hit.distance;
        }
        box.size = _size;
        box.offset = new Vector2(0, _size.y / 2f);

        // Calculate bottom relative to ground
        _bottom = transform.position;
        if (grounded) {
            _bottom.y -= hit.distance;
        }

        /*Debug.DrawLine(_bottom, _bottom + new Vector2(0, size.y), Color.red);
        Debug.DrawLine(_bottom + new Vector2(-0.5f, 0), _bottom + new Vector2(0.5f, 0), Color.red);*/

        if (!grounded) {
            rb.velocity += Vector2.down * gravity * dt;
        }
    }
}