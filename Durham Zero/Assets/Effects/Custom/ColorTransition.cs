using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ColorTransition : MonoBehaviour {
    public Color startColor;
    public Color endColor;
    public float speed = 1;
    public float duration = 0.5f;
    public bool flip = false;

    private SpriteRenderer effect;

    private void Start() {
        effect = GetComponent<SpriteRenderer>();
        effect.color = startColor;
        effect.flipX = flip;
    }

    // Update is called once per frame
    void Update() {
        effect.color = Color.Lerp(effect.color, endColor, speed * Time.deltaTime);
        if (duration <= 0) {
            Destroy(gameObject);
        } else {
            duration -= Time.deltaTime;
        }
    }
}
