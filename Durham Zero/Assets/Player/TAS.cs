using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player.PlayerController))]
public class TAS : MonoBehaviour {
    private Player.PlayerController player;

    private struct InputState {
        public float jump;
        public float dash;
        public Vector2 input;
        public float time;
        public bool waitForGrounded;

        public InputState(float j = 0, float d = 0, float x = 0, float y = 0, float t = 0, bool w = false) {
            jump = j;
            dash = d;
            input = new Vector2(x, y);
            time = t;
            waitForGrounded = w;
        }
    }

    private void Start() {
        Debug.Break();
        player = GetComponent<Player.PlayerController>();

        states.Enqueue(new InputState(x: -1, j: 1, t: 0.2f));
        states.Enqueue(new InputState(x: 1, y: -1, d: 1));
        states.Enqueue(new InputState(x: 1, j: 1, t: 0.15f));
        states.Enqueue(new InputState(x: 1, y: -1, j: 1, d: 1));
        states.Enqueue(new InputState(x: 1, j: 1, w: true));
    }

    private bool waitForGrounded = true;
    private float timer = 0;
    private InputState current;
    private Queue<InputState> states = new Queue<InputState>();

    private void FixedUpdate() {
        if (waitForGrounded) {
            if (player.state == Player.PlayerController.LocomotionState.Grounded) {
                waitForGrounded = false;
            }
            return;
        }

        if (timer <= 0) {
            if (states.Count > 0) {
                current = states.Dequeue();
                player.input = current.input;
                player.jump = current.jump;
                player.dash = current.dash;
                waitForGrounded = current.waitForGrounded;
                timer = current.time;
            } else {
                player.input = Vector2.zero;
                player.jump = 0;
                player.dash = 0;
            }
        } else {
            timer -= Time.fixedDeltaTime;
        }
    }
}
