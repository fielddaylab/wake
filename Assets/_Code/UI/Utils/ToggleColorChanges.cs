using System;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public sealed class ToggleColorChanges : MonoBehaviour, IUpdaterUI
    {
        [NonSerialized] private Toggle m_Toggle;
        [SerializeField] private Graphic m_Graphic = null;
        [SerializeField] private ColorGroup m_Group = null;
        [SerializeField] private Color m_OnColor = default;
        [SerializeField] private Color m_OffColor = default;

        [NonSerialized] private bool m_LastKnownToggleState;

        private void Awake() {
            m_Toggle = GetComponentInParent<Toggle>();
            OnToggleUpdated(m_Toggle.isOn);
        }

        private void OnEnable() {
            Services.UI.RegisterUpdate(this);
        }

        private void OnDisable() {
            Services.UI?.DeregisterUpdate(this);
        }

        private void OnToggleUpdated(bool state) {
            m_LastKnownToggleState = state;
            if (m_Graphic) {
                m_Graphic.color = state ? m_OnColor : m_OffColor;
            }
            if (m_Group) {
                m_Group.Color = state ? m_OnColor : m_OffColor;
            }
        }

        public void OnUIUpdate() {
            if (m_Toggle.isOn != m_LastKnownToggleState) {
                OnToggleUpdated(m_Toggle.isOn);
            }
        }
    }
}