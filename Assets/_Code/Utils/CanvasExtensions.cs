using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
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

        static public void SetAnchors(this RectTransform inRect, float inX, float inY) {
            inRect.anchorMin = inRect.anchorMax = new Vector2(inX, inY);
        }

        static public void SetAnchors(this RectTransform inRect, Vector2 inAnchor) {
            inRect.anchorMin = inRect.anchorMax = inAnchor;
        }

        static public void SetMaxAnchorX(this RectTransform inRect, float inAnchorX) {
            inRect.anchorMax = new Vector2(inAnchorX, inRect.anchorMax.y);
        }

        static public void SetAnchorsY(this RectTransform inRect, float inY0, float inY1) {
            Vector2 min = inRect.anchorMin,
                max = inRect.anchorMax;
            min.y = inY0;
            max.y = inY1;
            inRect.anchorMin = min;
            inRect.anchorMax = max;
        }

        static public void ResizeXForAspectRatio(this RectTransform inRect, float inWidth, float inHeight) {
            #if UNITY_EDITOR
            if (!Application.IsPlaying(inRect)) {
                UnityEditor.Undo.RecordObject(inRect, "Resizing RectTransform");
            }
            #endif // UNITY_EDITOR
            
            float aspectRatio = inWidth / inHeight;
            Vector2 size = inRect.sizeDelta;
            size.x = size.y * aspectRatio;
            inRect.sizeDelta = size;

            #if UNITY_EDITOR
            if (!Application.IsPlaying(inRect)) {
                UnityEditor.EditorUtility.SetDirty(inRect);
            }
            #endif // UNITY_EDITOR
        }

        static public void ResizeYForAspectRatio(this RectTransform inRect, float inWidth, float inHeight) {
            #if UNITY_EDITOR
            if (!Application.IsPlaying(inRect)) {
                UnityEditor.Undo.RecordObject(inRect, "Resizing RectTransform");
            }
            #endif // UNITY_EDITOR
            
            float aspectRatio = inHeight / inWidth;
            Vector2 size = inRect.sizeDelta;
            size.y = size.x * aspectRatio;
            inRect.sizeDelta = size;

            #if UNITY_EDITOR
            if (!Application.IsPlaying(inRect)) {
                UnityEditor.EditorUtility.SetDirty(inRect);
            }
            #endif // UNITY_EDITOR
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
            Assert.True(inTransform.parent == inRect.content);
            float transformY = inTransform.rect.center.y;
            float totalHeight = inRect.content.rect.height;
            inRect.verticalNormalizedPosition = transformY / totalHeight;
        }

        static public bool IsVisible(this ScrollRect inRect, RectTransform inTransform, Vector2 inPadding) {
            return IsVisible(inRect.viewport, inTransform, inPadding);
        }

        static public bool IsVisible(this ScrollRect inRect, RectTransform inTransform) {
            return IsVisible(inRect, inTransform, s_DefaultVisiblePadding);
        }

        static public bool IsVisible(RectTransform inParent, RectTransform inChild) {
            return IsVisible(inParent, inChild, s_DefaultVisiblePadding);
        }

        static public bool IsVisible(RectTransform inParent, RectTransform inTransform, Vector2 inPadding) {
            RectTransform viewport = inParent;
            Assert.True(inTransform.IsChildOf(viewport));

            Rect clipping = inParent.rect;
            clipping.size += inPadding;

            Rect transformBox = inTransform.rect;
            Vector2 transformPos = transformBox.position + (Vector2) inTransform.localPosition;
            
            RectTransform check = inTransform.parent as RectTransform;
            Vector2 offset;
            while(check && check != viewport) {
                offset = check.localPosition;
                transformPos += offset;
                check = check.parent as RectTransform;
            }

            transformBox.position = transformPos;
            return transformBox.Overlaps(clipping);
        }

        static private readonly Vector2 s_DefaultVisiblePadding = new Vector2(8, 8);
    }
}