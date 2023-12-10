using UnityEngine;

[RequireComponent(typeof(CharacterController2D))]
public class PlayerController : MonoBehaviour {
    CharacterController2D controller;

    private void Start() {
        controller = GetComponent<CharacterController2D>();
    }

    private void FixedUpdate() {

    }
}
