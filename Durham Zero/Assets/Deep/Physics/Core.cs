using System.Collections.Generic;
using UnityEngine;

namespace Deep.Physics {
    public class Core : MonoBehaviour {
        private static Core instance;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize() {
            Core exists = FindAnyObjectByType<Core>();
            if (exists != null) return;

            GameObject go = new GameObject();
            go.name = "Core";
            go.AddComponent<Core>();
        }

        private static List<DeepMonoBehaviour> behaviours = new List<DeepMonoBehaviour>();

        public static void Register(DeepMonoBehaviour behaviour) {
            behaviours.Add(behaviour);
        }

        public static void Unregister(DeepMonoBehaviour behaviour) {
            behaviours.Remove(behaviour);
        }

        private void Start() {
            if (instance != null) {
                Debug.LogError("Cannot have more than one Core script.");
                return;
            }
            instance = this;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }

        private void FixedUpdate() {
            foreach (DeepMonoBehaviour behaviour in behaviours) {
                if (!behaviour.enabled) continue;
                behaviour.PreFixedUpdate();
            }

            Physics2D.Simulate(Time.fixedDeltaTime);

            foreach (DeepMonoBehaviour behaviour in behaviours) {
                if (!behaviour.enabled) continue;
                behaviour.LateFixedUpdate();
            }
        }
    }
}
