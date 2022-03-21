using Aqua;
using UnityEngine;

namespace ProtoAqua.Observation {
    public sealed class PlayerROVMicroscope : MonoBehaviour, PlayerROV.ITool {
        #region Inspector

        [SerializeField] private GameObject m_FlashlightRoot = null;
        [SerializeField] private Collider2D m_Collider = null;

        #endregion // Inspector

        private void Awake() {
            WorldUtils.TrackLayerMask(m_Collider, GameLayers.Scannable_Mask, HandleEnter, HandleExit);
        }

        #region ITool

        public bool IsEnabled() {
            return m_FlashlightRoot.activeSelf;
        }

        public void Disable() {
            m_FlashlightRoot.SetActive(false);
        }

        public void Enable() {
            m_FlashlightRoot.SetActive(true);
        }

        public Vector3? GetTargetPosition(bool inbOnGamePlane) {
            return null;
        }

        public bool HasTarget() {
            return false;
        }

        public bool UpdateTool(in PlayerROVInput.InputData inInput, Vector2 inVelocity) {
            return false;
        }

        #endregion // ITool

        private void HandleEnter(Collider2D inCollider) {
            ScannableRegion region = inCollider.GetComponentInParent<ScannableRegion>();
            if (region != null) {
                region.Current |= ScannableStatusFlags.Microscope;
            }
        }

        private void HandleExit(Collider2D inCollider) {
            if (!inCollider)
                return;

            ScannableRegion region = inCollider.GetComponentInParent<ScannableRegion>();
            if (region != null) {
                region.Current &= ~ScannableStatusFlags.Microscope;
            }
        }
    }
}