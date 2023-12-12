using UnityEngine;

public class CameraFollow : MonoBehaviour {
    public static Vector3 offset = Vector3.zero;
    public GameObject target;
    public float speed = 5f;

    // Update is called once per frame
    void FixedUpdate() {
        if (target == null) return;

        Vector3 goal = target.transform.position + offset;

        goal.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, goal, speed * Time.deltaTime);
    }
}
