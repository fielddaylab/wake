using System;
using Aqua;
using Aqua.Entity;
using Aqua.Scripting;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace ProtoAqua.Observation {
    public class ScannableRegion : ToolRegion, IEditorOnlyData {
        public delegate void ScanStartDelegate(bool wasScanned);
        public delegate void ScanCompleteDelegate(ScanResult result);

        #region Inspector

        [Header("Scannable")]
        public SerializedHash32 ScanId;
        public Visual2DTransform Click;
        public Transform IconRootOverride;
        public float IconZAdjust;

        #endregion // Inspector

        public ScanData ScanData;
        [NonSerialized] public ScanIcon CurrentIcon;
        [NonSerialized] public bool CanScan;
        [NonSerialized] public bool InMicroscope;
        [NonSerialized] public bool ScannedLocal;
        public ScanStartDelegate OnScanStart;
        public ScanCompleteDelegate OnScanComplete;

        #if UNITY_EDITOR

        void IEditorOnlyData.ClearEditorOnlyData() {
            ValidationUtils.StripDebugInfo(ref ScanId);
        }

        #endif // UNITY_EDITOR
    }
}