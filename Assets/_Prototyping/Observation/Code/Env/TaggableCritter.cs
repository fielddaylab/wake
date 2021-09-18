using System;
using UnityEngine;
using BeauUtil;
using Aqua.Scripting;
using Aqua;

namespace ProtoAqua.Observation
{
    public class TaggableCritter : ScriptComponent, ISceneOptimizable
    {
        #region Inspector

        public SerializedHash32 CritterId;
        [Required] public Collider2D Collider;
        public Transform TrackTransform;

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

        void ISceneOptimizable.Optimize()
        {
            if (!TrackTransform)
                TrackTransform = transform;
        }

        #endif // UNITY_EDITOR
    }
}