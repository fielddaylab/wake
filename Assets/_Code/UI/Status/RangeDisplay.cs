using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using BeauRoutine;
using System;

namespace Aqua
{
    [RequireComponent(typeof(RectTransform))]
    public class RangeDisplay : MonoBehaviour
    {
        [SerializeField] private bool m_ClampValues = true;
        [SerializeField, Range(0, 1)] private float m_RestrictRange = 1;

        [NonSerialized] private RectTransform m_RectTransform;

        public void Display(float inMin, float inMax, float inFullMin, float inFullMax, bool inbIgnoreOffset = false)
        {
            float fullDistance = (inFullMax - inFullMin);
            float minRatio = (inMin - inFullMin) / fullDistance;
            float maxRatio = (inMax - inFullMin) / fullDistance;

            if (m_ClampValues)
            {
                minRatio = Mathf.Clamp01(minRatio);
                maxRatio = Mathf.Clamp01(maxRatio);
            }

            if (!inbIgnoreOffset)
            {
                float offset = (1 - m_RestrictRange) / 2;
                if (minRatio != 0)
                {
                    minRatio = offset + minRatio * m_RestrictRange;
                }
                if (maxRatio != 1)
                {
                    maxRatio = offset + maxRatio * m_RestrictRange;
                }
            }

            this.CacheComponent(ref m_RectTransform);

            Vector2 anchorMin = m_RectTransform.anchorMin;
            Vector2 anchorMax = m_RectTransform.anchorMax;

            anchorMin.x = minRatio;
            anchorMax.x = maxRatio;

            m_RectTransform.anchorMin = anchorMin;
            m_RectTransform.anchorMax = anchorMax;
        }
    
        public float AdjustValue(float inInput)
        {
            return AdjustValue(inInput, m_RestrictRange);
        }

        static public float AdjustValue(float inInput, float inRange)
        {
            return (1 - inRange) / 2 + inInput * inRange;
        }
    }
}