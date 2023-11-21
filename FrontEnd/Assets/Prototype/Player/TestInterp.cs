using UnityEngine;

public class SecondOrderDynamics {
    private float k1, k2, k3;
    private Vector3 xp;

    public SecondOrderDynamics(float f, float z, float r, Vector3 x0) {
        k1 = z / (Mathf.PI * f);
        k2 = 1 / ((2 * Mathf.PI * f) * (2 * Mathf.PI * f));
        k3 = r * z / (2 * Mathf.PI * f);
    }

    public void Update(float T, Rigidbody rb, Vector3 x, Vector3? xd = null) {
        if (xd == null) {
            xd = (x - xp) / T;
            xp = x;
        }
        float k2_stable = Mathf.Max(k2, 1.1f * (T * T / 4 + T * k1 / 2));
        Vector3 pos = rb.position + T * rb.velocity;
        rb.velocity = rb.velocity + T * (x + k3 * xd.Value - pos - k1 * rb.velocity) / k2_stable;
    }
}

public class TestInterp : MonoBehaviour {
    private SecondOrderDynamics groundDynamics = new SecondOrderDynamics(4, 0.35f, 0, Vector3.zero);

    private Rigidbody rb;

    public GameObject target;

    // Start is called before the first frame update
    private void Start() {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    private void FixedUpdate() {
        if (target != null) {
            groundDynamics.Update(Time.deltaTime, rb, target.transform.position);
        }
    }
}
