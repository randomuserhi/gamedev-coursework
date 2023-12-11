using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player {
    public class PlayerInputSystem : MonoBehaviour {
        [NonSerialized] public InputActionMap map;
        [NonSerialized] public InputAction movement;
        [NonSerialized] public InputAction jump;
        [NonSerialized] public InputAction dash;

        private void Start() {
            // TODO(randomuserhi): Load from config => otherwise generate default

            map = new InputActionMap("Player Gameplay");

            movement = map.AddAction("Movement");
            movement.AddCompositeBinding("2DVector(mode=0)")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            //movement.AddBinding("<Gamepad>/dpad");
            //movement.AddBinding("<Gamepad>/leftStick");

            jump = map.AddAction("Jump");
            jump.AddBinding("<Keyboard>/space");

            dash = map.AddAction("Dash");
            dash.AddBinding("<Keyboard>/i");

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
