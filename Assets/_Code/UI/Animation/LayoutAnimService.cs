using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Services;
using BeauUtil.Tags;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class LayoutAnimService : ServiceBehaviour {
        private readonly RingBuffer<ILayoutAnim> m_Animators = new RingBuffer<ILayoutAnim>(64);

        public void Add(ILayoutAnim animator) {
            m_Animators.PushBack(animator);
        }

        public void TryAdd(ILayoutAnim animator, ref float timer, float desiredTime) {
            bool hadTime = timer > 0;
            timer = desiredTime;
            if (!hadTime && animator.IsActive()) {
                Add(animator);
            }
        }

        public void TryAdd(ILayoutAnim animator, float timer) {
            if (timer > 0 && animator.IsActive()) {
                Add(animator);
            }
        }

        public void Remove(ILayoutAnim animator) {
            m_Animators.FastRemove(animator);
        }

        private void LateUpdate() {
            if (!Services.Valid)
                return;
            
            float dt = Time.deltaTime;
            int idx = 0;
            while(idx < m_Animators.Count) {
                if (!m_Animators[idx].OnAnimUpdate(dt)) {
                    m_Animators.FastRemoveAt(idx);
                } else {
                    idx++;
                }
            }
        }
    }
}