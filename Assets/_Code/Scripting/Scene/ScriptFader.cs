using BeauUtil;
using UnityEngine;
using System.Collections;
using Leaf.Runtime;
using BeauRoutine;
using UnityEngine.UI;

namespace Aqua.Scripting
{
    public class ScriptFader : ScriptComponent
    {
        #region Inspector

        [SerializeField] private CanvasGroup m_CanvasGroup = null;
        [SerializeField] private ColorGroup m_ColorGroup = null;
        [SerializeField] private Graphic m_Graphic = null;
        [SerializeField] private SpriteRenderer m_Sprite = null;

        #endregion // Inspector

        [LeafMember("FadeTo")]
        public IEnumerator FadeTo(float inAlpha, float inDuration = 0.5f)
        {
            if (m_CanvasGroup)
                return m_CanvasGroup.FadeTo(inAlpha, inDuration);
            if (m_ColorGroup)
                return Tween.Float(m_ColorGroup.GetAlpha(), inAlpha, m_ColorGroup.SetAlpha, inDuration);
            if (m_Graphic)
                return m_Graphic.FadeTo(inAlpha, inDuration);
            if (m_Sprite)
                return m_Sprite.FadeTo(inAlpha, inDuration);
            return null;
        }

        [LeafMember("SetFade")]
        public void SetFade(float inAlpha)
        {
            if (m_CanvasGroup)
                m_CanvasGroup.alpha = inAlpha;
            if (m_ColorGroup)
                m_ColorGroup.SetAlpha(inAlpha);
            if (m_Graphic)
                m_Graphic.SetAlpha(inAlpha);
            if (m_Sprite)
                m_Sprite.SetAlpha(inAlpha);
        }

        #if UNITY_EDITOR

        private void Reset()
        {
            m_CanvasGroup = GetComponent<CanvasGroup>();
            m_ColorGroup = GetComponent<ColorGroup>();
            m_Graphic = GetComponent<Graphic>();
            m_Sprite = GetComponent<SpriteRenderer>();
        }

        #endif // UNITY_EDITOR
    }
}