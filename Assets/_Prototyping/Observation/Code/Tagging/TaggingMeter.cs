using UnityEngine;
using Aqua;
using UnityEngine.UI;
using BeauUtil.UI;
using System;

namespace ProtoAqua.Observation
{
    public class TaggingMeter : MonoBehaviour, ILayoutAnim
    {
        private const float AnimTime = 0.1f;
        private const float AnimDist = -2;

        #region Inspector

        public LayoutOffset Offset;
        public Image Icon;
        public EllipseGraphic Meter;

        #endregion // Inspector

        [NonSerialized] private float m_Tick;

        private void OnEnable() {
            Services.Animation.Layout.TryAdd(this, ref m_Tick, transform.GetSiblingIndex() * 0.01f);
        }

        private void OnDisable() {
            if (m_Tick > 0) {
                Services.Animation.Layout.Remove(this);
                m_Tick = 0;
            }
            Offset.Offset1 = default(Vector2);
        }

        bool ILayoutAnim.OnAnimUpdate(float deltaTime) {
            m_Tick = Math.Max(0, m_Tick - deltaTime);
            float amt = Mathf.Clamp01(m_Tick / AnimTime);
            Offset.Offset1 = new Vector2(0, amt * AnimDist);
            return m_Tick > 0;
        }

        bool ILayoutAnim.IsActive() {
            return isActiveAndEnabled;
        }

        public void Ping() {
            Services.Animation.Layout.TryAdd(this, ref m_Tick, AnimTime);
        }
    }
}