using System;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public sealed class ToggleColorScheme : MonoBehaviour, IUpdaterUI
    {
        private enum State : byte {
            Disabled,
            Off,
            On
        }

        [Header("Colors")]
        [SerializeField] private ColorPalette2 m_OffPalette;
        [SerializeField] private ColorPalette2 m_OnPalette;
        [SerializeField] private ColorPalette2 m_DisabledPalette;

        [Header("Backgrounds")]
        [SerializeField] private Graphic[] m_BackgroundGraphics = null;
        [SerializeField] private ColorGroup[] m_BackgroundGroups = null;
        
        [Header("Content")]
        [SerializeField] private Graphic[] m_ContentGraphics = null;
        [SerializeField] private ColorGroup[] m_ContentGroups = null;

        [Header("Activate")]
        [SerializeField] private ActiveGroup m_OffGroup = new ActiveGroup();
        [SerializeField] private ActiveGroup m_OnGroup = new ActiveGroup();

        [NonSerialized] private Toggle m_Toggle;
        [NonSerialized] private State m_LastKnownToggleState;

        private void Awake() {
            m_Toggle = GetComponentInParent<Toggle>();
            Assert.NotNull(m_Toggle);
            OnToggleUpdated(GetState(m_Toggle), true);
        }

        private void OnEnable() {
            Services.UI.RegisterUpdate(this);
        }

        private void OnDisable() {
            Services.UI?.DeregisterUpdate(this);
        }

        private void OnToggleUpdated(State state, bool force) {
            m_LastKnownToggleState = state;
            
            ColorPalette2 palette;
            if (state == State.Disabled) {
                palette = m_DisabledPalette;
            } else if (state == State.Off) {
                palette = m_OffPalette; 
            } else {
                palette = m_OnPalette;
            }
            
            foreach(var bg in m_BackgroundGraphics) {
                bg.color = palette.Background;
            }
            foreach(var bg in m_BackgroundGroups) {
                bg.Color = palette.Background;
            }

            foreach(var content in m_ContentGraphics) {
                content.color = palette.Content;
            }
            foreach(var content in m_ContentGroups) {
                content.Color = palette.Content;
            }

            m_OffGroup.SetActive(state != State.On, force);
            m_OnGroup.SetActive(state == State.On, force);
        }

        public void OnUIUpdate() {
            State nextState = GetState(m_Toggle);
            if (nextState != m_LastKnownToggleState) {
                OnToggleUpdated(nextState, false);
            }
        }

        static private State GetState(Toggle toggle) {
            if (!toggle.interactable) {
                return State.Disabled;
            } else {
                return toggle.isOn ? State.On : State.Off;
            }
        }

        #if UNITY_EDITOR

        private void OnValidate() {
            if (Application.IsPlaying(this)) {
                return;
            }

            if (!m_Toggle) {
                m_Toggle = GetComponentInParent<Toggle>();
                if (!m_Toggle) {
                    return;
                }
            }

            State nextState = GetState(m_Toggle);
            OnToggleUpdated(nextState, true);
        }

        #endif // UNITY_EDITOR
    }
}