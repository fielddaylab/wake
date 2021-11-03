using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Cameras;
using BeauUtil;
using UnityEngine;

namespace Aqua.Spatial {
    public struct Visual2DPositionUpdate<T> : IEnumerable<T>
        where T : MonoBehaviour, IVisual2DObjectSource {
        public delegate void UpdateMethod(T item);

        public RingBuffer<T> DynamicObjects;
        public DistanceHeap<T> StaticObjects;
        public UpdateMethod UpdateActive;

        public void Add(T item, float radius) {
            switch(item.VisualTransform().PositionType) {
                case PositionMode.Static: {
                    StaticObjects.Add(item, item.VisualTransform(), radius);
                    break;
                }

                case PositionMode.Dynamic: {
                    DynamicObjects.PushBack(item);
                    break;
                }
            }
        }

        public bool Remove(T item) {
            switch(item.VisualTransform().PositionType) {
                case PositionMode.Static: {
                    return StaticObjects.Remove(item);
                }

                case PositionMode.Dynamic: {
                    return DynamicObjects.FastRemove(item);
                }

                default: {
                    return false;
                }
            }
        }

        public void Clear() {
            StaticObjects.Clear();
            DynamicObjects.Clear();
        }

        public void DeactivateAll() {
            StaticObjects.DeactivateAll();
        }

        public void ProcessObjects(ushort frameIndex, Vector2 cameraPosition, in CameraService.PlanePositionHelper positionHelper) {
            StaticObjects.UpdateVisual2D(frameIndex, cameraPosition, positionHelper);

            foreach(var obj in StaticObjects.ActiveItems()) {
                ProcessObject(obj, frameIndex, positionHelper);
            }
            foreach(var obj in DynamicObjects) {
                ProcessObject(obj, frameIndex, positionHelper);
            }
        }

        private void ProcessObject(T obj, ushort frameIndex, in CameraService.PlanePositionHelper positionHelper) {
            Visual2DTransform transform = obj.VisualTransform();
            transform.CalculatePosition(frameIndex, positionHelper);
            transform.Apply();
            UpdateActive(obj);
        }

        #region IEnumerable

        public IEnumerator<T> GetEnumerator() {
            foreach(var obj in StaticObjects) {
                yield return obj;
            }
            foreach(var obj in DynamicObjects) {
                yield return obj;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion // IEnumerable
    }
}