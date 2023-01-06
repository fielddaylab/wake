using System;
using System.Runtime.CompilerServices;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua {
    [RequireComponent(typeof(CanvasRenderer), typeof(Graphic))]
    public class FlashAnim : MonoBehaviour, ILayoutAnim
    {
        private const float AnimDuration = 0.2f;

        [NonSerialized] private float m_TimeLeft;
        [NonSerialized] private Graphic m_Graphic;
        [NonSerialized] private float m_OriginalAlpha;

        private void Awake() {
            TryGetComponent(out m_Graphic);
            m_OriginalAlpha = m_Graphic.GetAlpha();
        }

        private void OnEnable() {
            m_Graphic.enabled = m_TimeLeft > 0;
            Services.Animation.Layout.TryAdd(this, m_TimeLeft);
        }

        private void OnDisable() {
            if (Services.Valid && m_TimeLeft > 0) {
                m_TimeLeft = 0;

                m_Graphic.enabled = false;

                Services.Animation.Layout.Remove(this);
            }
        }

        bool ILayoutAnim.OnAnimUpdate(float deltaTime) {
            m_TimeLeft = Math.Max(0, m_TimeLeft - deltaTime);
            float alpha = m_TimeLeft / AnimDuration;
            m_Graphic.SetAlpha(TweenUtil.Evaluate(Curve.CubeOut, alpha) * m_OriginalAlpha);
            m_Graphic.enabled = alpha > 0;
            return m_TimeLeft > 0;
        }

        bool ILayoutAnim.IsActive() {
            return isActiveAndEnabled;
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ping() {
            Services.Animation.Layout.TryAdd(this, ref m_TimeLeft, AnimDuration);
            m_Graphic.enabled = true;
            m_Graphic.SetAlpha(m_OriginalAlpha);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ping(Color color) {
            Services.Animation.Layout.TryAdd(this, ref m_TimeLeft, AnimDuration);
            m_Graphic.enabled = true;
            m_Graphic.color = color.WithAlpha(m_OriginalAlpha);
        }
    }
}