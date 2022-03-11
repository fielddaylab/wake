using System;
using UnityEngine;
using BeauUtil;
using Aqua.Scripting;
using Aqua;
using ScriptableBake;

namespace ProtoAqua.Observation
{
    public class ScannableRegion : ScriptComponent, IBaked
    {
        #region Inspector

        public SerializedHash32 ScanId;
        [Required] public Collider2D Collider;
        public Transform TrackTransform;
        [Space]
        public ToolView ToolView;
        [SerializeField, HideInInspector] public bool InsideToolView;
        [Space]

        #endregion // Inspector

        public ScanData ScanData;
        [NonSerialized] public ScanIcon CurrentIcon;
        [NonSerialized] public bool InRange;

        private void OnEnable()
        {
            ScanSystem.Find<ScanSystem>().Register(this);
        }

        private void OnDisable()
        {
            ScanSystem.Find<ScanSystem>()?.Deregister(this);
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

            InsideToolView = this.GetComponentInParent<ToolView>(true);
            return true;
        }

        #endif // UNITY_EDITOR
    }
}