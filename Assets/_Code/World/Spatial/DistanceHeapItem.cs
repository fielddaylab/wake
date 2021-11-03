using UnityEngine;

namespace Aqua.Spatial {
    public struct DistanceHeapItem<T> {
        public float Score;
        public float Radius;

        public Transform Transform;
        public Visual2DTransform Visual2DTransform;
        
        public bool State;
        public T Item;

        public override string ToString() {
            return string.Format("{0}: {1} ({2})", Item, Score, State ? "[active]" : "[sleeping]");
        }
    }
}