using Deep.Math;
using Deep.Physics;
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class CharacterController2D : DeepMonoBehaviour {
    [NonSerialized] public Rigidbody2D rb;
    private BoxCollider2D box;

    public bool active = true;

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
    public float maxSlopeCosAngle = 0.5f;
    // Layer mask for surfaces controller can stand on
    public LayerMask surfaceLayerMask = Physics2D.AllLayers;

    private SecondOrderDynamics hoverSpring = new SecondOrderDynamics(5f, 0.5f, 0f, 0f);

    public bool slip = false;

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

    // NOTE(randomuserhi): To check for grounded, use sticky hit to check if player is sticky (will pull towards surface) then check if the player is below hover height
    //                     since the player hovers above the surface rather than being grounded to it. (sticky is how far away the player needs to be from the ground to
    //                     stick to it (e.g if they rise slightly, but stay within sticky range, the sticky spring pulls them down) and hover height is the height the sticky
    //                     spring wants the player to be at, aka their grounded height (so less than or equal to hover height is grounded)
    //                     reference: https://www.youtube.com/watch?v=qdskE8PJy6Q&ab_channel=ToyfulGames
    public RaycastHit2D StickyHit() {
        return StickyHit(out _, out _);
    }
    public RaycastHit2D StickyHit(out RaycastHit2D slipHit, out bool willBeSticky) {
        /*float width = size.x;
        RaycastHit2D hit = new RaycastHit2D();
        for (int i = 0; i < groundHits.Length; ++i) {
            Vector3 point = transform.position + new Vector3(
                -width / 2f + width / Mathf.Max(2, groundHits.Length - 1) * i,
                0f
                );
            groundHits[i] = Physics2D.Raycast(point, Vector2.down, stickyHeight, surfaceLayerMask);
            if (Vector3.Dot(Vector2.up, groundHits[i].normal) >= maxSlopeCosAngle) {
                if (hit.collider == null || (
                    groundHits[i].collider != null &&
                    groundHits[i].distance < hit.distance &&
                    Vector3.Dot(Vector2.down, groundHits[i].normal) < 0)
                ) {
                    hit = groundHits[i];
                }
            }
        }
        return hit;*/

        slipHit = new RaycastHit2D();
        RaycastHit2D hit = new RaycastHit2D();

        // Will we be sticky in the future?
        Vector2 dir = Vector2.down;
        if (rb.velocity.x != 0 && rb.velocity.y < 0) {
            dir = rb.velocity.normalized;
        }
        RaycastHit2D willBeStickyHit = Physics2D.BoxCast(rb.position, new Vector2(size.x, 0.01f), 0, dir, Mathf.Infinity, surfaceLayerMask);
        willBeSticky = false;
        if (willBeStickyHit.collider != null) {
            willBeStickyHit.distance = Mathf.Abs(rb.position.y - willBeStickyHit.point.y);
            if (willBeStickyHit.distance < stickyHeight && Vector3.Dot(Vector2.down, willBeStickyHit.normal) < 0) {
                if (Vector3.Dot(Vector2.up, willBeStickyHit.normal) >= maxSlopeCosAngle) {
                    willBeSticky = true;
                }
            }
        }

        // Are we sticky right now?
        RaycastHit2D stickyHit = Physics2D.BoxCast(rb.position, new Vector2(size.x, 0.01f), 0, Vector2.down, stickyHeight, surfaceLayerMask);
        if (stickyHit.collider != null) {
            if (stickyHit.distance < stickyHeight && Vector3.Dot(Vector2.down, stickyHit.normal) < 0) {
                if (Vector3.Dot(Vector2.up, stickyHit.normal) >= maxSlopeCosAngle) {
                    hit = stickyHit;
                } else {
                    slipHit = stickyHit;
                }
            }
        }

        return hit;
    }

#if UNITY_EDITOR
    private void DebugBox(Vector2 center, Vector2 size, Color color) {
        size /= 2f;
        Debug.DrawLine(center + new Vector2(size.x, size.y), center + new Vector2(size.x, -size.y), color);
        Debug.DrawLine(center + new Vector2(size.x, -size.y), center + new Vector2(-size.x, -size.y), color);
        Debug.DrawLine(center + new Vector2(-size.x, -size.y), center + new Vector2(-size.x, size.y), color);
        Debug.DrawLine(center + new Vector2(-size.x, size.y), center + new Vector2(size.x, size.y), color);
    }
#endif

    private RaycastHit2D cachedSlipHit;
    private RaycastHit2D cachedGroundHit;
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

        cachedGroundHit = StickyHit(out cachedSlipHit, out bool willBeSticky);

        // Update slip
        if (cachedSlipHit.collider != null && cachedSlipHit.distance <= hoverHeight + 0.05f) {
            if (!slip) {
                slip = Vector3.Dot(rb.velocity, cachedSlipHit.normal) <= 0;
            }
        } else {
            slip = false;
        }

        // Update sticky state
        sticky = cachedGroundHit.collider != null;
        stickyTransition = prevSticky != sticky;
        prevSticky = sticky;

        // Check grounded state given sticky state
        if (sticky) {
            if (!grounded) {
                grounded =
                    cachedGroundHit.distance <= hoverHeight + 0.05f &&
                    Vector3.Dot(rb.velocity, surfaceNormal) <= 0
                    ;
            }
        } else {
            grounded = false;
            airborne = true;
        }
        groundedTransition = prevGrounded != grounded;
        prevGrounded = grounded;

        // Update normals
        if (slip || sticky) {
            surfaceNormal = slip ? cachedSlipHit.normal : cachedGroundHit.normal;
        } else {
            surfaceNormal = Vector2.up;
        }

        if (grounded) {
            if (groundedTransition || Vector3.Dot(rb.velocity, gravity) > 0) {
                airborne = false;
            }
            if (airborne) {
                // Reset airborne state if we have been grounded for a long time 
                // -> safety precaution if airborne fails to reset.
                if (airborneTimer < 0.5f) {
                    airborneTimer += Time.fixedDeltaTime;
                } else {
                    airborne = false;
                }
            }
        }

        // Only stick to surface if player will remain sticky
        if (willBeSticky && sticky && !airborne) {
            rb.velocity += new Vector2(0, hoverSpring.Solve(dt, cachedGroundHit.distance, rb.velocity.y, hoverHeight - 0.05f));
        }
    }

    private float dt;
    public override void LateFixedUpdate() {
        dt = Time.fixedDeltaTime;

        if (active) HandleGrounded();

        // Calcuate bounding box size relative to ground
        Vector2 _size = size;
        if (grounded) {
            _size.y -= cachedGroundHit.distance;
        }
        box.size = _size;
        box.offset = new Vector2(0, _size.y / 2f);

        // Calculate bottom relative to ground
        _bottom = transform.position;
        if (grounded) {
            _bottom.y -= cachedGroundHit.distance;
        }

        /*Debug.DrawLine(_bottom, _bottom + new Vector2(0, size.y), Color.red);
        Debug.DrawLine(_bottom + new Vector2(-0.5f, 0), _bottom + new Vector2(0.5f, 0), Color.red);*/

        if (!grounded) {
            rb.velocity += Vector2.down * gravity * dt;
        }
    }
}