using UnityEngine;

namespace Player {
    public class PlayerRig : MonoBehaviour {
        public PlayerMovement player;

        // Start is called before the first frame update
        void Start() {

        }

        // Update is called once per frame
        void Update() {
            transform.position = player.transform.position;
        }
    }
}
