using System;
using System.Collections;
using Aqua.Animation;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua {
    public class LoadingIcon : MonoBehaviour {
        static private readonly StringHash32 Event_Activate = "loading-icon:activate";
        static private readonly StringHash32 Event_Deactivate = "loading-icon:deactivate";
        static private readonly StringHash32 Event_Prompt = "loading-icon:prompt";

        #region Inspector

        [SerializeField] private Canvas m_Canvas = null;

        [Header("Loading")]
        [SerializeField] private float m_LoadingDelay = 2;
        [SerializeField] private CanvasGroup m_LoadingGroup = null;
        
        [Header("Slow Connection")]
        [SerializeField] private float m_SlowLoadingDelay = 15;
        [SerializeField] private CanvasGroup m_SlowLoadingGroup = null;

        [Header("Retry")]
        [SerializeField] private CanvasGroup m_RetryGroup = null;
        [SerializeField] private Button m_RetryButton = null;

        #endregion // Inspector

        private Routine m_DisplayRoutine;
        private int m_ActiveCount;
        private bool m_RetryQueued;

        private void Awake() {
            m_Canvas.enabled = false;
            m_LoadingGroup.alpha = 0;
            m_SlowLoadingGroup.alpha = 0;
            m_RetryGroup.alpha = 0;
            m_RetryGroup.blocksRaycasts = false;

            m_RetryButton.onClick.AddListener(() => {
                m_RetryQueued = true;
            });

            Services.Events.Register(GameEvents.SceneWillUnload, Activate, this)
                .Register(GameEvents.SceneLoaded, Deactivate, this)
                .Register(Event_Activate, Activate, this)
                .Register(Event_Deactivate, Deactivate, this)
                .Register<Future>(Event_Prompt, OnPromptRetry, this);
        }

        private void OnDestroy() {
            Services.Events?.DeregisterAll(this);
        }

        private void Activate() {
            SetActive(true);
        }

        private void Deactivate() {
            SetActive(false);
        }

        private void OnPromptRetry(Future future) {
            if (m_ActiveCount == 0) {
                m_ActiveCount++;
            }
            m_DisplayRoutine.Replace(this, DisplayPrompt(future)).Tick();
        }

        private void SetActive(bool active) {
            if (active) {
                m_ActiveCount++;
                if (m_ActiveCount == 1) {
                    m_DisplayRoutine.Replace(this, Show()).Tick();
                }
            } else if (m_ActiveCount > 0) {
                m_ActiveCount--;
                if (m_ActiveCount == 0) {
                    m_DisplayRoutine.Replace(this, Complete()).Tick();
                }
            }
        }

        private IEnumerator Show() {
            yield return m_LoadingDelay;

            m_Canvas.enabled = true;
            yield return m_LoadingGroup.FadeTo(1, 0.1f);

            yield return m_SlowLoadingDelay - m_LoadingDelay;
            
            yield return Routine.Combine(
                m_LoadingGroup.FadeTo(0, 0.5f),
                m_SlowLoadingGroup.FadeTo(1, 0.5f)
            );
        }

        private IEnumerator Complete() {
            if (!m_Canvas.enabled) {
                m_LoadingGroup.alpha = 0;
                m_SlowLoadingGroup.alpha = 0;
                m_RetryGroup.alpha = 0;
                yield break;
            }

            yield return Routine.Combine(
                m_LoadingGroup.FadeTo(0, 0.1f).DelayBy(0.1f),
                m_SlowLoadingGroup.FadeTo(0, 0.1f).DelayBy(0.1f),
                m_RetryGroup.FadeTo(0, 0.1f).DelayBy(0.1f)
            );
            m_Canvas.enabled = false;
        }

        private IEnumerator DisplayPrompt(Future future) {
            m_Canvas.enabled = true;

            m_RetryGroup.blocksRaycasts = true;
            m_RetryQueued = false;

            yield return Routine.Combine(
                m_SlowLoadingGroup.FadeTo(0, 0.5f),
                m_LoadingGroup.FadeTo(0, 0.5f),
                m_RetryGroup.FadeTo(1, 0.5f)
            );

            while(!m_RetryQueued) {
                yield return null;
            }

            future.Complete();

            m_RetryGroup.blocksRaycasts = false;

            yield return Routine.Combine(
                m_SlowLoadingGroup.FadeTo(1, 0.5f),
                m_RetryGroup.FadeTo(0, 0.5f)
            );
        }

        static public void Queue()
        {
            Services.Events?.Dispatch(Event_Activate);
        }

        static public void Cancel()
        {
            Services.Events?.Dispatch(Event_Deactivate);
        }

        static public Future PromptRetry()
        {
            Future future = new Future();
            Services.Events?.Dispatch(Event_Prompt, future);
            return future;
        }
    }
}