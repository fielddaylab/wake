using Aqua;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using UnityEngine.UI.Extensions;
using System;

namespace ProtoAqua.Modeling
{
    public class GraphPoint : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Image m_Background = null;
        [SerializeField] private Image m_Renderer = null;

        #endregion // Inspector

        [NonSerialized] private Vector2 m_RawPoint;

        public Image Background { get { return m_Background; } }
        public Image Renderer { get { return m_Renderer; } }

        public void SetPoint(Vector2 inPoint)
        {
            m_RawPoint = inPoint;
        }

        public void SetPoint(float inX, float inY)
        {
            m_RawPoint.x = inX;
            m_RawPoint.y = inY;
        }

        public void Render(Rect inBounds)
        {
            float xMin = inBounds.xMin, xMax = inBounds.xMax, yMin = inBounds.yMin, yMax = inBounds.yMax;
            float x = MathUtil.Remap(m_RawPoint.x, xMin, xMax, 0, 1);
            float y = MathUtil.Remap(m_RawPoint.y, yMin, yMax, 0, 1);
            RectTransform r = m_Renderer.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(x, y);
        }
    }
}