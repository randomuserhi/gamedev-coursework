using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Deep.Anim {
    public class AnimDriver {
        private Anim _anim = null;
        public Anim anim {
            get { return _anim; }
            set { _anim = value; }
        }
        public int Length {
            get => _anim.sprites.Length;
        }

        public void Set(Anim anim, int frame = -1) {
            this.anim = anim;
            this.frame = frame;
        }

        public Vector2 offset {
            get => anim != null ? anim.offset : Vector2.zero;
        }

        public Sprite sprite {
            get { return anim != null ? anim.sprites[Mathf.Abs(frame % _anim.sprites.Length)].sprite : null; }
        }

        public FrameData current {
            get { return anim != null ? anim.sprites[Mathf.Abs(frame % _anim.sprites.Length)] : new FrameData(); }
        }

        public static implicit operator AnimDriver(Anim anim) {
            AnimDriver driver = new AnimDriver();
            driver.anim = anim;
            return driver;
        }

        private int _frame = 0;
        public int frame {
            get { return _frame; }
            set { _frame = value; frameTimer = 0; }
        }
        private float frameTimer = 0;
        public bool AutoIncrement() {
            if (_anim == null) return false;
            bool reset = false;
            bool start = _frame == -1;
            if (frameTimer <= 0) {
                _frame = (_frame + 1) % _anim.sprites.Length;
                if (_frame == 0 && !start) {
                    reset = true;
                }
                frameTimer = 1f / _anim.frameRate;
            } else {
                frameTimer -= Time.deltaTime;
            }
            return reset;
        }

        public bool AutoDecrement() {
            if (_anim == null) return false;
            int last = _anim.sprites.Length - 1;
            bool reset = false;
            bool start = _frame == _anim.sprites.Length;
            if (frameTimer <= 0) {
                _frame = _frame - 1;
                if (_frame == -1) _frame = last;
                if (_frame == last && !start) {
                    reset = true;
                }
                frameTimer = 1f / _anim.frameRate;
            } else {
                frameTimer -= Time.deltaTime;
            }
            return reset;
        }
    }

    [Serializable]
    public struct FrameData {
        [SerializeField] public Sprite sprite;
        [SerializeField] public Vector2 Scalf;
    }

    [CreateAssetMenu(fileName = "Animation", menuName = "Deep/Animation")]
    public class Anim : ScriptableObject {
        public int frameRate = 10;
        public Vector2 offset = Vector2.zero;
        [SerializeField] public FrameData[] sprites = new FrameData[0];

        public static Anim Create(Sprite[] frames) {
            Anim instance = CreateInstance<Anim>();
            instance.sprites = new FrameData[frames.Length];
            for (int i = 0; i < frames.Length; ++i) {
                FrameData f = new FrameData();
                f.sprite = frames[i];
                instance.sprites[i] = f;
            }
            return instance;
        }
    }

    [CustomEditor(typeof(Anim))]
    public class AnimEditor : Editor {
        [MenuItem("Assets/Create/Deep/Animation (From Textures)", false, 400)]
        private static void CreateFromTextures() {
            Regex trailingNumbersRegex = new Regex(@"(\d+$)");

            List<Sprite> sprites = new List<Sprite>();
            Texture2D[] textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
            foreach (Texture2D texture in textures) {
                string path = AssetDatabase.GetAssetPath(texture);
                sprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>());
            }

            Sprite[] frames = sprites
                .OrderBy(
                    sprite => {
                        var match = trailingNumbersRegex.Match(sprite.name);
                        return match.Success ? int.Parse(match.Groups[0].Captures[0].ToString()) : 0;
                    }
                )
                .ToArray();

            Anim asset = Anim.Create(frames);
            string baseName = trailingNumbersRegex.Replace(textures[0].name, "");
            asset.name = baseName + "_animation";

            string assetPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(textures[0]));
            AssetDatabase.CreateAsset(asset, Path.Combine(assetPath ?? Application.dataPath, asset.name + ".asset"));
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Assets/Create/Reanimator/Simple Animation (From Textures)", true, 400)]
        private static bool CreateFromTexturesValidation() {
            return Selection.GetFiltered<Texture2D>(SelectionMode.Assets).Length > 0;
        }
    }
}
