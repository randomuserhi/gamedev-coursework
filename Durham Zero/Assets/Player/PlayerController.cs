using UnityEngine;

namespace Player {
    [RequireComponent(typeof(CharacterController2D))]
    public class PlayerController : MonoBehaviour {
        public enum State {
            Walk
        }

        private CharacterController2D controller;
        private State state;

        private void Start() {
            controller = GetComponent<CharacterController2D>();
        }

        private void FixedUpdate() {

        }
    }
}
