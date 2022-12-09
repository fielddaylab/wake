using System;
using BeauRoutine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aqua {
    [RequireComponent(typeof(LayoutOffset))]
    public class ClickPopAnim : MonoBehaviour, IUpdaterUI, IPointerClickHandler {
        private const float AnimDuration = 0.15f;
        private const float AnimDistance = -2;

        [NonSerialized] private float m_TimeLeft;
        [NonSerialized] private Selectable m_Selectable = null;
        [NonSerialized] private LayoutOffset m_Offset = null;

        private void Awake() {
            m_Selectable = GetComponent<Selectable>();
            m_Offset = GetComponent<LayoutOffset>();
        }

        private void OnEnable() {
            Services.UI.RegisterUpdate(this);
        }

        private void OnDisable() {
            Services.UI?.DeregisterUpdate(this);
        }

        void IUpdaterUI.OnUIUpdate() {
            if (m_TimeLeft > 0) {
                m_TimeLeft = Math.Max(0, m_TimeLeft - Routine.DeltaTime);
                float amt = (m_TimeLeft / AnimDuration);
                m_Offset.Offset1 = new Vector2(0, AnimDistance * amt);
            }
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            if (eventData.button != 0) {
                return;
            }

            if (Services.Input.IsForcingInput() || !m_Selectable || m_Selectable.IsInteractable()) {
                m_TimeLeft = AnimDuration;
            }
        }
    }
}