using System;
using Aqua;
using BeauUtil;

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

        #endif // UNITY_EDITOR
    }
}