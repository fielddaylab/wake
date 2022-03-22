using System;
using Aqua;
using Aqua.Entity;
using Aqua.Scripting;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace ProtoAqua.Observation {
    public class ScannableRegion : ScriptComponent, IBaked {
        #region Inspector

        public SerializedHash32 ScanId;
        [Required] public Collider2D Collider;
        [Required] public Visual2DTransform ColliderPosition;
        public Transform TrackTransform;

        #endregion // Inspector

        public ScanData ScanData;
        [NonSerialized] public ScanIcon CurrentIcon;
        [NonSerialized] public bool CanScan;

        private void Awake() {
            ColliderPosition.Source = TrackTransform ? TrackTransform : transform;
        }

        #if UNITY_EDITOR

        private void Reset() {
            TrackTransform = transform;
        }

        private void OnValidate() {
            ColliderPosition = Collider.GetComponent<Visual2DTransform>();
        }

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags) {
            if (!TrackTransform) {
                TrackTransform = transform;
                return true;
            }

            return false;
        }

        #endif // UNITY_EDITOR
    }
}