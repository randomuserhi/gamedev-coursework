using Deep.Anim;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedEffect : MonoBehaviour {
    [SerializeField] private Anim anim;
    public bool loop = false;
    public Color color = Color.white;

    private AnimDriver driver = new AnimDriver();
    private SpriteRenderer effect;

    private void Start() {
        driver.Set(anim);
        effect = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update() {
        if (driver.AutoIncrement() && !loop) {
            Destroy(this);
        }
        effect.sprite = driver.sprite;
        effect.color = color;
    }
}
