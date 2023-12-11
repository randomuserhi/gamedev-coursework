using Deep.Anim;
using UnityEngine;

namespace Player {
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(CharacterController2D))]
    public class PlayerRig : MonoBehaviour {
        private SpriteRenderer character;
        private SpriteRenderer secondary;
        private SpriteRenderer sword;

        private CharacterController2D controller;

        public enum AnimState {
            IdleChill,
            IdleToChill,
            Idle,
            Airborne,
            Walk,
            Run,
        }
        private AnimState state = AnimState.Idle;

        private void Start() {
            controller = GetComponent<CharacterController2D>();

            GameObject _character = new GameObject();
            character = _character.AddComponent<SpriteRenderer>();
            character.transform.parent = transform;
            character.transform.localPosition = Vector3.zero;
            character.enabled = true;

            GameObject _secondary = new GameObject();
            secondary = _secondary.AddComponent<SpriteRenderer>();
            secondary.transform.parent = transform;
            secondary.transform.localPosition = Vector3.zero;
            secondary.enabled = false;

            GameObject _sword = new GameObject();
            sword = _sword.AddComponent<SpriteRenderer>();
            sword.transform.parent = transform;
            sword.transform.localPosition = Vector3.zero;
            sword.enabled = false;

            Enter_Idle();
        }

        public void Update() {
            Vector2 position = controller.bottom;
            character.transform.position = position + primaryAnim.offset;
            secondary.transform.position = position + secondaryAnim.offset;
            sword.transform.position = position + swordAnim.offset;

            switch (state) {
                case AnimState.Idle:
                case AnimState.IdleChill:
                case AnimState.IdleToChill:
                    if (controller.Grounded) {

                    } else {
                        Enter_Airborne();
                    }
                    break;
            }

            switch (state) {
                case AnimState.IdleChill:
                    Update_IdleChill();
                    break;
                case AnimState.Idle:
                    Update_Idle();
                    break;
                case AnimState.IdleToChill:
                    Update_IdleToChill();
                    break;
            }
        }

        #region Global State

        private AnimDriver primaryAnim = new AnimDriver();
        private AnimDriver secondaryAnim = new AnimDriver();
        private AnimDriver swordAnim = new AnimDriver();

        [Header("Library")]

        #endregion

        #region IdleChill State

        public Anim IdleChillAnim;

        private void Enter_IdleChill() {
            state = AnimState.IdleChill;
            primaryAnim.Set(IdleChillAnim);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_IdleChill() {
            primaryAnim.AutoIncrement();
            character.sprite = primaryAnim.sprite;
        }

        #endregion

        #region IdleToChill

        public Anim IdleToChillAnim;

        private void Enter_IdleToChill() {
            state = AnimState.IdleToChill;
            primaryAnim.Set(IdleToChillAnim);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_IdleToChill() {
            if (primaryAnim.AutoIncrement()) {
                Enter_IdleChill();
            }
            character.sprite = primaryAnim.sprite;
        }

        #endregion

        #region Idle State

        private float idleTimer = 0;
        public Anim IdleAnim;

        private void Enter_Idle() {
            state = AnimState.Idle;
            primaryAnim.Set(IdleAnim);

            character.enabled = true;
            secondary.enabled = false;

            idleTimer = 3f;
        }

        private void Update_Idle() {
            bool looped = primaryAnim.AutoIncrement();
            character.sprite = primaryAnim.sprite;
            if (idleTimer <= 0 && looped) {
                Enter_IdleToChill();
            } else {
                idleTimer -= Time.deltaTime;
            }
        }

        #endregion

        #region Airborne State

        public Anim JumpFallAnim;

        private void Enter_Airborne() {
            state = AnimState.Idle;
            primaryAnim.Set(JumpFallAnim);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_Airborne() {
            float vy = controller.rb.velocity.y;
            if (vy > 1) {
                primaryAnim.frame = 0;
            } else if (vy > 0) {
                primaryAnim.frame = 1;
            } else if (vy < -1) {
                primaryAnim.frame = 2;
            } else {
                primaryAnim.frame = 3;
            }
        }

        #endregion
    }
}
