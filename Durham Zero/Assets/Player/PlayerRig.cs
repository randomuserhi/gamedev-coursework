using Deep.Anim;
using Deep.Math;
using System;
using UnityEngine;

namespace Player {
    [RequireComponent(typeof(PlayerInputSystem))]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(CharacterController2D))]
    public class PlayerRig : MonoBehaviour {
        private SpriteRenderer character;
        private SpriteRenderer secondary;

        private CharacterController2D controller;
        private PlayerInputSystem inputSystem;
        private PlayerController player;

        public enum AnimState {
            IdleChill,
            IdleToChill,
            Idle,
            Airborne,
            WallSlide,
            Land,
            ToWalkRun,
            Walk,
            Run,
            ToSlide,
            Slide,
            RunFlip,
            WalkFlip,
            Dash,
            Stance,
            ToCrouch,
            Crouch,
            FromCrouch,
        }
        [SerializeField] private AnimState _state = AnimState.Idle;
        private AnimState prevState = AnimState.Idle;
        private AnimState state {
            get => _state;
            set {
                prevState = _state;
                _state = value;
            }
        }

        private void Start() {
            controller = GetComponent<CharacterController2D>();
            inputSystem = GetComponent<PlayerInputSystem>();
            player = GetComponent<PlayerController>();

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

            Enter_Idle();
        }

        public Anim test;

        private SecondOrderDynamics2D scaleSpring = new SecondOrderDynamics2D(5f, 0.8f, 1.2f);

        public void FixedUpdate() {
            float topSpeed = 13;
            Vector2 goalScale = new Vector2(1f, 1f);
            if (Mathf.Abs(controller.rb.velocity.x) < Mathf.Abs(controller.rb.velocity.y)) {
                float speed = Mathf.Abs(controller.rb.velocity.y);
                if (speed > topSpeed) {
                    speed -= topSpeed;
                    float offset = 0.3f * Mathf.Clamp01(speed / topSpeed);
                    goalScale = new Vector2(1f - offset, 1 + offset);
                }
            }
            // TODO(randomuserhi): consider skew https://www.sector12games.com/skewshear-vertex-shader/

            Vector2 scale = scaleSpring.Solve(Time.fixedDeltaTime, character.transform.localScale, goalScale);
            character.transform.localScale = scale;
            secondary.transform.localScale = scale;
        }

        private bool prevCrouchState = false;

        public void Update() {
            switch (state) {
                case AnimState.WalkFlip:
                case AnimState.RunFlip:
                    if (player.facingRight) {
                        character.flipX = true;
                        secondary.flipX = true;
                    } else {
                        character.flipX = false;
                        secondary.flipX = false;
                    }
                    break;
                default:
                    if (player.facingRight) {
                        character.flipX = false;
                        secondary.flipX = false;
                    } else {
                        character.flipX = true;
                        secondary.flipX = true;
                    }
                    break;
            }

            /*Vector2 position = controller.bottom;
            character.transform.position = position + primaryAnim.offset;
            secondary.transform.position = position + secondaryAnim.offset;
            sword.transform.position = position + swordAnim.offset;

            primaryAnim.anim = test;
            primaryAnim.AutoIncrement();
            character.sprite = primaryAnim.sprite;

            return;*/

            switch (state) {
                case AnimState.Idle:
                case AnimState.IdleChill:
                case AnimState.IdleToChill:
                case AnimState.Walk:
                case AnimState.Run:
                case AnimState.ToWalkRun:
                    if (player.isCrouching) {
                        Enter_ToCrouch();
                    } else if (prevCrouchState != player.isCrouching && !player.isCrouching) {
                        Enter_FromCrouch();
                    }
                    break;
            }
            prevCrouchState = player.isCrouching;

            switch (state) {
                case AnimState.Idle:
                case AnimState.IdleChill:
                case AnimState.IdleToChill:
                case AnimState.Land:
                case AnimState.Slide:
                case AnimState.ToSlide:
                    Vector2 input = inputSystem.movement.ReadValue<Vector2>();
                    Vector2 velocity = controller.rb.velocity;
                    if (controller.Grounded) {
                        float speed = Mathf.Abs(velocity.x);
                        if (input.x != 0 && speed > moveThresh) {
                            if (
                                state == AnimState.Idle
                            ) {
                                Enter_ToWalkRun();
                            } else {
                                if (
                                    state == AnimState.Slide ||
                                    state == AnimState.ToSlide
                                ) {
                                    Enter_Run();
                                } else {
                                    Enter_Walk();
                                }
                            }
                        }
                    }
                    break;
            }

            if (player.state != PlayerController.LocomotionState.Dash) {
                if (!controller.Grounded) {
                    if (player.state == PlayerController.LocomotionState.WallSlide) {
                        Enter_WallSlide();
                    } else {
                        Enter_Airborne();
                    }
                }
            } else {
                Enter_Dash();
            }

            Vector2 position = controller.bottom;
            character.transform.position = position + primaryAnim.offset * new Vector2(character.flipX ? -1 : 1, 1);
            secondary.transform.position = position + secondaryAnim.offset * new Vector2(secondary.flipX ? -1 : 1, 1);

            switch (state) {
                case AnimState.IdleChill: Update_IdleChill(); break;
                case AnimState.Idle: Update_Idle(); break;
                case AnimState.IdleToChill: Update_IdleToChill(); break;
                case AnimState.Airborne: Update_Airborne(); break;
                case AnimState.WallSlide: Update_WallSlide(); break;
                case AnimState.Land: Update_Land(); break;
                case AnimState.ToWalkRun: Update_ToWalkRun(); break;
                case AnimState.Walk: Update_Walk(); break;
                case AnimState.WalkFlip: Update_WalkFlip(); break;
                case AnimState.Run: Update_Run(); break;
                case AnimState.RunFlip: Update_RunFlip(); break;
                case AnimState.ToSlide: Update_ToSlide(); break;
                case AnimState.Slide: Update_Slide(); break;
                case AnimState.Dash: Update_Dash(); break;
                case AnimState.ToCrouch: Update_ToCrouch(); break;
                case AnimState.FromCrouch: Update_FromCrouch(); break;
                case AnimState.Crouch: Update_Crouch(); break;
            }
        }

        [Header("Settings")]
        public float moveThresh = 0.5f;
        public float walkThresh = 2f;

        #region Global State

        [Header("State")]
        [SerializeField] private bool isChill = true;

        [NonSerialized] public AnimDriver primaryAnim = new AnimDriver();
        private AnimDriver secondaryAnim = new AnimDriver();
        private AnimDriver swordAnim = new AnimDriver();

        [Header("Library")]

        #endregion

        #region IdleChill State

        public Anim IdleChillAnim;

        private void Enter_IdleChill() {
            state = AnimState.IdleChill;
            primaryAnim.Set(IdleChillAnim);
            isChill = true;

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
                return;
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
            isChill = false;

            character.enabled = true;
            secondary.enabled = false;

            idleTimer = 3f;
        }

        private void Update_Idle() {
            bool looped = primaryAnim.AutoIncrement();
            character.sprite = primaryAnim.sprite;
            if (idleTimer <= 0 && looped) {
                Enter_IdleToChill();
                return;
            } else {
                idleTimer -= Time.deltaTime;
            }
        }

        #endregion

        #region Airborne State

        public Anim JumpFallAnim;

        private void Enter_Airborne() {
            state = AnimState.Airborne;
            primaryAnim.Set(JumpFallAnim);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_Airborne() {
            if (player.state == PlayerController.LocomotionState.WallSlide) {
                Enter_WallSlide();
                return;
            }
            if (controller.Grounded) {
                Enter_Land();
                return;
            }

            float vy = controller.rb.velocity.y;
            if (vy > 3) {
                primaryAnim.frame = 0;
            } else if (vy > 0) {
                primaryAnim.frame = 1;
            } else if (vy > -3) {
                primaryAnim.frame = 2;
            } else {
                primaryAnim.frame = 3;
            }
            character.sprite = primaryAnim.sprite;
        }

        #endregion

        #region WallSlide State

        public Anim WallSlideAnim;

        private void Enter_WallSlide() {
            state = AnimState.WallSlide;
            primaryAnim.Set(WallSlideAnim);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_WallSlide() {
            if (player.state != PlayerController.LocomotionState.WallSlide) {
                Enter_Airborne();
                return;
            }

            primaryAnim.AutoIncrement();
            character.sprite = primaryAnim.sprite;
        }

        #endregion

        #region Land State

        public Anim LandAnim;

        private void Enter_Land() {
            state = AnimState.Land;
            primaryAnim.Set(LandAnim);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_Land() {
            if (player.isCrouching) {
                Enter_ToCrouch();
                return;
            }
            if (primaryAnim.AutoIncrement()) {
                if (isChill) Enter_IdleChill();
                else Enter_Idle();
                return;
            }
            character.sprite = primaryAnim.sprite;
        }

        #endregion

        #region ToWalkRun State

        public Anim ToWalkRunAnim;

        private void Enter_ToWalkRun() {
            state = AnimState.ToWalkRun;
            primaryAnim.Set(ToWalkRunAnim);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_ToWalkRun() {
            Vector2 input = inputSystem.movement.ReadValue<Vector2>();
            Vector2 velocity = controller.rb.velocity;
            float speed = Mathf.Abs(velocity.x);
            if (speed < moveThresh) {
                if (isChill) Enter_IdleChill();
                else Enter_Idle();
                return;
            }
            if (primaryAnim.AutoIncrement()) {
                if (speed < walkThresh) {
                    Enter_Walk();
                } else {
                    Enter_Run();
                }
                return;
            }
            character.sprite = primaryAnim.sprite;
        }

        #endregion

        #region Walk State

        public Anim WalkAnim;

        private void Enter_Walk() {
            state = AnimState.Walk;
            primaryAnim.Set(WalkAnim);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_Walk() {
            Vector2 input = inputSystem.movement.ReadValue<Vector2>();
            Vector2 velocity = controller.rb.velocity;
            float speed = Mathf.Abs(velocity.x);
            if (speed < moveThresh) {
                if (isChill) Enter_IdleChill();
                else Enter_Idle();
                return;
            }
            if (input.x != 0) {
                if (Mathf.Sign(input.x) != Mathf.Sign(velocity.x)) {
                    Enter_WalkFlip();
                    return;
                }
                primaryAnim.AutoIncrement();
                character.sprite = primaryAnim.sprite;
                if (speed > walkThresh) {
                    Enter_Run();
                    return;
                }
            } else {
                Enter_IdleChill();
                return;
            }
        }

        #endregion

        #region WalkFlip State

        public Anim WalkFlip;

        private void Enter_WalkFlip() {
            state = AnimState.WalkFlip;
            primaryAnim.Set(WalkFlip);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_WalkFlip() {
            Vector2 input = inputSystem.movement.ReadValue<Vector2>();
            Vector2 velocity = controller.rb.velocity;
            float speed = Mathf.Abs(velocity.x);
            if (speed < moveThresh) {
                Enter_Idle();
                return;
            }
            if (speed < moveThresh) {
                Enter_Idle();
                return;
            }
            if (input.x != 0) {
                if (Mathf.Sign(input.x) != Mathf.Sign(velocity.x)) {
                    Enter_WalkFlip();
                    return;
                }
            }
            if (primaryAnim.AutoIncrement()) {
                Enter_Walk();
                return;
            }
            character.sprite = primaryAnim.sprite;
        }

        #endregion

        #region Run State

        public Anim RunAnim;

        private void Enter_Run() {
            state = AnimState.Run;
            primaryAnim.Set(RunAnim);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_Run() {
            Vector2 input = inputSystem.movement.ReadValue<Vector2>();
            Vector2 velocity = controller.rb.velocity;
            float speed = Mathf.Abs(velocity.x);
            if (speed < moveThresh) {
                Enter_Idle();
                return;
            }
            if (input.x != 0) {
                if (Mathf.Sign(input.x) != Mathf.Sign(velocity.x)) {
                    Enter_RunFlip();
                    return;
                }
                primaryAnim.AutoIncrement();
                character.sprite = primaryAnim.sprite;
                if (speed < walkThresh) {
                    Enter_Walk();
                    return;
                }
            } else {
                Enter_ToSlide();
                return;
            }
        }

        #endregion

        #region RunFlip State

        public Anim RunFlip;

        private void Enter_RunFlip() {
            state = AnimState.RunFlip;
            primaryAnim.Set(RunFlip);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_RunFlip() {
            Vector2 input = inputSystem.movement.ReadValue<Vector2>();
            Vector2 velocity = controller.rb.velocity;
            float speed = Mathf.Abs(velocity.x);
            if (speed < moveThresh) {
                Enter_Idle();
                return;
            }
            if (input.x != 0) {
                if (Mathf.Sign(input.x) != Mathf.Sign(velocity.x)) {
                    Enter_RunFlip();
                    return;
                }
            }
            if (primaryAnim.AutoIncrement()) {
                Enter_Run();
                return;
            }
            character.sprite = primaryAnim.sprite;
        }

        #endregion

        #region ToSlide State

        private void Enter_ToSlide() {
            state = AnimState.ToSlide;
            primaryAnim.Set(ToWalkRunAnim, ToWalkRunAnim.sprites.Length);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_ToSlide() {
            Vector2 input = inputSystem.movement.ReadValue<Vector2>();
            Vector2 velocity = controller.rb.velocity;
            float speed = Mathf.Abs(velocity.x);
            if (speed < moveThresh) {
                Enter_Idle();
                return;
            }
            if (input.x != 0) {
                if (Mathf.Sign(input.x) != Mathf.Sign(velocity.x)) {
                    Enter_RunFlip();
                    return;
                }
            }
            if (primaryAnim.AutoDecrement()) {
                Enter_Slide();
                return;
            }
            character.sprite = primaryAnim.sprite;
        }

        #endregion

        #region Slide State

        private void Enter_Slide() {
            state = AnimState.Slide;
            primaryAnim.Set(IdleAnim);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_Slide() {
            Vector2 input = inputSystem.movement.ReadValue<Vector2>();
            Vector2 velocity = controller.rb.velocity;
            float speed = Mathf.Abs(velocity.x);
            if (speed < moveThresh) {
                Enter_Idle();
                return;
            }
            if (input.x != 0) {
                if (Mathf.Sign(input.x) != Mathf.Sign(velocity.x)) {
                    Enter_RunFlip();
                    return;
                }
            }
            character.sprite = primaryAnim.sprite;
        }

        #endregion

        #region Dash State

        private void Enter_Dash() {
            state = AnimState.Dash;

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_Dash() {
            if (player.state != PlayerController.LocomotionState.Dash) {
                if (isChill) Enter_IdleChill();
                else Enter_Idle();
                return;
            }

            if (player.isCrouching) {
                primaryAnim.Set(ToCrouchAnim, ToCrouchAnim.sprites.Length - 1);
                character.sprite = primaryAnim.sprite;
            } else {
                primaryAnim.Set(CrouchTransitionAnim);
                character.sprite = primaryAnim.sprite;
            }
        }

        #endregion

        #region Crouch State

        private void Enter_Crouch() {
            state = AnimState.Crouch;
            primaryAnim.Set(ToCrouchAnim);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_Crouch() {
            if (!player.isCrouching) {
                Enter_FromCrouch();
                return;
            }
            character.sprite = primaryAnim.anim.sprites[primaryAnim.Length - 1].sprite;
        }

        public Anim ToCrouchAnim;

        private void Enter_ToCrouch() {
            state = AnimState.ToCrouch;
            primaryAnim.Set(ToCrouchAnim);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_ToCrouch() {
            if (!player.isCrouching) {
                Enter_FromCrouch();
                return;
            }
            if (primaryAnim.AutoIncrement()) {
                Enter_Crouch();
                return;
            }
            character.sprite = primaryAnim.sprite;
        }

        public Anim CrouchTransitionAnim;

        private void Enter_FromCrouch() {
            state = AnimState.FromCrouch;
            primaryAnim.Set(CrouchTransitionAnim);

            character.enabled = true;
            secondary.enabled = false;
        }

        private void Update_FromCrouch() {
            if (primaryAnim.AutoIncrement()) {
                if (isChill) Enter_IdleChill();
                else Enter_Idle();
                return;
            }
            character.sprite = primaryAnim.sprite;
        }

        #endregion
    }
}
