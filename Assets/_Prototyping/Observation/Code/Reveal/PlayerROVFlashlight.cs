using Aqua;
using Aqua.Entity;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Observation {
    public sealed class PlayerROVFlashlight : MonoBehaviour, PlayerROV.ITool {
        #region Inspector

        [SerializeField] private GameObject m_FlashlightRoot = null;
        [SerializeField] private Collider2D m_Collider = null;

        #endregion // Inspector

        private void Awake() {
            WorldUtils.TrackLayerMask(m_Collider, GameLayers.Flashlight_Mask, HandleEnter, HandleExit);
        }

        #region ITool

        public bool IsEnabled() {
            return m_FlashlightRoot.activeSelf;
        }

        public void Disable() {
            m_FlashlightRoot.SetActive(false);
            Visual2DSystem.Deactivate(GameLayers.Flashlight_Mask);
        }

        public void Enable() {
            m_FlashlightRoot.SetActive(true);
            Visual2DSystem.Activate(GameLayers.Flashlight_Mask);
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
            FlashlightRegion region = inCollider.GetComponentInParent<FlashlightRegion>();
            if (region != null) {
                if (region.LightCount++ == 1) {
                    region.Hidden.SetActive(false);
                    region.Reveal.SetActive(true);
                    region.OnLit?.Invoke(region);
                }
            }
        }

        private void HandleExit(Collider2D inCollider) {
            if (!inCollider)
                return;

            FlashlightRegion region = inCollider.GetComponentInParent<FlashlightRegion>(true);
            if (region != null) {
                if (--region.LightCount == 0) {
                    region.Reveal.SetActive(false);
                    region.Hidden.SetActive(true);
                    region.OnUnlit?.Invoke(region);
                }
            }
        }
    }
}