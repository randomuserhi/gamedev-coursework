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
        [NonSerialized] public SpriteRenderer scalf;

        private CharacterController2D controller;
        private PlayerInputSystem inputSystem;
        private PlayerController player;

        [Header("Colors")]
        [SerializeField] private float colorTransitionSpeed = 20f;
        public Color characterColor = Color.black;
        public Color scalfColor = Color.red;
        public Color scalfDashColor = Color.blue;
        public Color scalfDashReadyColor = Color.white;

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
            character.transform.localPosition = new Vector3(0, 0, 20);
            character.enabled = true;
            character.color = characterColor;

            GameObject _secondary = new GameObject();
            scalf = _secondary.AddComponent<SpriteRenderer>();
            scalf.transform.parent = transform;
            scalf.transform.localPosition = new Vector3(0, 0, 10);
            scalf.enabled = true;

            Enter_Idle();
        }

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
            scalf.transform.localScale = scale;
        }

        private bool prevCrouchState = false;

        public Anim DeathAnim;
        private bool wasDead = false;

        public void Update() {
            float dt = Time.deltaTime;

            if (player.state == PlayerController.LocomotionState.Dead) {
                if (!wasDead) {
                    wasDead = true;
                    scalf.enabled = false;
                    character.enabled = true;
                    primaryAnim.Set(DeathAnim);
                }

                if (primaryAnim.AutoIncrement()) {
                    player.Alive();

                    AnimatedEffect e = EffectLibrary.SpawnEffect(0, player.respawnPoint);
                    e.color = character.color;
                }
                character.sprite = primaryAnim.sprite;

                return;
            } else {
                wasDead = false;
            }

            switch (state) {
                case AnimState.WalkFlip:
                case AnimState.RunFlip:
                    if (player.facingRight) {
                        character.flipX = true;
                        scalf.flipX = true;
                    } else {
                        character.flipX = false;
                        scalf.flipX = false;
                    }
                    break;
                default:
                    if (player.facingRight) {
                        character.flipX = false;
                        scalf.flipX = false;
                    } else {
                        character.flipX = true;
                        scalf.flipX = true;
                    }
                    break;
            }

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

            // Positioning sprites
            Vector2 position = controller.bottom;
            Vector2 characterPos = position + primaryAnim.offset * new Vector2(character.flipX ? -1 : 1, 1);
            character.transform.position = new Vector3(characterPos.x, characterPos.y, character.transform.position.z);
            Vector2 scalfPos = position + scalfAnim.offset * new Vector2(scalf.flipX ? -1 : 1, 1);
            scalf.transform.position = new Vector3(scalfPos.x, scalfPos.y, scalf.transform.position.z);

            // colors
            bool canDash = player.canDash <= 0;
            if (canDash && prevDashReady != canDash) {
                scalfDashReadyTimer = scalfDashReadyTime;
            }
            prevDashReady = canDash;

            Color goal = scalfColor;
            if (scalfDashReadyTimer > 0) {
                goal = scalfDashReadyColor;
                scalfDashReadyTimer -= dt;
            } else {
                switch (state) {
                    case AnimState.Dash:
                        goal = scalfDashColor;
                        break;
                }
            }
            if (colorTransitionSpeed != 0) {
                scalf.color = Color.Lerp(scalf.color, goal, colorTransitionSpeed * dt);
            } else {
                scalf.color = goal;
            }

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

        public float scalfDashReadyTime = 0.2f;

        #region Global State

        [Header("State")]
        [SerializeField] private bool isChill = true;
        [SerializeField] private bool prevDashReady = true;
        [SerializeField] private float scalfDashReadyTimer = 0;

        [NonSerialized] public AnimDriver primaryAnim = new AnimDriver();
        private AnimDriver scalfAnim = new AnimDriver();
        private AnimDriver swordAnim = new AnimDriver();

        [Header("Library")]

        #endregion

        #region IdleChill State

        public Anim IdleChillAnim;
        public Anim ScalfIdleChillAnim;

        private void Enter_IdleChill() {
            state = AnimState.IdleChill;
            primaryAnim.Set(IdleChillAnim);
            scalfAnim.Set(ScalfIdleChillAnim);
            isChill = true;

            character.enabled = true;
            scalf.enabled = true;
        }

        private void Update_IdleChill() {
            primaryAnim.AutoIncrement();
            character.sprite = primaryAnim.sprite;
            scalfAnim.AutoIncrement();
            scalf.sprite = scalfAnim.sprite;
        }

        #endregion

        #region IdleToChill

        public Anim IdleToChillAnim;
        public Anim ScalfIdleToChillAnim;

        private void Enter_IdleToChill() {
            state = AnimState.IdleToChill;
            primaryAnim.Set(IdleToChillAnim);
            scalfAnim.Set(ScalfIdleToChillAnim);

            character.enabled = true;
            scalf.enabled = true;
        }

        private void Update_IdleToChill() {
            if (primaryAnim.AutoIncrement()) {
                Enter_IdleChill();
                return;
            }
            character.sprite = primaryAnim.sprite;
            scalfAnim.AutoIncrement();
            scalf.sprite = scalfAnim.sprite;
        }

        #endregion

        #region Idle State

        private float idleTimer = 0;
        public Anim IdleAnim;
        public Anim ScalfIdleAnim;

        private void Enter_Idle() {
            state = AnimState.Idle;
            primaryAnim.Set(IdleAnim);
            scalfAnim.Set(ScalfIdleAnim);
            isChill = false;

            character.enabled = true;
            scalf.enabled = true;

            idleTimer = 3f;
        }

        private void Update_Idle() {
            bool looped = primaryAnim.AutoIncrement();
            character.sprite = primaryAnim.sprite;
            scalfAnim.AutoIncrement();
            scalf.sprite = scalfAnim.sprite;
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
        public Anim ScalfJumpFallAnim;

        private void Enter_Airborne() {
            state = AnimState.Airborne;
            primaryAnim.Set(JumpFallAnim);
            scalfAnim.Set(ScalfJumpFallAnim);

            character.enabled = true;
            scalf.enabled = true;
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
                scalfAnim.frame = 0;
            } else if (vy > 0) {
                primaryAnim.frame = 1;
                scalfAnim.frame = 1;
            } else if (vy > -3) {
                primaryAnim.frame = 2;
                scalfAnim.frame = 2;
            } else {
                primaryAnim.frame = 3;
                scalfAnim.frame = 3;
            }
            character.sprite = primaryAnim.sprite;
            scalf.sprite = scalfAnim.sprite;
        }

        #endregion

        #region WallSlide State

        public Anim WallSlideAnim;
        public Anim ScalfWallSlideAnim;

        private void Enter_WallSlide() {
            state = AnimState.WallSlide;
            primaryAnim.Set(WallSlideAnim);
            scalfAnim.Set(ScalfWallSlideAnim);

            character.enabled = true;
            scalf.enabled = true;
        }

        private void Update_WallSlide() {
            if (player.state != PlayerController.LocomotionState.WallSlide) {
                Enter_Airborne();
                return;
            }

            primaryAnim.AutoIncrement();
            character.sprite = primaryAnim.sprite;
            scalfAnim.AutoIncrement();
            scalf.sprite = scalfAnim.sprite;
        }

        #endregion

        #region Land State

        public Anim LandAnim;
        public Anim ScalfLandAnim;

        private void Enter_Land() {
            state = AnimState.Land;
            primaryAnim.Set(LandAnim);
            scalfAnim.Set(ScalfLandAnim);

            character.enabled = true;
            scalf.enabled = true;
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
            scalfAnim.AutoIncrement();
            scalf.sprite = scalfAnim.sprite;
        }

        #endregion

        #region ToWalkRun State

        public Anim ToWalkRunAnim;
        public Anim ScalfToWalkRunAnim;

        private void Enter_ToWalkRun() {
            state = AnimState.ToWalkRun;
            primaryAnim.Set(ToWalkRunAnim);
            scalfAnim.Set(ScalfToWalkRunAnim);

            character.enabled = true;
            scalf.enabled = true;
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
            scalfAnim.AutoIncrement();
            scalf.sprite = scalfAnim.sprite;
        }

        #endregion

        #region Walk State

        public Anim WalkAnim;
        public Anim ScalfWalkAnim;

        private void Enter_Walk() {
            state = AnimState.Walk;
            primaryAnim.Set(WalkAnim);
            scalfAnim.Set(ScalfWalkAnim);

            character.enabled = true;
            scalf.enabled = true;
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
                scalfAnim.AutoIncrement();
                scalf.sprite = scalfAnim.sprite;
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
        public Anim ScalfWalkFlip;

        private void Enter_WalkFlip() {
            state = AnimState.WalkFlip;
            primaryAnim.Set(WalkFlip);
            scalfAnim.Set(ScalfWalkFlip);

            character.enabled = true;
            scalf.enabled = true;
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
            scalfAnim.AutoIncrement();
            scalf.sprite = scalfAnim.sprite;
        }

        #endregion

        #region Run State

        public Anim RunAnim;
        public Anim ScalfRunAnim;

        private void Enter_Run() {
            state = AnimState.Run;
            primaryAnim.Set(RunAnim);
            scalfAnim.Set(ScalfRunAnim);

            character.enabled = true;
            scalf.enabled = true;
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
                scalfAnim.AutoIncrement();
                scalf.sprite = scalfAnim.sprite;
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
        public Anim ScalfRunFlip;

        private void Enter_RunFlip() {
            state = AnimState.RunFlip;
            primaryAnim.Set(RunFlip);
            scalfAnim.Set(ScalfRunFlip);

            character.enabled = true;
            scalf.enabled = true;
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
            scalfAnim.AutoIncrement();
            scalf.sprite = scalfAnim.sprite;
        }

        #endregion

        #region ToSlide State

        private void Enter_ToSlide() {
            state = AnimState.ToSlide;
            primaryAnim.Set(ToWalkRunAnim, ToWalkRunAnim.sprites.Length);
            scalfAnim.Set(ScalfToWalkRunAnim, ScalfToWalkRunAnim.sprites.Length);

            character.enabled = true;
            scalf.enabled = true;
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
            scalfAnim.AutoDecrement();
            scalf.sprite = scalfAnim.sprite;
        }

        #endregion

        #region Slide State

        private void Enter_Slide() {
            state = AnimState.Slide;
            primaryAnim.Set(IdleAnim);
            scalfAnim.Set(ScalfIdleAnim);

            character.enabled = true;
            scalf.enabled = true;
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
            scalf.sprite = scalfAnim.sprite;
        }

        #endregion

        #region Dash State

        private void Enter_Dash() {
            state = AnimState.Dash;

            character.enabled = true;
            scalf.enabled = true;
        }

        private void Update_Dash() {
            if (player.state != PlayerController.LocomotionState.Dash) {
                Enter_Crouch();
                return;
            }

            if (player.isCrouching) {
                primaryAnim.Set(ToCrouchAnim, ToCrouchAnim.sprites.Length - 1);
                scalfAnim.Set(ScalfToCrouchAnim, ScalfToCrouchAnim.sprites.Length - 1);
            } else {
                primaryAnim.Set(CrouchTransitionAnim);
                scalfAnim.Set(ScalfCrouchTransitionAnim);
            }
            character.sprite = primaryAnim.sprite;
            scalf.sprite = scalfAnim.sprite;
        }

        #endregion

        #region Crouch State

        private void Enter_Crouch() {
            state = AnimState.Crouch;
            primaryAnim.Set(ToCrouchAnim);
            scalfAnim.Set(ScalfToCrouchAnim);

            character.enabled = true;
            scalf.enabled = true;
        }

        private void Update_Crouch() {
            if (!player.isCrouching) {
                Enter_FromCrouch();
                return;
            }
            character.sprite = primaryAnim.anim.sprites[primaryAnim.Length - 1].sprite;
            scalf.sprite = scalfAnim.anim.sprites[scalfAnim.Length - 1].sprite;
        }

        public Anim ToCrouchAnim;
        public Anim ScalfToCrouchAnim;

        private void Enter_ToCrouch() {
            state = AnimState.ToCrouch;
            primaryAnim.Set(ToCrouchAnim);
            scalfAnim.Set(ScalfToCrouchAnim);

            character.enabled = true;
            scalf.enabled = true;
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
            scalfAnim.AutoIncrement();
            scalf.sprite = scalfAnim.sprite;
        }

        public Anim CrouchTransitionAnim;
        public Anim ScalfCrouchTransitionAnim;

        private void Enter_FromCrouch() {
            state = AnimState.FromCrouch;
            primaryAnim.Set(CrouchTransitionAnim);
            scalfAnim.Set(ScalfCrouchTransitionAnim);

            character.enabled = true;
            scalf.enabled = true;
        }

        private void Update_FromCrouch() {
            if (primaryAnim.AutoIncrement()) {
                if (isChill) Enter_IdleChill();
                else Enter_Idle();
                return;
            }
            character.sprite = primaryAnim.sprite;
            scalfAnim.AutoIncrement();
            scalf.sprite = scalfAnim.sprite;
        }

        #endregion
    }
}
