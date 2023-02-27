using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua {
    [RequireComponent(typeof(LayoutOffset))]
    public class AppearAnim : MonoBehaviour, ILayoutAnim
    {
        private const float AnimDistance = -4;
        private const float AnimDuration = 8 / 60f;

        [SerializeField, Range(0.1f, 3)] private float m_AnimDurationScale = 1;
        [SerializeField, Range(0.1f, 3)] private float m_AnimDistanceScale = 1;
        [SerializeField] private bool m_DisableRaycasts = false;
        [SerializeField] private bool m_PlayOnEnable = false;

        [NonSerialized] private float m_TimeLeft;
        [NonSerialized] private LayoutOffset m_Offset;
        [NonSerialized] private CanvasGroup m_CanvasGroup;
        [NonSerialized] private CanvasRenderer m_CanvasRenderer;

        [NonSerialized] private bool m_Awoken = false;

        private void Awake() {
            if (!m_Awoken) {
                m_Offset = GetComponent<LayoutOffset>();
                if (!TryGetComponent(out m_CanvasGroup)) {
                    if (TryGetComponent(out m_CanvasRenderer)) {
                        m_CanvasRenderer.cullTransparentMesh = true;
                    }
                }
                m_Awoken = true;
            }
        }

        private void OnEnable() {
            if (m_PlayOnEnable) {
                Ping();
            } else {
                Services.Animation.Layout?.TryAdd(this, m_TimeLeft);
            }
        }

        private void OnDisable() {
            if (Services.Valid && m_TimeLeft > 0) {
                m_TimeLeft = 0;

                if (m_CanvasGroup) {
                    m_CanvasGroup.alpha = 1;
                    if (m_DisableRaycasts) {
                        m_CanvasGroup.blocksRaycasts = true;
                    }
                } else if (m_CanvasRenderer) {
                    m_CanvasRenderer.SetAlpha(1);
                }

                m_Offset.Offset0 = default(Vector2);

                Services.Animation.Layout.Remove(this);
            }
        }

        public void Hide() {
            if (!m_Awoken) {
                Awake();
            }

            if (m_CanvasGroup) {
                m_CanvasGroup.alpha = 0;
                if (m_DisableRaycasts) {
                    m_CanvasGroup.blocksRaycasts = false;
                }
            } else if (m_CanvasRenderer) {
                m_CanvasRenderer.SetAlpha(0);
            }

            if (m_TimeLeft > 0) {
                m_TimeLeft = 0;
                Services.Animation.Layout.Remove(this);
            }
        }

        private void Apply(float percent) {
            float alpha = percent;
            float pos = 1 - (Math.Abs(percent - 0.5f) * 2);

            if (m_CanvasGroup) {
                m_CanvasGroup.alpha = alpha;
                if (m_DisableRaycasts) {
                    m_CanvasGroup.blocksRaycasts = percent >= 1;
                }
            } else if (m_CanvasRenderer) {
                m_CanvasRenderer.SetAlpha(alpha);
            }

            m_Offset.Offset0 = new Vector2(0, m_AnimDistanceScale * AnimDistance * pos);
        }

        bool ILayoutAnim.OnAnimUpdate(float deltaTime) {
            m_TimeLeft = Math.Max(0, m_TimeLeft - deltaTime);
            float percent = 1 - (m_TimeLeft / (m_AnimDurationScale * AnimDuration));
            if (percent >= 0) {
                Apply(percent);
            }
            return m_TimeLeft > 0;
        }

        bool ILayoutAnim.IsActive() {
            return isActiveAndEnabled;
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Ping() {
            return Ping(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Ping(float delay) {
            float duration = (AnimDuration * m_AnimDurationScale);
            Services.Animation.Layout.TryAdd(this, ref m_TimeLeft, duration + delay);
            if (object.ReferenceEquals(m_Offset, null)) {
                Awake();
            }
            Apply(0);
            return duration;
        }

        static public float PingGroup(AppearAnim[] anims, float delay, float intervalScale, RectTransform clipping = null) {
            bool hasClipping = clipping;
            for(int i = 0; i < anims.Length; i++) {
                AppearAnim anim = anims[i];
                if (anim.isActiveAndEnabled && (!hasClipping || CanvasExtensions.IsVisible(clipping, (RectTransform) anim.transform))) {
                    delay += anim.Ping(delay) * intervalScale;
                }
            }
            return delay;
        }

        static public float PingGroup(List<AppearAnim> anims, float delay, float intervalScale, RectTransform clipping = null) {
            bool hasClipping = clipping;
            for(int i = 0; i < anims.Count; i++) {
                AppearAnim anim = anims[i];
                if (anim.isActiveAndEnabled && (!hasClipping || CanvasExtensions.IsVisible(clipping, (RectTransform) anim.transform))) {
                    delay += anim.Ping(delay) * intervalScale;
                }
            }
            return delay;
        }

        static public int FindGroup(Transform root, List<AppearAnim> group, bool recursive = false) {
            int childCount = root.childCount;
            int animCount = 0;
            for(int i = 0; i < childCount; i++) {
                Transform child = root.GetChild(i);
                AppearAnim anim = child.GetComponent<AppearAnim>();
                if (anim) {
                    group.Add(anim);
                    animCount++;
                } else if (recursive) {
                    animCount += FindGroup(child, group, true);
                }
            }
            return animCount;
        }

        static public float PingChildren(Transform animRoot, bool recursive, float delay, float intervalScale, RectTransform clipping = null) {
            if (animRoot.gameObject.activeInHierarchy) {
                bool hasClipping = clipping;
                int childCount = animRoot.childCount;
                for(int i = 0; i < childCount; i++) {
                    Transform child = animRoot.GetChild(i);
                    AppearAnim anim = child.GetComponent<AppearAnim>();
                    if (anim) {
                        if (anim.isActiveAndEnabled && (!hasClipping || CanvasExtensions.IsVisible(clipping, (RectTransform) child))) {
                            delay += anim.Ping(delay) * intervalScale;
                        }
                    } else if (recursive) {
                        delay = PingChildren(child, true, delay, intervalScale, clipping);
                    }
                }
            }
            return delay;
        }
    }
}