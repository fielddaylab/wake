using System.Collections;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua {
    static public class CanvasExtensions {
        static public void SetAnchorX(this RectTransform inRect, float inX) {
            Vector2 min = inRect.anchorMin,
                max = inRect.anchorMax;
            min.x = inX;
            max.x = inX;
            inRect.anchorMin = min;
            inRect.anchorMax = max;
        }

        static public void SetAnchorY(this RectTransform inRect, float inY) {
            Vector2 min = inRect.anchorMin,
                max = inRect.anchorMax;
            min.y = inY;
            max.y = inY;
            inRect.anchorMin = min;
            inRect.anchorMax = max;
        }

        static public IEnumerator Show(this CanvasGroup inGroup, float inDuration, bool? inbRaycasts = true) {
            if (!inGroup.gameObject.activeSelf) {
                inGroup.alpha = 0;
                inGroup.gameObject.SetActive(true);
            }
            if (inbRaycasts.HasValue)
                inGroup.blocksRaycasts = false;
            yield return inGroup.FadeTo(1, inDuration);
            if (inbRaycasts.HasValue)
                inGroup.blocksRaycasts = inbRaycasts.Value;
        }

        static public void Show(this CanvasGroup inGroup, bool? inbRaycasts = true) {
            inGroup.alpha = 1;
            if (inbRaycasts.HasValue) {
                inGroup.blocksRaycasts = inbRaycasts.Value;
            }
            inGroup.gameObject.SetActive(true);
        }

        static public IEnumerator Hide(this CanvasGroup inGroup, float inDuration, bool? inbRaycasts = false) {
            if (inGroup.gameObject.activeSelf) {
                if (inbRaycasts.HasValue)
                    inGroup.blocksRaycasts = inbRaycasts.Value;
                yield return inGroup.FadeTo(0, inDuration);
                inGroup.gameObject.SetActive(false);
            }
        }

        static public void Hide(this CanvasGroup inGroup, bool? inbRaycasts = false) {
            inGroup.gameObject.SetActive(false);
            inGroup.alpha = 0;
            if (inbRaycasts.HasValue) {
                inGroup.blocksRaycasts = inbRaycasts.Value;
            }
        }

        static public void ScrollYToShow(this ScrollRect inRect, RectTransform inTransform) {
            float transformY = inTransform.rect.center.y;
            float totalHeight = ((RectTransform) inTransform.parent).rect.height;
            inRect.verticalNormalizedPosition = transformY / totalHeight;
        }
    }
}