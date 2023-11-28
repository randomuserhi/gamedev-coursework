using Player;
using UnityEngine;

public class CameraFollow : MonoBehaviour {
    public PlayerMovement target;
    private Rigidbody2D rb;
    private SecondOrderDynamics follow = new SecondOrderDynamics(15f, 1, 0);

    private void Start() {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update() {
        if (target == null) return;

        rb.velocity = new Vector2(
            follow.Solve(Time.deltaTime, transform.position.x, rb.velocity.x, target.transform.position.x, target.rb.velocity.x),
            follow.Solve(Time.deltaTime, transform.position.y, rb.velocity.y, target.transform.position.y, target.rb.velocity.y)
            );
    }
}
