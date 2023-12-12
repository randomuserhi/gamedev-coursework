using UnityEngine;

public class EffectLibrary : MonoBehaviour {
    private static EffectLibrary instance = null;

    [RuntimeInitializeOnLoadMethod]
    private static void Initialize() {
        EffectLibrary exists = FindAnyObjectByType<EffectLibrary>();
        if (exists != null) return;

        GameObject go = new GameObject();
        go.name = "EffectLibrary";
        go.AddComponent<EffectLibrary>();
    }

    [SerializeField] private GameObject[] library;

    public static AnimatedEffect SpawnEffect(int effect, Vector2 position, float z = 0) {
        GameObject e = Instantiate(instance.library[effect]);
        e.transform.position = new Vector3(position.x, position.y, z);
        return e.GetComponent<AnimatedEffect>();
    }

    public void Start() {
        if (instance == null) {
            instance = this;
        } else {
            Debug.LogError("Only one effect library can be active at a time.");
            return;
        }
    }
}
