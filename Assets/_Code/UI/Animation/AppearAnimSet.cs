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
        
        [Header("Chaining")]
        [SerializeField] private AppearAnimSet m_NextSet = null;

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

        public float Play(float delay = 0) {
            float totalDelay = AppearAnim.PingGroup(m_Anims, delay + m_InitialDelay, m_IntervalScale, m_ClippingRegion);
            if (m_NextSet) {
                totalDelay = m_NextSet.Play(totalDelay);
            }
            return totalDelay;
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