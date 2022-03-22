using System;
using Aqua;
using Aqua.Entity;
using Aqua.Scripting;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace ProtoAqua.Observation {
    public class TaggableCritter : ScriptComponent, IBaked {

        #region Inspector

        [FilterBestiaryId(BestiaryDescCategory.Critter)] public SerializedHash32 CritterId;
        [Required] public Collider2D Collider;
        [Required] public Visual2DTransform ColliderPosition;
        public Transform TrackTransform;

        #endregion // Inspector

        [NonSerialized] public bool WasTagged;

        private void Awake() {
            ColliderPosition.Source = TrackTransform ? TrackTransform : transform;
        }

        private void OnEnable() {
            ScanSystem.Find<TaggingSystem>().Register(this);
        }

        private void OnDisable() {
            if (!Services.Valid || !Collider) {
                return;
            }
            ScanSystem.Find<TaggingSystem>()?.Deregister(this);
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
            if (!TrackTransform)
                TrackTransform = transform;

            return true;
        }

#endif // UNITY_EDITOR
    }
}