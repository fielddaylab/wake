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

        static public IEnumerator LoadSceneWithFader(string inSceneName, object inContext = null)
        {
            Services.Audio.FadeOut(FadeDuration);
            return Services.UI.WorldFaders.FadeTransition(Color.white, FadeDuration, 0,
                () => Sequence.Create(Services.State.LoadScene(inSceneName, inContext, SceneLoadFlags.NoLoadingScreen)).Then(() => Services.Audio.FadeIn(FadeDuration))
            );
        }

        static public IEnumerator LoadPreviousSceneWithFader(object inContext = null)
        {
            Services.Audio.FadeOut(FadeDuration);
            return Services.UI.WorldFaders.FadeTransition(Color.white, FadeDuration, 0,
                () => Sequence.Create(Services.State.LoadPreviousScene(null, inContext, SceneLoadFlags.NoLoadingScreen)).Then(() => Services.Audio.FadeIn(FadeDuration))
            );
        }
    }
}