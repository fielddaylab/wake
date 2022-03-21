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
        [AutoEnum] public ScannableStatusFlags Required;

        #endregion // Inspector

        public ScanData ScanData;
        [NonSerialized] public ScannableStatusFlags Current;
        [NonSerialized] public ScanIcon CurrentIcon;
        [NonSerialized] public bool CanScan;

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
            {
                TrackTransform = transform;
                return true;
            }

            return false;
        }

        #endif // UNITY_EDITOR
    }

    [Flags]
    public enum ScannableStatusFlags {
        [Hidden] InRange = 0x01,
        Flashlight = 0x02,
        Microscope = 0x04
    }
}