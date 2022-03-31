using System.Collections;
using BeauRoutine;
using UnityEngine;
using Aqua.Scripting;
using BeauUtil;

namespace Aqua
{
    static public class StateUtil
    {
        private const SceneLoadFlags LoadFlags = SceneLoadFlags.DoNotDispatchPreUnload;

        private const float FadeDuration = 0.25f;
        private const float PauseDuration = 0.15f;
        private const string DefaultBackScene = "Ship";

        static private SceneLoadFlags s_LastFlags;

        static public IEnumerator LoadSceneWithFader(string inSceneName, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            inFlags |= LoadFlags;

            BeforeLoad(inFlags);

            return Services.UI.ScreenFaders.FadeTransition(Color.black, FadeDuration, PauseDuration,
                () => Sequence.Create(Services.State.LoadScene(inSceneName, inEntrance, inContext, inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadSceneWithWipe(string inSceneName, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            inFlags |= LoadFlags;

            BeforeLoad(inFlags);

            return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(Services.State.LoadScene(inSceneName, inEntrance, inContext, inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadMapWithWipe(StringHash32 inMapId, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            inFlags |= LoadFlags;

            BeforeLoad(inFlags);
            StringHash32 currentMapId = MapDB.LookupCurrentMap();
            if (!currentMapId.IsEmpty)
            {
                if (inEntrance.IsEmpty && (inFlags & SceneLoadFlags.DoNotOverrideEntrance) == 0)
                    inEntrance = currentMapId;
            }
            
            return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(Services.State.LoadSceneFromMap(inMapId, inEntrance, inContext, inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadPreviousSceneWithFader(StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            inFlags |= LoadFlags;

            BeforeLoad(inFlags);
            return Services.UI.ScreenFaders.FadeTransition(Color.black, FadeDuration, PauseDuration,
                () => Sequence.Create(Services.State.LoadPreviousScene(DefaultBackScene, inEntrance, inContext, inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadPreviousSceneWithWipe(StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            string sceneToLoad = null;
            StringHash32 currentMapId = MapDB.LookupCurrentMap();
            if (!currentMapId.IsEmpty)
            {
                if (inEntrance.IsEmpty && (inFlags & SceneLoadFlags.DoNotOverrideEntrance) == 0)
                    inEntrance = currentMapId;
                sceneToLoad = Assets.Map(currentMapId).Parent()?.SceneName();
            }

            inFlags |= LoadFlags;

            BeforeLoad(inFlags);
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(Services.State.LoadScene(sceneToLoad, inEntrance, inContext, inFlags)).Then(AfterLoad)
            );
            }
            return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(Services.State.LoadPreviousScene(DefaultBackScene, inEntrance, inContext, inFlags)).Then(AfterLoad)
            );
        }

        static private void BeforeLoad(SceneLoadFlags inFlags)
        {
            s_LastFlags = inFlags;
            if ((s_LastFlags & SceneLoadFlags.Cutscene) != 0)
            {
                Services.UI.ShowLetterbox();
            }
            Services.Input.PauseAll();
            Services.Audio.FadeOut(FadeDuration);
            Services.Script.KillLowPriorityThreads();
            Services.Events.Dispatch(GameEvents.SceneWillUnload);
        }

        static private void AfterLoad()
        {
            if ((s_LastFlags & SceneLoadFlags.Cutscene) != 0)
            {
                Services.UI.HideLetterbox();
            }
            Services.Audio.FadeIn(FadeDuration);
            Services.Input.ResumeAll();
        }
    }
}