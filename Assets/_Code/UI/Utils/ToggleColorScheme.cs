using System;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public sealed class ToggleColorScheme : MonoBehaviour, IUpdaterUI
    {
        [Header("Colors")]
        [SerializeField] private ColorPalette2 m_OffPalette;
        [SerializeField] private ColorPalette2 m_OnPalette;

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
        [NonSerialized] private bool m_LastKnownToggleState;

        private void Awake() {
            m_Toggle = GetComponentInParent<Toggle>();
            OnToggleUpdated(m_Toggle.isOn, true);
        }

        private void OnEnable() {
            Services.UI.RegisterUpdate(this);
        }

        private void OnDisable() {
            Services.UI?.DeregisterUpdate(this);
        }

        private void OnToggleUpdated(bool state, bool force) {
            m_LastKnownToggleState = state;
            
            ColorPalette2 palette = state ? m_OnPalette : m_OffPalette;
            
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

            m_OffGroup.SetActive(!state, force);
            m_OnGroup.SetActive(state, force);
        }

        public void OnUIUpdate() {
            if (m_Toggle.isOn != m_LastKnownToggleState) {
                OnToggleUpdated(m_Toggle.isOn, false);
            }
        }
    }
}