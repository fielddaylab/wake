using System;
using Aqua;
using Aqua.Entity;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Observation {
    public class MicroscopeRegion : ToolRegion {
        public delegate void Callback(MicroscopeRegion region);

        #region Inspector

        public ActiveGroup Reveal = new ActiveGroup();
        public ActiveGroup Hidden = new ActiveGroup();
        public Visual2DTransform[] ProjectedTransforms = Array.Empty<Visual2DTransform>();
        public ScannableRegion Scannable = null;

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

        #endif // UNITY_EDITOR
    }
}