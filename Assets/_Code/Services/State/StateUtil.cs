using System.Collections;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua {
    static public class StateUtil {
        private const SceneLoadFlags LoadFlags = SceneLoadFlags.DoNotDispatchPreUnload;

        private const float FadeDuration = 0.25f;
        private const float PauseDuration = 0.15f;
        private const string DefaultBackScene = "Helm";

        static private SceneLoadFlags s_LastFlags;
        static private bool s_IsLoading = false;

        static public bool IsLoading {
            get { return s_IsLoading || (Services.Valid && Services.State.IsLoadingScene()); }
        }

        static public IEnumerator LoadSceneWithFader(string inSceneName, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default) {
            inFlags |= LoadFlags;

            if (!BeforeLoad(inFlags)) {
                return null;
            }

            return Services.UI.ScreenFaders.FadeTransition(Color.black, FadeDuration, PauseDuration,
                () => Sequence.Create(Services.State.LoadScene(inSceneName, inEntrance, inContext, inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadSceneWithWipe(string inSceneName, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default) {
            inFlags |= LoadFlags;

            if (!BeforeLoad(inFlags)) {
                return null;
            }

            return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(Services.State.LoadScene(inSceneName, inEntrance, inContext, inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadMapWithWipe(StringHash32 inMapId, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default) {
            inFlags |= LoadFlags;

            if (!BeforeLoad(inFlags)) {
                return null;
            }

            StringHash32 currentMapId = MapDB.LookupCurrentMap();
            if (!currentMapId.IsEmpty) {
                if (inEntrance.IsEmpty && (inFlags & SceneLoadFlags.DoNotOverrideEntrance) == 0)
                    inEntrance = currentMapId;
            }

            return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(Services.State.LoadSceneFromMap(inMapId, inEntrance, inContext, inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadPreviousSceneWithFader(StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default) {
            inFlags |= LoadFlags;

            if (!BeforeLoad(inFlags)) {
                return null;
            }

            return Services.UI.ScreenFaders.FadeTransition(Color.black, FadeDuration, PauseDuration,
                () => Sequence.Create(Services.State.LoadPreviousScene(DefaultBackScene, inEntrance, inContext, inFlags)).Then(AfterLoad)
            );
        }

        static public IEnumerator LoadPreviousSceneWithWipe(StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default) {
            string sceneToLoad = null;
            StringHash32 currentMapId = MapDB.LookupCurrentMap();
            if (!currentMapId.IsEmpty) {
                if (inEntrance.IsEmpty && (inFlags & SceneLoadFlags.DoNotOverrideEntrance) == 0)
                    inEntrance = currentMapId;
                sceneToLoad = Assets.Map(currentMapId).Parent()?.SceneName();
            }

            inFlags |= LoadFlags;

            if (!BeforeLoad(inFlags)) {
                return null;
            }

            if (!string.IsNullOrEmpty(sceneToLoad)) {
                return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(PreSceneChange).Then(Services.State.LoadScene(sceneToLoad, inEntrance, inContext, inFlags)).Then(AfterLoad)
            );
            }
            return Services.UI.ScreenFaders.WipeTransition(PauseDuration,
                () => Sequence.Create(PreSceneChange).Then(Services.State.LoadPreviousScene(DefaultBackScene, inEntrance, inContext, inFlags)).Then(AfterLoad)
            );
        }

        static private bool BeforeLoad(SceneLoadFlags inFlags) {
            if (IsLoading)
                return false;

            s_LastFlags = inFlags;
            if ((s_LastFlags & SceneLoadFlags.Cutscene) != 0) {
                Services.UI.ShowLetterbox();
            }
            if ((s_LastFlags & SceneLoadFlags.StopMusic) != 0) {
                Services.Audio.StopMusic();
            }
            if ((s_LastFlags & SceneLoadFlags.SuppressAutoSave) != 0) {
                AutoSave.Suppress();
            }
            Services.Input.PauseAll();
            Services.Audio.FadeOut(FadeDuration);
            Services.Script.KillLowPriorityThreads(TriggerPriority.Cutscene, true);
            Services.Events.Dispatch(GameEvents.SceneWillUnload);
            s_IsLoading = true;
            return true;
        }

        static private void PreSceneChange() {
            s_IsLoading = false;
        }

        static private void AfterLoad() {
            if ((s_LastFlags & SceneLoadFlags.Cutscene) != 0) {
                Services.UI.HideLetterbox();
            }
            Services.Audio.FadeIn(FadeDuration);
            Services.Input.ResumeAll();
            s_IsLoading = false;
        }
    }
}