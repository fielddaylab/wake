using UnityEngine;
using Aqua;
using BeauUtil;
using ScriptableBake;
using BeauUtil.UI;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

namespace ProtoAqua.Observation {
    public class BreakerUI : SharedPanel
    {
        [Serializable]
        private struct PhaseConfig {
            public TextId Label;
            public Color32 BG;
            public Color32 Fill;
        }

        #region Inspector

        [Header("Breaker")]
        [SerializeField] private RectTransformPinned m_Pin = null;
        [SerializeField] private Button m_Button = null;
        [SerializeField] private PointerListener m_Pointer = null;
        [SerializeField] private ColorGroup m_ButtonBG = null;
        [SerializeField] private LocText m_ButtonText = null;
        [SerializeField] private RectTransform m_ButtonFill = null;
        [SerializeField] private ColorGroup m_ButtonFillColor = null;
        [SerializeField] private Mask m_ButtonMask = null;

        [Header("Config")]
        [SerializeField] private PhaseConfig m_ReadyState = default;
        [SerializeField] private PhaseConfig m_CharginState = default;
        [SerializeField] private PhaseConfig m_DischarginState = default;
        [SerializeField] private PhaseConfig m_RecharginState = default;

        #endregion // Inspector

        [NonSerialized] private bool m_Active;
        [NonSerialized] private bool m_ButtonHeld;

        protected override void Awake() {
            base.Awake();

            Services.Events.Register(GameEvents.ContextDisplay, OnContextDisplay, this)
                .Register(GameEvents.ContextHide, OnContextHide, this);

            m_Pointer.onPointerDown.AddListener(OnButtonPress);
            m_Pointer.onPointerUp.AddListener(OnButtonReleased);

            SetProgress(0);
        }

        protected override void OnDestroy() {
            Services.Events?.DeregisterAll(this);

            base.OnDestroy();
        }

        public void Enable(Transform pin) {
            m_Pin.Pin(pin);
            m_Active = true;

            if (!ContextButtonDisplay.IsDisplaying())
                Show();
        }

        public void Disable() {
            m_Pin.Unpin();
            m_Active = false;
            Hide();
        }

        public bool IsHeld() {
            return m_ButtonHeld;
        }

        public void SetPhase(PlayerROVBreaker.Phase phase, float progress = 0) {
            switch(phase) {
                case PlayerROVBreaker.Phase.Ready: {
                    SetConfig(m_ReadyState);
                    m_Button.interactable = true;
                    break;
                }
                case PlayerROVBreaker.Phase.Charging: {
                    SetConfig(m_CharginState);
                    m_Button.interactable = true;
                    break;
                }
                case PlayerROVBreaker.Phase.Bursting: {
                    SetConfig(m_DischarginState);
                    m_Button.interactable = false;
                    m_ButtonHeld = false;
                    break;
                }
                case PlayerROVBreaker.Phase.Recharging: {
                    SetConfig(m_RecharginState);
                    m_Button.interactable = false;
                    m_ButtonHeld = false;
                    break;
                }
            }

            SetProgress(progress);
        }

        public void SetProgress(float progress) {
            m_ButtonFill.SetMaxAnchorX(progress);
            m_ButtonFillColor.Visible = m_ButtonMask.enabled = progress > 0;
        }

        private void SetConfig(PhaseConfig config) {
            m_ButtonText.SetText(config.Label);
            m_ButtonBG.Color = config.BG;
            m_ButtonFillColor.Color = config.Fill;
        }

        #region Handlers

        private void OnContextDisplay() {
            if (!m_Active) {
                return;
            }

            Hide();
        }

        private void OnContextHide() {
            if (!m_Active) {
                return;
            }

            Show();
        }

        private void OnButtonPress(PointerEventData evt) {
            if (evt.button != 0) {
                return;
            }

            m_ButtonHeld = true;
        }

        private void OnButtonReleased(PointerEventData evt) {
            if (evt.button != 0) {
                return;
            }

            m_ButtonHeld = false;
        }

        #endregion // Handlers
    }
}