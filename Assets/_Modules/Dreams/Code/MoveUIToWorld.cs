using System;
using System.Collections;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace Aqua.Dreams {
    public class MoveUIToWorld : ScriptComponent {
        [Required] public RectTransformPinned Pin;
        public float PinZOffset = -10;

        [Header("Animation")]
        public CanvasGroup FadeGroup;
        public float FadeDelay = 3;
        public float FadeDuration = 8;
        public Vector2 FadeDistance = new Vector2(-30, 30);
        public float FadeRotation = 80;

        [NonSerialized] private bool m_Triggered = false;
        private Routine m_Routine;

        private void Awake() {
            Pin.enabled = false;
        }

        [LeafMember("PlayAnimation"), Preserve]
        public void Activate() {
            if (m_Triggered) {
                return;
            }

            m_Triggered = true;
            transform.SetParent(null, false);
            transform.SetPositionAndRotation(GenerateWorldPosition(Pin, PinZOffset), Quaternion.identity);
            Pin.Pin(transform);
            Pin.enabled = true;

            if (FadeGroup != null && FadeDelay > 0) {
                m_Routine.Replace(this, FadeAnimation());
            }
        }

        private IEnumerator FadeAnimation() {
            yield return FadeDelay;
            RectTransform groupRect = null;
            FadeGroup.CacheComponent(ref groupRect);
            yield return Routine.Combine(
                FadeGroup.FadeTo(0, FadeDuration),
                groupRect.AnchorPosTo(groupRect.anchoredPosition + FadeDistance, FadeDuration),
                groupRect.RotateTo(groupRect.localEulerAngles.z + FadeRotation, FadeDuration, Axis.Z, Space.Self, AngleMode.Absolute)
            );
            groupRect.gameObject.SetActive(false);
        }

        static private Vector3 GenerateWorldPosition(RectTransformPinned pin, float inPinOffset) {
            Vector2 screen = TransformHelper.ScreenPosition(pin.transform, Services.UI.Camera);
            return Services.Camera.ScreenToGameplayPosition(screen, inPinOffset);
        }
    }
}