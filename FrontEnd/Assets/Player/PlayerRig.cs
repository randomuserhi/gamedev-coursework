using Deep.Anim;
using UnityEngine;

namespace Player {
    public class PlayerRig : MonoBehaviour {
        public PlayerMovement player;

        public Anim idle;
        public Anim idleChill;
        public Anim airborne;
        public Anim walk;
        public Anim run;
        public Anim stance;
        public Anim strike;
        public Anim recovery;

        private AnimFrame current;
        private SpriteRenderer spriteRenderer;
        private float timer = 0;

        private float timeSpentIdle = 0;

        private enum State {
            idle,
            idleChill,
            walking,
            running,
            airborne,
            stance,
            strike,
            recovery,
        }
        private State state = State.idleChill;

        // Start is called before the first frame update
        void Start() {
            current = new AnimFrame(idleChill);
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void ChangeAnim(Anim anim, int index = 0) {
            if (current.anim != anim) {
                current.anim = anim;
                current.index = index;
                timer = 0;
            }
        }

        // Update is called once per frame
        void Update() {
            spriteRenderer.flipX = !player.facingRight;

            transform.position = player.transform.position + current.anim.offset;
            spriteRenderer.sprite = current.anim.sprites[current.index];
            timer += Time.deltaTime;
            if (timer > 1f / current.anim.frameRate) {
                timer = 0;
                current.index = (current.index + 1) % current.anim.sprites.Length;
            }

            if (player.state == PlayerMovement.State.stance) {
                ChangeAnim(stance);

                state = State.stance;
            } else if (player.state == PlayerMovement.State.attack) {
                ChangeAnim(strike);

                state = State.strike;
            } else if (player.state == PlayerMovement.State.recovery) {
                ChangeAnim(recovery);

                state = State.recovery;
            } else if (player.state == PlayerMovement.State.dash) {
                ChangeAnim(idle);

                state = State.recovery;
            } else if (player.state == PlayerMovement.State.movement) {
                if (!player.grounded) {
                    ChangeAnim(airborne);

                    state = State.airborne;
                } else {
                    if (player.input.x != 0) {
                        if (Mathf.Sign(player.input.x) != Mathf.Sign(player.rb.velocity.x)) {
                            state = State.idle;
                        } else {
                            if (Mathf.Abs(player.rb.velocity.x) > 10f) {
                                state = State.running;
                            } else if (Mathf.Abs(player.rb.velocity.x) > 0f) {
                                state = State.walking;
                            } else {
                                state = State.idle;
                            }
                        }
                    } else {
                        if (state == State.walking) {
                            state = State.idleChill;
                        } else {
                            state = State.idle;
                        }
                    }
                }

                if (state == State.running) {
                    ChangeAnim(run);
                } else if (state == State.walking) {
                    ChangeAnim(walk);
                }

                if (state == State.idle) {
                    ChangeAnim(idle);

                    if (timeSpentIdle < 5) {
                        timeSpentIdle += Time.deltaTime;
                    } else {
                        state = State.idleChill;
                    }
                } else {
                    timeSpentIdle = 0;
                }

                if (state == State.idleChill) {
                    ChangeAnim(idleChill);
                }
            }
        }
    }
}
