using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua.Modeling {
    public unsafe class SimTargetGraph : MonoBehaviour {
        [Serializable] private class TargetPool : SerializablePool<RectGraphic> { }

        #region Inspector

        [SerializeField] private TargetPool m_TargetPool = null;

        #endregion // Inspector

        private RingBuffer<Vector2> m_TargetRangesAbs = new RingBuffer<Vector2>(8, RingBufferMode.Fixed);

        public void Clear() {
            m_TargetPool.Reset();
            m_TargetRangesAbs.Clear();
        }

        public void LoadTargets(JobModelScope scope) {
            Assert.NotNull(scope);

            m_TargetPool.Reset();
            m_TargetRangesAbs.Clear();

            foreach(var target in scope.InterventionTargets) {
                BestiaryDesc targetEntry = Assets.Bestiary(target.Id);
                RectGraphic graphic = m_TargetPool.Alloc();
                graphic.color = targetEntry.Color();

                m_TargetRangesAbs.PushBack(new Vector2(target.Population - target.Range, target.Population + target.Range));
            }
        }

        public void RenderTargets(Rect inRange) {
            var activeObjects = m_TargetPool.ActiveObjects;
            for(int i = 0; i < activeObjects.Count; i++) {
                RectGraphic target = activeObjects[i];
                Vector2 range = m_TargetRangesAbs[i];

                RectTransform transform = target.rectTransform;

                Vector2 min, max;
                min = transform.anchorMin;
                max = transform.anchorMax;

                min.y = range.x / inRange.height;
                max.y = range.y / inRange.height;

                transform.anchorMin = min;
                transform.anchorMax = max;
            }
        }
    }
}