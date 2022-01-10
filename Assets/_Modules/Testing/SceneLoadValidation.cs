#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System.Collections;
using BeauUtil;
using UnityEngine;

namespace Aqua.Testing {
    static public class SceneLoadValidation {
        #if DEVELOPMENT

        static public IEnumerator LoadAllScenes() {
            foreach(var scene in SceneHelper.FindScenes(SceneCategories.Build)) {
                if (scene.Name.Contains("-Visuals") || scene.Name.Contains("Layer")) {
                    continue;
                }

                yield return LoadScene(scene);
            }
        }

        static private IEnumerator LoadScene(SceneBinding scene) {
            Services.UI.HideAll();
            Services.Script.KillAllThreads();
            Services.Audio.StopAll();
            yield return Services.State.LoadScene(scene, null, null, SceneLoadFlags.NoLoadingScreen);
        }

        #endif // DEVELOPMENT
    }
}