using UnityEngine;

namespace Deep.Physics {
    public abstract class DeepMonoBehaviour : MonoBehaviour {
        protected virtual void Start() {
            Core.Register(this);
        }

        protected virtual void OnDestroy() {
            Core.Unregister(this);
        }

        public virtual void PreFixedUpdate() {

        }

        public virtual void LateFixedUpdate() {

        }
    }
}
