using System.Collections;
using BeauRoutine;
using UnityEngine;
using Aqua.Scripting;

namespace Aqua
{
    static public class StateUtil
    {
        private const SceneLoadFlags LoadFlags = SceneLoadFlags.NoLoadingScreen | SceneLoadFlags.DoNotDispatchPreUnload;

        private const float FadeDuration = 0.25f;
        private const float PauseDuration = 0.15f;
        private const string DefaultBackScene = "Ship";

        // TODO: Tie some of this into StateMgr itself??

        static public IEnumerator LoadSceneWithFader(string inSceneName, object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            BeforeLoad();
            return Services.UI.ScreenFaders.FadeTransition(Color.black, FadeDuration, PauseDuration,
                () => Sequence.Create(Services.State.LoadScene(inSceneName, inContext, LoadFlags | inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadSceneWithWipe(string inSceneName, object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            BeforeLoad();
            return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(Services.State.LoadScene(inSceneName, inContext, LoadFlags | inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadPreviousSceneWithFader(object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            BeforeLoad();
            return Services.UI.ScreenFaders.FadeTransition(Color.black, FadeDuration, PauseDuration,
                () => Sequence.Create(Services.State.LoadPreviousScene(DefaultBackScene, inContext, LoadFlags | inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadPreviousSceneWithWipe(object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            BeforeLoad();
            return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(Services.State.LoadPreviousScene(DefaultBackScene, inContext, LoadFlags | inFlags)).Then(AfterLoad)
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