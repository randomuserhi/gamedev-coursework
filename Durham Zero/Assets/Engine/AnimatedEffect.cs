using Deep.Anim;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedEffect : MonoBehaviour {
    [SerializeField] private Anim anim;
    public bool loop = false;
    public bool reverse = false;
    public Color color = Color.white;

    private AnimDriver driver = new AnimDriver();
    private SpriteRenderer effect;

    private void Start() {
        driver.Set(anim, reverse ? anim.sprites.Length : -1);
        effect = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update() {
        if (reverse) {
            if (driver.AutoDecrement() && !loop) {
                Kill();
            }
        } else {
            if (driver.AutoIncrement() && !loop) {
                Kill();
            }
        }
        effect.sprite = driver.sprite;
        effect.color = color;
    }

    private void Kill() {
        Destroy(this.gameObject);
    }
}
