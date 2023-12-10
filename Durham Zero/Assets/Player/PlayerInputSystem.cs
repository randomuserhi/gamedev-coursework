using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player {
    public class PlayerInputSystem : MonoBehaviour {
        public InputActionMap map;
        [NonSerialized] public InputAction movement;

        private void Awake() {
            // TODO(randomuserhi): Load from config => otherwise generate default

            /*map = new InputActionMap("Player Gameplay");
            movement = map.AddAction("Movement");
            movement.AddCompositeBinding("2DVector(mode=0)")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");*/
            //movement.AddBinding("<Gamepad>/dpad");
            //movement.AddBinding("<Gamepad>/leftStick");

            map.Enable();
        }

        private void OnEnable() {
            if (map == null) return;
            map.Enable();
        }

        private void OnDisable() {
            if (map == null) return;
            map.Disable();
        }
    }
}
