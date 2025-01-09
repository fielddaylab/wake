using System;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay.Debugging {
    /// <summary>
    /// Time display.
    /// </summary>
    public class ConsoleTimeDisplay : MonoBehaviour {
        #region Inspector

        [SerializeField] private GameObject m_TimeScaleGroup = null;
        [SerializeField] private TMP_Text m_TimeScaleLabel = null;
        [SerializeField] private Image m_StateIcon = null;

        [Header("Config")]
        [SerializeField] private Sprite m_PlaySprite = null;
        [SerializeField] private Sprite m_PauseSprite = null;
        [SerializeField] private Sprite m_FFSprite = null;
        [SerializeField] private Sprite m_SlowSprite = null;

        #endregion // Inspector

        [NonSerialized] private float m_LastKnownTimescale;
        [NonSerialized] private bool m_Paused;

        private void Awake() {
            UpdateTimescale(Time.timeScale);
        }

        public void UpdateTimescale(float inTimeScale) {
            if (m_LastKnownTimescale != inTimeScale) {
                m_LastKnownTimescale = inTimeScale;

                if (inTimeScale == 1) {
                    m_TimeScaleGroup.SetActive(false);
                } else {
                    if (inTimeScale < 1 || ((int) inTimeScale != inTimeScale)) {
                        m_TimeScaleLabel.text = string.Format("{0:0.000}x", inTimeScale);
                    } else {
                        m_TimeScaleLabel.text = string.Format("{0}x", ((int) inTimeScale).ToStringLookup());
                    }
                    m_TimeScaleGroup.SetActive(true);
                }

                UpdateIcon();
            }
        }

        private void UpdateIcon() {
            Sprite icon;
            if (m_Paused) {
                icon = m_PauseSprite;
            } else if (m_LastKnownTimescale > 1) {
                icon = m_FFSprite;
            } else if (m_LastKnownTimescale < 1) {
                icon = m_SlowSprite;
            } else {
                icon = m_PlaySprite;
            }

            m_StateIcon.sprite = icon;
        }

        public void UpdateState(bool paused) {
            if (m_Paused != paused) {
                m_Paused = paused;
                UpdateIcon();
            }
        }
    }
}