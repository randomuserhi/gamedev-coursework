using UnityEngine;

namespace Deep.Anim {
    [CreateAssetMenu(fileName = "Animation", menuName = "Deep/Animation")]
    public class Anim : ScriptableObject {
        public int frameRate = 10;
        public Vector3 offset = Vector3.zero;
        public Sprite[] sprites = new Sprite[0];
    }
}
