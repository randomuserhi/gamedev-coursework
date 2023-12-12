using Deep.Anim;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimRenderer : MonoBehaviour {
    public int frame;
    public Anim anim;

    private AnimDriver driver = new AnimDriver();
    private SpriteRenderer spriteRenderer;

    // Start is called before the first frame update
    void Start() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update() {
        if (anim == null) return;

        driver.anim = anim;
        driver.frame = frame;
        spriteRenderer.sprite = driver.sprite;
    }
}
