using System.Collections;
using BeauRoutine;
using UnityEngine;
using Aqua.Scripting;
using BeauUtil;

namespace Aqua
{
    static public class StateUtil
    {
        private const SceneLoadFlags LoadFlags = SceneLoadFlags.NoLoadingScreen | SceneLoadFlags.DoNotDispatchPreUnload;

        private const float FadeDuration = 0.25f;
        private const float PauseDuration = 0.15f;
        private const string DefaultBackScene = "Ship";

        static public IEnumerator LoadSceneWithFader(string inSceneName, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            BeforeLoad();
            return Services.UI.ScreenFaders.FadeTransition(Color.black, FadeDuration, PauseDuration,
                () => Sequence.Create(Services.State.LoadScene(inSceneName, inEntrance, inContext, LoadFlags | inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadSceneWithWipe(string inSceneName, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            BeforeLoad();
            return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(Services.State.LoadScene(inSceneName, inEntrance, inContext, LoadFlags | inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadMapWithWipe(StringHash32 inMapId, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            BeforeLoad();
            StringHash32 currentMapId = MapDB.LookupCurrentMap();
            if (!currentMapId.IsEmpty)
            {
                if (inEntrance.IsEmpty)
                    inEntrance = currentMapId;
            }
            
            return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(Services.State.LoadSceneFromMap(inMapId, inEntrance, inContext, LoadFlags | inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadPreviousSceneWithFader(StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            BeforeLoad();
            return Services.UI.ScreenFaders.FadeTransition(Color.black, FadeDuration, PauseDuration,
                () => Sequence.Create(Services.State.LoadPreviousScene(DefaultBackScene, inEntrance, inContext, LoadFlags | inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadPreviousSceneWithWipe(StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            string sceneToLoad = null;
            StringHash32 currentMapId = MapDB.LookupCurrentMap();
            if (!currentMapId.IsEmpty)
            {
                if (inEntrance.IsEmpty)
                    inEntrance = currentMapId;
                sceneToLoad = Services.Assets.Map.Get(currentMapId).Parent()?.SceneName();
            }

            BeforeLoad();
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(Services.State.LoadScene(sceneToLoad, inEntrance, inContext, LoadFlags | inFlags)).Then(AfterLoad)
            );
            }
            return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(Services.State.LoadPreviousScene(DefaultBackScene, inEntrance, inContext, LoadFlags | inFlags)).Then(AfterLoad)
            );
        }

        static private void BeforeLoad()
        {
            Services.Input.PauseAll();
            Services.Audio.FadeOut(FadeDuration);
            Services.Script.KillLowPriorityThreads();
            Services.Events.Dispatch(GameEvents.SceneWillUnload);
        }

        static private void AfterLoad()
        {
            Services.Audio.FadeIn(FadeDuration);
            Services.Input.ResumeAll();
        }
    }
}