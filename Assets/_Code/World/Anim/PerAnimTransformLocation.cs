using System;
using BeauUtil;
using UnityEngine;

namespace Aqua.Animation
{
    public sealed class PerAnimTransformLocation : MonoBehaviour {
        [Serializable]
        public struct State {
            public string StateName;
            [NonSerialized] public int StateNameHash;
            public Transform TargetStart;
            public Transform TargetEnd;
        }

        [Required] public Animator Animator;
        public Transform Transform;
        public State[] States;

        [NonSerialized] private int m_LastStateHash;
        [NonSerialized] private int m_LastStateIdx = -1;
        [NonSerialized] private float m_LastStateNormalizedPosition;

        private void Awake() {
            for(int i = 0; i < States.Length; i++) {
                ref State state = ref States[i];
                state.StateNameHash = Animator.StringToHash(state.StateName);
            }

            if (!Transform) {
                Transform = Animator.transform;
            }
        }

        private void LateUpdate() {
            AnimatorStateInfo current = Animator.GetCurrentAnimatorStateInfo(0);
            bool timeUpdated = current.normalizedTime != m_LastStateNormalizedPosition;
            int currentHash = current.shortNameHash;
            if (m_LastStateIdx >= 0 && currentHash == m_LastStateHash) {
                State entry = States[m_LastStateIdx];
                if (timeUpdated && !current.loop && current.normalizedTime == 1) {
                    OnStateEnd(true);
                }
            } else {
                if (currentHash == 0) {
                    OnStateEnd(true);
                } else {
                    for(int i = 0; i < States.Length; i++) {
                        ref State entry = ref States[i];
                        if (entry.StateNameHash == currentHash) {
                            OnStateBegin(i);
                            break;
                        }
                    }

                    if (timeUpdated) {
                        OnStateEnd(true);
                    }
                }
            }

            m_LastStateNormalizedPosition = current.normalizedTime;
        }

        private void OnStateBegin(int newIdx) {
            if (m_LastStateIdx == newIdx) {
                return;
            }

            State state = States[newIdx];
            
            if (!state.TargetStart) {
                OnStateEnd(false);
            } else {
                Transform.SetPositionAndRotation(state.TargetStart.position, state.TargetStart.rotation);
            }

            m_LastStateIdx = newIdx;
            m_LastStateHash = state.StateNameHash;
        }

        private void OnStateEnd(bool close) {
            if (m_LastStateIdx >= 0) {
                State state = States[m_LastStateIdx];
                if (state.TargetEnd) {
                    Transform.SetPositionAndRotation(state.TargetEnd.position, state.TargetEnd.rotation);
                }

                if (close) {
                    m_LastStateHash = 0;
                    m_LastStateIdx = -1;
                }
            }
        }

        #if UNITY_EDITOR
        private void Reset() {
            Animator = GetComponentInChildren<Animator>();
        }
        #endif // UNITY_EDITOR
    }
}