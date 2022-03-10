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

        #region Inspector

        [SerializeField] private Canvas m_Canvas = null;
        [SerializeField] private CanvasGroup m_Group = null;
        [SerializeField] private float m_LoadingDelay = 2;

        #endregion // Inspector

        private Routine m_DisplayRoutine;
        private int m_ActiveCount;

        private void Awake() {
            m_Canvas.enabled = false;
            m_Group.alpha = 0;

            Services.Events.Register(GameEvents.SceneWillUnload, Activate, this)
                .Register(GameEvents.SceneLoaded, Deactivate, this)
                .Register(Event_Activate, Activate, this)
                .Register(Event_Deactivate, Deactivate, this);
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
            yield return m_Group.FadeTo(1, 0.1f);
        }

        private IEnumerator Complete() {
            if (!m_Canvas.enabled) {
                m_Group.alpha = 0;
                yield break;
            }

            yield return m_Group.FadeTo(0, 0.1f).DelayBy(0.1f);
            m_Canvas.enabled = false;
        }

        static public void Queue()
        {
            Services.Events?.Dispatch(Event_Activate);
        }

        static public void Cancel()
        {
            Services.Events?.Dispatch(Event_Deactivate);
        }
    }
}