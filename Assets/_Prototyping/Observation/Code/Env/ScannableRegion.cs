using System;
using Aqua;
using Aqua.Entity;
using Aqua.Scripting;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace ProtoAqua.Observation {
    public class ScannableRegion : ToolRegion {
        #region Inspector

        [Header("Scannable")]
        public SerializedHash32 ScanId;
        public Visual2DTransform Click;
        public Transform IconRootOverride;

        #endregion // Inspector

        public ScanData ScanData;
        [NonSerialized] public ScanIcon CurrentIcon;
        [NonSerialized] public bool CanScan;
        [NonSerialized] public bool InMicroscope;
    }
}