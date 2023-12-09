using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Deep.Anim {
    [CreateAssetMenu(fileName = "Animation", menuName = "Deep/Animation")]
    public class Anim : ScriptableObject {
        public int frameRate = 10;
        public Vector3 offset = Vector3.zero;
        public Sprite[] sprites = new Sprite[0];

        public static Anim Create(Sprite[] frames) {
            Anim instance = CreateInstance<Anim>();
            instance.sprites = frames;
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
