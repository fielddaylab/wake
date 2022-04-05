using System;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Observation {
    public class FlashlightRegion : ToolRegion {
        public delegate void Callback(FlashlightRegion region);

        #region Inspector

        public ActiveGroup Reveal = new ActiveGroup();
        public ActiveGroup Hidden = new ActiveGroup();

        #endregion // Inspector

        [NonSerialized] public int LightCount = 0;
        public Callback OnLit;
        public Callback OnUnlit;

        protected override void Awake() {
            base.Awake();
        }

        private void Start() {
            Reveal.ForceActive(false);
            Hidden.ForceActive(true);
        }

        #if UNITY_EDITOR

        protected override void Reset() {
            base.Reset();

            TrackTransform = transform;
            var scanRegion = transform.parent.GetComponentInChildren<ScannableRegion>();
            if (scanRegion) {
                ArrayUtils.Add(ref Reveal.GameObjects, scanRegion.gameObject);
                TrackTransform = scanRegion.SafeTrackedTransform;
            }
        }

        [ContextMenu("Auto Configure")]
        internal void AutoConfigure() {
            var scanRegions = transform.parent.GetComponentsInChildren<ScannableRegion>();
            var scanRegion = Array.Find(scanRegions, (t) => !t.name.Contains("MicroscopeHint"));
            var microscopeRegion = transform.parent.GetComponentInChildren<MicroscopeRegion>();
            UnityEditor.Undo.RecordObject(this, "Auto configure FlashlightRegion");
            if (scanRegion) {
                TrackTransform = scanRegion.SafeTrackedTransform;
            }

            if (microscopeRegion) {
                microscopeRegion.TrackTransform = TrackTransform;
                if (!ArrayUtils.Contains(Reveal.GameObjects, microscopeRegion.gameObject)) {
                    ArrayUtils.Add(ref Reveal.GameObjects, microscopeRegion.gameObject);
                }
                if (scanRegion) {
                    ArrayUtils.Remove(ref Reveal.GameObjects, scanRegion.gameObject);
                }
                microscopeRegion.AutoConfigure();
            } else if (scanRegion) {
                if (!ArrayUtils.Contains(Reveal.GameObjects, scanRegion.gameObject)) {
                    ArrayUtils.Add(ref Reveal.GameObjects, scanRegion.gameObject);
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }

        #endif // UNITY_EDITOR
    }
}