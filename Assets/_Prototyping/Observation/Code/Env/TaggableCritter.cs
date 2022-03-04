using System;
using UnityEngine;
using BeauUtil;
using Aqua.Scripting;
using Aqua;
using ScriptableBake;

namespace ProtoAqua.Observation
{
    public class TaggableCritter : ScriptComponent, IBaked
    {
        #region Inspector

        [FilterBestiaryId(BestiaryDescCategory.Critter)] public SerializedHash32 CritterId;
        [Required] public Collider2D Collider;
        public Transform TrackTransform;

        public float ColliderRadius;

        #endregion // Inspector

        private void OnEnable()
        {
            ScanSystem.Find<TaggingSystem>().Register(this);
        }

        private void OnDisable()
        {
            ScanSystem.Find<TaggingSystem>()?.Deregister(this);
        }

        #if UNITY_EDITOR

        private void Reset()
        {
            TrackTransform = transform;
        }

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags)
        {
            if (!TrackTransform)
                TrackTransform = transform;

            ColliderRadius = Collider != null ? PhysicsUtils.GetRadius(Collider) : 0;
            return true;
        }

        #endif // UNITY_EDITOR
    }
}