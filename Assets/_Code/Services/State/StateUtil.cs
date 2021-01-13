using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauUtil.Debugger;
using BeauUtil.Variants;

namespace Aqua
{
    static public class StateUtil
    {
        private const float FadeDuration = 0.25f;
        private const float PauseDuration = 0.15f;
        private const string DefaultBackScene = "Ship";

        // TODO: Tie some of this into StateMgr itself??

        static public IEnumerator LoadSceneWithFader(string inSceneName, object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            BeforeLoad();
            return Services.UI.ScreenFaders.FadeTransition(Color.black, FadeDuration, PauseDuration,
                () => Sequence.Create(Services.State.LoadScene(inSceneName, inContext, SceneLoadFlags.NoLoadingScreen | inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadSceneWithWipe(string inSceneName, object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            BeforeLoad();
            return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(Services.State.LoadScene(inSceneName, inContext, SceneLoadFlags.NoLoadingScreen | inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadPreviousSceneWithFader(object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            BeforeLoad();
            return Services.UI.ScreenFaders.FadeTransition(Color.black, FadeDuration, PauseDuration,
                () => Sequence.Create(Services.State.LoadPreviousScene(DefaultBackScene, inContext, SceneLoadFlags.NoLoadingScreen | inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadPreviousSceneWithWipe(object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            BeforeLoad();
            return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(Services.State.LoadPreviousScene(DefaultBackScene, inContext, SceneLoadFlags.NoLoadingScreen | inFlags)).Then(AfterLoad)
            );
        }

        static private void BeforeLoad()
        {
            Services.Input.PauseAll();
            Services.Audio.FadeOut(FadeDuration);
        }

        static private void AfterLoad()
        {
            Services.Audio.FadeIn(FadeDuration);
            Services.Input.ResumeAll();
        }
    }
}