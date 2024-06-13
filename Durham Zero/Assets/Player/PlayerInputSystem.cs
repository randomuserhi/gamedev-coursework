using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Player {
    public class PlayerInputSystem : MonoBehaviour {
        [NonSerialized] public InputActionMap map;
        [NonSerialized] public InputAction movement;
        [NonSerialized] public InputAction jump;
        [NonSerialized] public InputAction dash;
        [NonSerialized] public InputAction reset;

        private PlayerController controller;

        private void Start() {
            // TODO(randomuserhi): Load from config => otherwise generate default

            map = new InputActionMap("Player Gameplay");

            movement = map.AddAction("Movement");
            movement.AddCompositeBinding("2DVector(mode=1)")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            movement.AddBinding("<Gamepad>/dpad");
            movement.AddBinding("<Gamepad>/leftStick");
            movement.AddBinding("<Joystick>/stick");

            jump = map.AddAction("Jump");
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<HID::RetroFlag Wired Controller>/button2");

            dash = map.AddAction("Dash");
            dash.AddBinding("<Keyboard>/i");
            dash.AddBinding("<HID::RetroFlag Wired Controller>/button4");

            reset = map.AddAction("Reset");
            reset.AddBinding("<Keyboard>/r");

            map.Enable();

            /*InputActionRebindingExtensions.RebindingOperation rebindOperation = null;
            rebindOperation = movement.PerformInteractiveRebinding()
                // To avoid accidental input from mouse motion
                .WithControlsExcluding("Mouse")
                .OnMatchWaitForAnother(0.1f)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(operation => {
                    if (rebindOperation != null) {
                        rebindOperation.Dispose();
                    }
                })
                .OnComplete(operation => {
                    //map.Enable();
                    if (rebindOperation != null) {
                        rebindOperation.Dispose();
                    }
                    Debug.Log(movement.bindings[0].effectivePath);
                });
            rebindOperation.Start();*/

            controller = GetComponent<PlayerController>();
        }

        private void FixedUpdate() {
            if (controller != null) {
                controller.input = movement.ReadValue<Vector2>();
                if (Math.Abs(controller.input.x) < 0.01f) {
                    controller.input.x = 0;
                }
                if (Math.Abs(controller.input.y) < 0.01f) {
                    controller.input.y = 0;
                }
                controller.jump = jump.ReadValue<float>();
                controller.dash = dash.ReadValue<float>();
            }

            if (reset.ReadValue<float>() > 0) {
                SceneManager.LoadScene(0);
            }
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
