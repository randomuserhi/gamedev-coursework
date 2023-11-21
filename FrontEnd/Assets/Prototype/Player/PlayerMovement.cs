using UnityEngine;

public class PlayerMovement : MonoBehaviour {
    private Rigidbody rb;

    // TODO(randomuserhi): make readonly
    public bool grounded = false;

    private void Start() {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate() {
        // shoot a ray cast straight down
        if (Physics.Raycast(rb.position, Vector3.down, out RaycastHit hit, 1f)) {
            grounded = true;

            // TODO(randomuserhi): float above the ground? => physics character body video as reference => need to make it stable

            // TODO(randomuserhi): not sure if i should implement force equations to negate gravity or just set y velocity to 0
            //                     refer to impulse physics engine series by that chinese guy
            rb.velocity = Vector3.Scale(rb.velocity, new Vector3(1, 0, 1));
        } else {
            grounded = false;

            // apply gravity
            rb.velocity += Vector3.down * 0.01f;
        }
    }
}
