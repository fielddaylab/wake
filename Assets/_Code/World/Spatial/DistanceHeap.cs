using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Cameras;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua.Spatial {
    static public class DistanceHeap {
        public const float RebaseThreshold = 128;

        public delegate void ActivateDeactivateDelegate<T>(T item, bool state, DistanceHeapActivationParams parameters);

        static private int Parent(int index) {
            return (index - 1) >> 1;
        }

        static private int Left(int index) {
            return (index << 1) + 1;
        }

        static private int Right(int index) {
            return (index << 1) + 2;
        }

        static public void Insert<T>(RingBuffer<DistanceHeapItem<T>> buffer, DistanceHeapItem<T> item) {
            buffer.PushBack(item);
            ReheapifyUp(buffer, buffer.Count - 1);
        }

        static public void Delete<T>(RingBuffer<DistanceHeapItem<T>> buffer, int index) {
            buffer.FastRemoveAt(index);
            if (index == buffer.Count) {
                return;
            }

            int parentIdx = Parent(index);
            if (index == 0 || buffer[parentIdx].Score < buffer[index].Score) {
                ReheapifyDown(buffer, index);
            } else {
                ReheapifyUp(buffer, index);
            }
        }

        static public void ReheapifyUp<T>(RingBuffer<DistanceHeapItem<T>> buffer, int index) {
            int key = index;
            int parent = Parent(key);
            while(key != 0 && buffer[key].Score < buffer[parent].Score) {
                Ref.Swap(ref buffer[key], ref buffer[parent]);
                key = parent;
                parent = Parent(key);
            }
        }

        static public void ReheapifyDown<T>(RingBuffer<DistanceHeapItem<T>> buffer, int index) {
            int key = index,
                left, right,
                size = buffer.Count;

            int smallest = key;
            while(true) {
                left = Left(key);
                right = Right(key);

                if (left < size && buffer[left].Score < buffer[smallest].Score) {
                    smallest = left;
                }
                if (right < size && buffer[right].Score < buffer[smallest].Score) {
                    smallest = right;
                }

                if (smallest == key) {
                    break;
                }

                Ref.Swap(ref buffer[key], ref buffer[smallest]);
                key = smallest;
            }
        }
    }

    public struct DistanceHeapActivationParams {
        public ushort FrameIndex;
        public Vector3 Visual2DPosition;
        public float Visual2DDistanceScale;

        public DistanceHeapActivationParams(ushort frameIndex) {
            FrameIndex = frameIndex;
            Visual2DPosition = default;
            Visual2DDistanceScale = 1;
        }
    }

    public class DistanceHeap<T> : IReadOnlyCollection<T> {
        private readonly RingBuffer<DistanceHeapItem<T>> m_Heap = new RingBuffer<DistanceHeapItem<T>>(32, RingBufferMode.Expand);
        private readonly RingBuffer<T> m_ActiveList = new RingBuffer<T>(32, RingBufferMode.Expand);
        private readonly IEqualityComparer<T> m_Comparer = EqualityComparer<T>.Default;
        private readonly DistanceHeap.ActivateDeactivateDelegate<T> m_ActivateDelegate;

        private float m_ActivationRadius;
        private Vector3 m_LastKnownPosition;
        private float m_ScoreAccumulator;

        public DistanceHeap(float inActivationRadius, DistanceHeap.ActivateDeactivateDelegate<T> activateDeactivate) {
            m_ActivationRadius = inActivationRadius;
            m_ActivateDelegate = activateDeactivate;
        }

        #region Radius

        public float Radius() {
            return m_ActivationRadius;
        }

        public void SetRadius(float radius) {
            if (radius == m_ActivationRadius) {
                return;
            }

            float diff = m_ActivationRadius - radius;
            m_ActivationRadius = radius;

            for(int i = 0, len = m_Heap.Count; i < len; i++) {
                m_Heap[i].Score -= diff;
            }
        }

        #endregion // Radius

        #region Collection

        public void Add(T item, Transform transform, float radius = 0) {
            DistanceHeapItem<T> heapItem;
            heapItem.Item = item;
            heapItem.Radius = radius;
            heapItem.Transform = transform;
            heapItem.Score = m_ScoreAccumulator;
            heapItem.State = false;
            heapItem.Visual2DTransform = transform.GetComponent<Visual2DTransform>();
            DistanceHeap.Insert(m_Heap, heapItem);
        }

        public void Add(T item, Visual2DTransform transform, float radius = 0) {
            DistanceHeapItem<T> heapItem;
            heapItem.Item = item;
            heapItem.Radius = radius;
            heapItem.Transform = transform.Source;
            heapItem.Score = m_ScoreAccumulator;
            heapItem.State = false;
            heapItem.Visual2DTransform = transform;
            DistanceHeap.Insert(m_Heap, heapItem);
        }

        public bool Remove(T item) {
            for(int i = 0, len = m_Heap.Count; i < len; i++) {
                if (m_Comparer.Equals(m_Heap[i].Item, item)) {
                    TrySetActiveState(ref m_Heap[i], false, new DistanceHeapActivationParams(Frame.Index));
                    DistanceHeap.Delete(m_Heap, i);
                    return true;
                }
            }

            return false;
        }

        public void Clear() {
            DistanceHeapActivationParams parms = new DistanceHeapActivationParams(Frame.Index);
            for(int i = 0, len = m_Heap.Count; i < len; i++) {
                TrySetActiveState(ref m_Heap[i], false, parms);
            }
            m_Heap.Clear();
            m_ActiveList.Clear();
        }

        public int Count { get { return m_Heap.Count; } }

        public IEnumerator<T> GetEnumerator() {
            foreach(var item in m_Heap) {
                yield return item.Item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion // Collection

        #region Updates

        public void Update(ushort frameIndex, Vector3 position) {
            float distance = Vector3.Distance(position, m_LastKnownPosition);
            AccumulateScore(distance, position);

            if (m_Heap.Count > 0) {
                while(true) {
                    ref var heapItem = ref m_Heap[0];
                    if (heapItem.Score >= m_ScoreAccumulator) {
                        break;
                    }

                    float itemDistance = DistanceToItem3D(ref heapItem, position);
                    TrySetActiveState(ref heapItem, itemDistance <= 0, new DistanceHeapActivationParams(frameIndex));
                    heapItem.Score = m_ScoreAccumulator + Math.Abs(itemDistance);
                    DistanceHeap.ReheapifyDown(m_Heap, 0);
                }
            }
        }

        public void Update2D(ushort frameIndex, Vector2 position) {
            float distance = Vector2.Distance(position, m_LastKnownPosition);
            AccumulateScore(distance, position);

            if (m_Heap.Count > 0) {
                while(true) {
                    ref var heapItem = ref m_Heap[0];
                    if (heapItem.Score >= m_ScoreAccumulator) {
                        break;
                    }

                    float itemDistance = DistanceToItem2D(ref heapItem, position);
                    TrySetActiveState(ref heapItem, itemDistance <= 0, new DistanceHeapActivationParams(frameIndex));
                    heapItem.Score = m_ScoreAccumulator + Math.Abs(itemDistance);
                    DistanceHeap.ReheapifyDown(m_Heap, 0);
                }
            }
        }

        public void UpdateVisual2D(ushort frameIndex, Vector2 position, in CameraService.PlanePositionHelper positionHelper) {
            float distance = Vector2.Distance(position, m_LastKnownPosition);
            AccumulateScore(distance, position);

            if (m_Heap.Count > 0) {
                while(true) {
                    ref var heapItem = ref m_Heap[0];
                    // Debug.DrawLine(heapItem.Transform.position, heapItem.Transform.position + Vector3.up * 2, Color.red, 0.1f);
                    if (heapItem.Score >= m_ScoreAccumulator) {
                        break;
                    }

                    DistanceHeapActivationParams parms = new DistanceHeapActivationParams(frameIndex);
                    float itemDistance = DistanceToItemVisual2D(ref heapItem, position, positionHelper, ref parms);
                    TrySetActiveState(ref heapItem, itemDistance <= 0, parms);

                    // scale score by distance to account for distance parallax effect
                    heapItem.Score = m_ScoreAccumulator + Math.Abs(itemDistance * parms.Visual2DDistanceScale);
                    Log.Msg("[DistanceHeap] Requeueing item {0} further {1} (distance {2}, distance scale {3})", heapItem.Item, heapItem.Score - m_ScoreAccumulator, itemDistance, parms.Visual2DDistanceScale);
                    DistanceHeap.ReheapifyDown(m_Heap, 0);
                }
            }
        }

        #endregion // Updates

        #region Distance

        private void AccumulateScore(float score, Vector3 newPosition) {
            m_ScoreAccumulator += score;
            m_LastKnownPosition = newPosition;

            if (m_ScoreAccumulator >= DistanceHeap.RebaseThreshold) {
                for(int i = 0, len = m_Heap.Count; i < len; i++) {
                    m_Heap[i].Score -= DistanceHeap.RebaseThreshold;
                }
                m_ScoreAccumulator -= DistanceHeap.RebaseThreshold;
            }
        }

        private float DistanceToItem3D(ref DistanceHeapItem<T> heapItem, Vector3 position) {
            return Vector3.Distance(position, heapItem.Transform.position) - m_ActivationRadius - heapItem.Radius;
        }

        private float DistanceToItem2D(ref DistanceHeapItem<T> heapItem, Vector3 position) {
            return Vector2.Distance((Vector2) position, (Vector2) heapItem.Transform.position) - m_ActivationRadius - heapItem.Radius;
        }

        private float DistanceToItemVisual2D(ref DistanceHeapItem<T> heapItem, Vector3 position, in CameraService.PlanePositionHelper positionHelper, ref DistanceHeapActivationParams parameters) {
            parameters.Visual2DPosition = positionHelper.CastToPlane(heapItem.Transform, out parameters.Visual2DDistanceScale);
            // Debug.DrawLine(heapItem.Transform.position, heapItem.Transform.position + Vector3.up, Color.yellow, 0.5f);
            return Vector2.Distance((Vector2) position, (Vector2) parameters.Visual2DPosition) - m_ActivationRadius - heapItem.Radius;
        }

        #endregion // Distance

        #region Active

        public IReadOnlyList<T> ActiveItems() {
            return m_ActiveList;
        }

        public void DeactivateAll() {
            DistanceHeapActivationParams parms = new DistanceHeapActivationParams(Frame.Index);
            for(int i = 0, len = m_Heap.Count; i < len; i++) {
                TrySetActiveState(ref m_Heap[i], false, parms);
            }
        }

        private void TrySetActiveState(ref DistanceHeapItem<T> heapItem, bool active, DistanceHeapActivationParams parameters) {
            if (heapItem.State == active) {
                return;
            }

            heapItem.State = active;

            if (active) {
                m_ActiveList.PushBack(heapItem.Item);
                if (!heapItem.Visual2DTransform.IsReferenceNull()) {
                    heapItem.Visual2DTransform.WritePosition(parameters.FrameIndex, parameters.Visual2DPosition, parameters.Visual2DDistanceScale);
                }
                m_ActivateDelegate(heapItem.Item, true, parameters);
            } else {
                m_ActiveList.FastRemove(heapItem.Item);
                if (!heapItem.Visual2DTransform.IsReferenceNull()) {
                    heapItem.Visual2DTransform.Wipe();
                }
                m_ActivateDelegate(heapItem.Item, false, parameters);
            }
        }

        #endregion // Active
    }
}