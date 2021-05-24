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

        [NonSerialized] private RectTransform m_RectTransform;

        public void Display(float inMin, float inMax, float inFullMin, float inFullMax)
        {
            float fullDistance = (inFullMax - inFullMin);
            float minRatio = (inMin - inFullMin) / fullDistance;
            float maxRatio = (inMax - inFullMin) / fullDistance;

            if (m_ClampValues)
            {
                minRatio = Mathf.Clamp01(minRatio);
                maxRatio = Mathf.Clamp01(maxRatio);
            }

            this.CacheComponent(ref m_RectTransform);

            Vector2 anchorMin = m_RectTransform.anchorMin;
            Vector2 anchorMax = m_RectTransform.anchorMax;

            anchorMin.x = minRatio;
            anchorMax.x = maxRatio;

            m_RectTransform.anchorMin = anchorMin;
            m_RectTransform.anchorMax = anchorMax;
        }
    }
}