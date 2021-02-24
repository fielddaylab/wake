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
        [NonSerialized] private RectTransform m_RectTransform;

        public void Display(float inMin, float inMax, float inFullMin, float inFullMax)
        {
            float fullDistance = (inFullMax - inFullMin);
            float minRatio = (inMin - inFullMin) / fullDistance;
            float maxRatio = (inMax - inFullMin) / fullDistance;

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