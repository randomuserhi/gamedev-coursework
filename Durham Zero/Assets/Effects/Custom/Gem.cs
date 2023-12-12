using UnityEngine;

public class Gem : MonoBehaviour {
    public float oscillateHeight = 0.6f;
    public float oscillateSpeed = 0.5f;

    private float timer = 0;

    private Vector3 origin;
    private void Start() {
        origin = transform.position;
    }

    // Update is called once per frame
    void FixedUpdate() {
        timer += oscillateSpeed * Time.deltaTime;
        Vector3 pos = origin + new Vector3(0, Mathf.Cos(timer) * oscillateHeight, 0);
        pos.z = 5f;
        transform.position = pos;
    }
}
