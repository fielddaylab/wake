using System;
using System.Collections;
using BeauPools;
using BeauRoutine;
using BeauUtil.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    static public class CanvasExtensions
    {
        static public void SetAnchorX(this RectTransform inRect, float inX)
        {
            Vector2 min = inRect.anchorMin,
                max = inRect.anchorMax;
            min.x = inX;
            max.x = inX;
            inRect.anchorMin = min;
            inRect.anchorMax = max;
        }

        static public void SetAnchorY(this RectTransform inRect, float inY)
        {
            Vector2 min = inRect.anchorMin,
                max = inRect.anchorMax;
            min.y = inY;
            max.y = inY;
            inRect.anchorMin = min;
            inRect.anchorMax = max;
        }

        static public IEnumerator Show(this CanvasGroup inGroup, float inDuration, bool? inbRaycasts = null)
        {
            if (!inGroup.gameObject.activeSelf)
            {
                inGroup.alpha = 0;
                inGroup.gameObject.SetActive(true);
                if (inbRaycasts.HasValue)
                    inGroup.blocksRaycasts = false;
            }
            yield return inGroup.FadeTo(1, inDuration);
            if (inbRaycasts.HasValue)
                inGroup.blocksRaycasts = inbRaycasts.Value;
        }

        static public IEnumerator Hide(this CanvasGroup inGroup, float inDuration, bool? inbRaycasts = null)
        {
            if (inGroup.gameObject.activeSelf)
            {
                if (inbRaycasts.HasValue)
                    inGroup.blocksRaycasts = inbRaycasts.Value;
                yield return inGroup.FadeTo(0, inDuration);
                inGroup.gameObject.SetActive(false);
            }
        }
    }
}