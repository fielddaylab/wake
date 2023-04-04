using System;
using Aqua;
using Aqua.Entity;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace ProtoAqua.Observation {
    public class MicroscopeRegion : ToolRegion {
        public delegate void Callback(MicroscopeRegion region);

        #region Inspector

        public ActiveGroup Reveal = new ActiveGroup();
        public ActiveGroup Hidden = new ActiveGroup();
        public ActiveGroup Visuals = new ActiveGroup();
        public Visual2DTransform[] ProjectedTransforms = Array.Empty<Visual2DTransform>();
        public ScannableRegion Scannable = null;
        public ScannableRegion[] AdditionalScannables = null;

        #endregion // Inspector

        public Callback OnViewed;
        public Callback OnUnviewed;

        protected override void Awake() {
            base.Awake();
        }

        private void Start() {
            Reveal.ForceActive(false);
            Hidden.ForceActive(true);
        }

        #if UNITY_EDITOR

        protected override bool CustomBake() {
            bool bChanged = false;

            if (Scannable) {
                Scannable.IconRootOverride = Scannable.Click.transform;
                Baking.SetDirty(Scannable);
                bChanged = true;

                if (!ArrayUtils.Contains(Reveal.GameObjects, Scannable.gameObject)) {
                    ArrayUtils.Add(ref Reveal.GameObjects, Scannable.gameObject);
                }
                if (!ArrayUtils.Contains(ProjectedTransforms, Scannable.Click)) {
                    ArrayUtils.Add(ref ProjectedTransforms, Scannable.Click);
                }
            }

            if (AdditionalScannables != null && AdditionalScannables.Length > 0) {
                foreach(var scannable in AdditionalScannables) {
                    scannable.IconRootOverride = scannable.Click.transform;
                    Baking.SetDirty(scannable);
                    bChanged = true;

                    if (!ArrayUtils.Contains(Reveal.GameObjects, scannable.gameObject)) {
                        ArrayUtils.Add(ref Reveal.GameObjects, scannable.gameObject);
                    }
                    if (!ArrayUtils.Contains(ProjectedTransforms, scannable.Click)) {
                        ArrayUtils.Add(ref ProjectedTransforms, scannable.Click);
                    }
                }
            }

            ValidationUtils.EnsureUnique(ref Reveal.GameObjects);
            ValidationUtils.EnsureUnique(ref Hidden.GameObjects);
            ValidationUtils.EnsureUnique(ref Visuals.GameObjects);
            ValidationUtils.EnsureUnique(ref ProjectedTransforms);

            Reveal.ForceActive(false);

            return bChanged;
        }

        protected override void Reset() {
            base.Reset();

            TrackTransform = transform;
            var scanRegion = transform.parent.GetComponentInChildren<ScannableRegion>();
            if (scanRegion) {
                Scannable = scanRegion;
                ArrayUtils.Add(ref Reveal.GameObjects, scanRegion.gameObject);
                TrackTransform = scanRegion.SafeTrackedTransform;
            }
        }

        [ContextMenu("Auto Configure")]
        internal void AutoConfigure() {
            var scanRegions = transform.parent.GetComponentsInChildren<ScannableRegion>();
            var scanRegion = Array.Find(scanRegions, (t) => !t.name.Contains("MicroscopeHint"));
            var hintRegion = Array.Find(scanRegions, (t) => t != scanRegion);
            UnityEditor.Undo.RecordObject(this, "Auto configure MicroscopeRegion");
            if (scanRegion) {
                Scannable = scanRegion;
                if (!ArrayUtils.Contains(Reveal.GameObjects, scanRegion.gameObject)) {
                    ArrayUtils.Add(ref Reveal.GameObjects, scanRegion.gameObject);
                }
                if (!ArrayUtils.Contains(ProjectedTransforms, scanRegion.Click)) {
                    ArrayUtils.Add(ref ProjectedTransforms, scanRegion.Click);
                }
                scanRegion.IconRootOverride = scanRegion.Click.transform;
                TrackTransform = scanRegion.SafeTrackedTransform;
            }
            if (hintRegion) {
                hintRegion.TrackTransform = TrackTransform;
                if (!ArrayUtils.Contains(Hidden.GameObjects, hintRegion.gameObject)) {
                    ArrayUtils.Add(ref Hidden.GameObjects, hintRegion.gameObject);
                }
            }
            ValidationUtils.EnsureUnique(ref Reveal.GameObjects);
            ValidationUtils.EnsureUnique(ref Hidden.GameObjects);
            ValidationUtils.EnsureUnique(ref Visuals.GameObjects);
            ValidationUtils.EnsureUnique(ref ProjectedTransforms);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        #endif // UNITY_EDITOR
    }
}