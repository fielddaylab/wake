using System;
using BeauRoutine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aqua {
    [RequireComponent(typeof(LayoutOffset))]
    public class ClickPopAnim : MonoBehaviour, ILayoutAnim, IPointerClickHandler, IPointerUpHandler {
        private const float AnimDuration = 0.15f;
        private const float AnimDistance = -2;

        [NonSerialized] private float m_TimeLeft;
        [NonSerialized] private Selectable m_Selectable = null;
        [NonSerialized] private LayoutOffset m_Offset = null;
        [NonSerialized] private bool m_WasSelectable = false;

        private void Awake() {
            m_Selectable = GetComponent<Selectable>();
            m_Offset = GetComponent<LayoutOffset>();
        }

        private void OnEnable() {
            Services.Animation.Layout.TryAdd(this, m_TimeLeft);
        }

        private void OnDisable() {
            if (m_TimeLeft > 0) {
                m_TimeLeft = 0;
                if (m_Offset) {
                    m_Offset.Offset1 = default(Vector2);
                }
                Services.Animation.Layout?.Remove(this);
            }
        }

        public void Ping() {
            Services.Animation.Layout.TryAdd(this, ref m_TimeLeft, AnimDuration);
        }

        bool ILayoutAnim.OnAnimUpdate(float dt) {
            m_TimeLeft = Math.Max(0, m_TimeLeft - dt);
            float amt = (m_TimeLeft / AnimDuration);
            m_Offset.Offset1 = new Vector2(0, AnimDistance * amt);
            return m_TimeLeft > 0;
        }

        bool ILayoutAnim.IsActive() {
            return isActiveAndEnabled;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            if (eventData.button != 0) {
                return;
            }

            if (Services.Input.IsForcingInput() || m_WasSelectable) {
                Services.Animation.Layout.TryAdd(this, ref m_TimeLeft, AnimDuration);
            }
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
            m_WasSelectable = !m_Selectable || m_Selectable.IsInteractable();
        }
    }
}