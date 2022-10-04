using UnityEngine;
using UnityEngine.SceneManagement;
using BeauUtil;
using System.Collections.Generic;
using BeauUtil.Debugger;
using ScriptableBake;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
#endif // UNITY_EDITOR

namespace Aqua
{
    public sealed class GameplaySceneOnly : MonoBehaviour, IBaked {

        #if UNITY_EDITOR

        public int Order { get { return -100; } }

        public bool Bake(BakeFlags flags, BakeContext context) {
            bool isGameplayScene = IsGameplayScene(SceneHelper.ActiveScene());
            if (!isGameplayScene) {
                ScriptableBake.Bake.Destroy(gameObject);
                return true;
            } else {
                ScriptableBake.Bake.Destroy(this);
                return false;
            }
        }

        static private bool IsGameplayScene(SceneBinding binding) {
            if (binding.BuildIndex < GameConsts.GameSceneIndexStart) {
                return false;
            }

            if (binding.Name.Contains("Title")) {
                return false;
            }

            return true;
        }

        #endif // UNITY_EDITOR
    }
}