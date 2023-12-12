using UnityEngine;

namespace Player {
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(CharacterController2D))]
    [RequireComponent(typeof(PlayerRig))]
    public class ScalfRig : MonoBehaviour {
        private struct Verlet {
            public Vector2 position;
            public Vector2 prev;
            public Vector2 acceleration;
        }
        private struct VerletLinks {
            public int a;
            public int b;
        }

        private PlayerController player;
        private CharacterController2D controller;
        private PlayerRig rig;

        [Header("Settings")]
        [SerializeField] private GameObject point;
        [SerializeField] private int subSteps = 3;
        [SerializeField] private int numPoints = 3;
        [SerializeField] private float spacing = 0.1f;
        [SerializeField] private float gravity = 1f;
        [SerializeField] private float friction = 0.2f;
        [SerializeField] private Vector2 offset = Vector2.zero;

        [Header("State")]
        [SerializeField] private Vector2 neck;
        GameObject[] points;
        Verlet[] verlets;
        VerletLinks[] links;

        // Start is called before the first frame update
        private void Start() {
            player = GetComponent<PlayerController>();
            controller = GetComponent<CharacterController2D>();
            rig = GetComponent<PlayerRig>();

            neck = transform.position;

            points = new GameObject[numPoints];
            for (int i = 0; i < numPoints; ++i) {
                points[i] = Instantiate(point, transform);
            }
            verlets = new Verlet[numPoints];
            links = new VerletLinks[numPoints - 1];
            for (int i = 0; i < numPoints; ++i) {
                verlets[i].position = neck;
                if (i > 0) {
                    links[i - 1].a = i - 1;
                    links[i - 1].b = i;
                }
            }
        }

        // Update is called once per frame
        private void FixedUpdate() {
            neck = controller.bottom + rig.primaryAnim.offset + ((player.facingRight ? new Vector2(1f, 1f) : new Vector2(-1f, 1f)) * rig.primaryAnim.current.Scalf) + offset;

            verlets[0].position = neck;

            // integrate
            float dt = Time.fixedDeltaTime / subSteps;
            for (int j = 0; j < subSteps; ++j) {
                for (int i = 1; i < numPoints; ++i) {
                    ref Verlet v = ref verlets[i];
                    Vector2 velocity = v.position - v.prev;
                    v.prev = v.position;
                    v.position = v.position + velocity * Mathf.Clamp01(1f - friction) + v.acceleration * dt * dt;
                    v.acceleration = Vector2.zero;
                }

                // gravity + drag
                for (int i = 1; i < numPoints; ++i) {
                    ref Verlet v = ref verlets[i];
                    v.acceleration += Vector2.down * gravity;
                }

                // links
                for (int i = 0; i < links.Length; ++i) {
                    VerletLinks link = links[i];
                    ref Verlet a = ref verlets[link.a];
                    ref Verlet b = ref verlets[link.b];
                    Vector2 axis = a.position - b.position;
                    if (a.position == b.position) {
                        axis = Vector2.right * 0.01f;
                    }
                    float dist = axis.magnitude;
                    Vector2 n = axis / dist;
                    float delta = spacing - dist;
                    if (link.a != 0) {
                        a.position += 0.5f * delta * n;
                        b.position -= 0.5f * delta * n;
                    } else {
                        b.position -= delta * n;
                    }
                }
            }

            for (int i = 0; i < numPoints; ++i) {
                points[i].transform.position = verlets[i].position;
            }
        }
    }
}
