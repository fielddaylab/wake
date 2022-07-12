using Aqua;
using Aqua.Character;
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
            if (m_FlashlightRoot.activeSelf) {
                m_FlashlightRoot.SetActive(false);
                Services.Audio?.PostEvent("ROV.Flashlight.Off");
            }
            Visual2DSystem.Deactivate(GameLayers.Flashlight_Mask);
        }

        public void Enable(PlayerBody inBody) {
            if (!m_FlashlightRoot.activeSelf) {
                m_FlashlightRoot.SetActive(true);
                Services.Audio?.PostEvent("ROV.Flashlight.On");
            }
            Visual2DSystem.Activate(GameLayers.Flashlight_Mask);
        }

        public void GetTargetPosition(bool inbOnGamePlane, out Vector3? outWorld, out Vector3? outCursor) {
            outWorld = outCursor = null;
        }

        public bool HasTarget() {
            return false;
        }

        public bool UpdateTool(in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody) {
            return false;
        }

        public void UpdateActive() {
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
            if (!inCollider || !Services.Valid)
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