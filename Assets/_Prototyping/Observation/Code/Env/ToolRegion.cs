using System;
using Aqua;
using Aqua.Entity;
using Aqua.Scripting;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace ProtoAqua.Observation {
    public abstract class ToolRegion : ScriptComponent, IBaked {

        #region Inspector

        [Header("Position")]
        public Collider2D Collider;
        public Visual2DTransform ColliderPosition;
        public Transform TrackTransform;

        #endregion // Inspector

        protected virtual void Awake() {
            ColliderPosition.Source = SafeTrackedTransform;
        }

        public Transform SafeTrackedTransform {
            get { return TrackTransform ? TrackTransform : transform; }
        }

        #if UNITY_EDITOR

        protected virtual void Reset() {
            TrackTransform = transform;
        }

        protected virtual void OnValidate() {
            if (Collider) {
                ColliderPosition = Collider.GetComponent<Visual2DTransform>();
            }
        }

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            Baking.ResetStaticFlags(gameObject, true);

            bool bChanged = CustomBake();

            if (!TrackTransform) {
                TrackTransform = transform;
                bChanged = true;
            }

            return bChanged;
        }

        protected virtual bool CustomBake() { return false; }

        #endif // UNITY_EDITOR
    }
}