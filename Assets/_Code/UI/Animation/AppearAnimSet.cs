using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil.Debugger;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua {
    public class AppearAnimSet : MonoBehaviour {
        [SerializeField] private List<AppearAnim> m_Anims = new List<AppearAnim>();
        [SerializeField] private float m_IntervalScale = 0.2f;
        [SerializeField] private float m_InitialDelay = 0;
        [SerializeField] private bool m_PlayOnEnable = true;
        [SerializeField] private RectTransform m_ClippingRegion = null;

        private readonly Action m_PlayDelegate;

        private AppearAnimSet() {
            m_PlayDelegate = AsyncPlay;
        }

        private void OnEnable() {
            if (m_PlayOnEnable && !Script.IsLoading) {
                Async.InvokeAsync(m_PlayDelegate);
            }
        }

        private void AsyncPlay() {
            if (!this || !isActiveAndEnabled) {
                return;
            }

            Play(0);
        }

        public void Play(float delay = 0) {
            using(Profiling.Sample("appear anim init")) {
                bool hasClipping = m_ClippingRegion;
                delay += m_InitialDelay;
                for(int i = 0; i < m_Anims.Count; i++) {
                    AppearAnim anim = m_Anims[i];
                    if (anim.isActiveAndEnabled && (!hasClipping || CanvasExtensions.IsVisible(m_ClippingRegion, (RectTransform) anim.transform))) {
                        delay += anim.Ping(delay) * m_IntervalScale;
                    }
                }
            }
        }

        [ContextMenu("Find All Children")]
        private void LoadAll() {
            #if UNITY_EDITOR
            Undo.RecordObject(this, "Gathering all AppearAnim children");
            EditorUtility.SetDirty(this);
            #endif // UNITY_EDITOR
            GetComponentsInChildren<AppearAnim>(true, m_Anims);
            m_Anims.Sort((a, b) => (int) Math.Sign(b.transform.position.y - a.transform.position.y));
        }
    }
}