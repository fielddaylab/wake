using System.Collections;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aqua
{
    public class FastBootController : MonoBehaviour, ISceneLoadHandler
    {
        public AudioClip BootAudio;

        public void OnSceneLoad(SceneBinding inScene, object inContext)
        {
            int buildIdx = SceneHelper.ActiveScene().BuildIndex + 1;
            SceneBinding nextScene = SceneHelper.FindSceneByIndex(buildIdx);
            Async.Invoke(() => {
                Services.State.LoadScene(nextScene, null, null);
                Services.State.OnSceneLoadReady(SceneLoadReady);
            });
        }

        static private IEnumerator SceneLoadReady() {
            var fader = Services.UI.WorldFaders.AllocFader();
            Services.State.OnLoad(() => {
                fader.Dispose();
            });
            return fader.Object.Show(Color.black, 0.3f);
        }
    }
}