using Aqua;
using UnityEngine;
using Aqua.Entity;
using Aqua.Cameras;
using BeauUtil;
using Aqua.Character;
using System;
using ScriptableBake;
using System.Collections.Generic;

namespace ProtoAqua.Observation {
    public sealed class PlayerROVMicroscope : MonoBehaviour, PlayerROV.ITool, IBaked {
        #region Inspector

        [SerializeField] private ActiveGroup m_VisualRoots = null;
        [SerializeField] private Collider2D m_Collider = null;

        [Header("Positioning")]
        [SerializeField] private float m_CameraHintStrength = 3;
        [SerializeField] private float m_CameraHintWeight = 0.8f;
        [SerializeField] private float m_CameraHintZoom = 1;

        [Header("Projection")]
        [SerializeField] private Transform m_ProjectionCenter = null;
        [SerializeField] private Camera m_ProjectionCamera = null;
        [SerializeField] private float m_ProjectionRadius = 1;
        [SerializeField] private float m_ProjectionMultiplier = 0.5f;

        [HideInInspector] private ActiveGroup m_WorldMicroscopeLayer = new ActiveGroup();

        #endregion // Inspector

        private readonly Visual2DPositionDelegate ProjectPosition;
        [NonSerialized] private uint m_CameraHint;

        private PlayerROVMicroscope() {
            ProjectPosition = (Visual2DTransform transform, Vector3 position, in CameraService.PlanePositionHelper positionHelper, out float scale) => {
                Vector2 viewport = m_ProjectionCamera.WorldToViewportPoint(transform.Source.position, Camera.MonoOrStereoscopicEye.Mono);
                viewport.x = (viewport.x - 0.5f) * 2 * m_ProjectionMultiplier;
                viewport.y = (viewport.y - 0.5f) * 2 * m_ProjectionMultiplier;
                if (viewport.sqrMagnitude > 1) {
                    viewport.Normalize();
                }
                viewport.x *= m_ProjectionRadius;
                viewport.y *= m_ProjectionRadius;
                Vector3 pos = m_ProjectionCenter.position + (Vector3) viewport;
                scale = 1;
                return pos;
            };
        }

        private void Awake() {
            WorldUtils.TrackLayerMask(m_Collider, GameLayers.Microscope_Mask, HandleEnter, HandleExit);
            m_VisualRoots.ForceActive(false);

            Script.OnSceneLoad(() => {
                m_WorldMicroscopeLayer.ForceActive(false);
            });
        }

        #region ITool

        public bool IsEnabled() {
            return m_Collider.isActiveAndEnabled;
        }

        public void Disable() {
            if (!Services.Valid) {
                return;
            }

            m_VisualRoots.Deactivate();
            Visual2DSystem.Deactivate(GameLayers.Microscope_Mask);
            Services.Camera.RemoveHint(m_CameraHint);
            m_CameraHint = 0;

            m_WorldMicroscopeLayer.SetActive(false);
        }

        public void Enable(PlayerBody inBody) {
            m_VisualRoots.Activate();
            Visual2DSystem.Activate(GameLayers.Microscope_Mask);
            m_CameraHint = Services.Camera.AddHint(m_ProjectionCenter, m_CameraHintStrength, m_CameraHintWeight, m_CameraHintZoom).Id;

            m_WorldMicroscopeLayer.SetActive(true);
        }

        public void GetTargetPosition(bool inbOnGamePlane, out Vector3? outWorld, out Vector3? outCursor) {
            outWorld = outCursor = null;
        }

        public bool HasTarget() {
            return false;
        }

        public PlayerROVAnimationFlags AnimFlags() {
            return 0;
        }

        public bool UpdateTool(in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody) {
            return false;
        }

        public void UpdateActive(in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody) {
        }

        #endregion // ITool

        private void HandleEnter(Collider2D inCollider) {
            MicroscopeRegion region = inCollider.GetComponentInParent<MicroscopeRegion>();
            if (region != null) {
                if (region.Scannable) {
                    region.Scannable.InMicroscope = true;
                }
                region.Hidden.SetActive(false);
                region.Reveal.SetActive(true);
                foreach(var v2d in region.ProjectedTransforms) {
                    v2d.CustomPosition = ProjectPosition;
                    v2d.Radius += 3;
                }
                region.OnViewed?.Invoke(region);
            }
        }

        private void HandleExit(Collider2D inCollider) {
            if (!inCollider || !Services.Valid)
                return;

            MicroscopeRegion region = inCollider.GetComponentInParent<MicroscopeRegion>(true);
            if (region != null) {
                region.Reveal.SetActive(false);
                region.Hidden.SetActive(true);
                foreach(var v2d in region.ProjectedTransforms) {
                    v2d.CustomPosition = null;
                    v2d.Radius -= 3;
                }
                if (region.Scannable) {
                    region.Scannable.InMicroscope = false;
                }
                region.OnUnviewed?.Invoke(region);
            }
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 15; }}

        bool IBaked.Bake(BakeFlags flags) {
            List<GameObject> microscopeViewGO = new List<GameObject>();
            List<Behaviour> microscopeViewComp = new List<Behaviour>();

            var allRegions = FindObjectsOfType<MicroscopeRegion>();
            foreach(var region in allRegions) {
                if (!region.Visuals.Empty) {
                    microscopeViewGO.AddRange(region.Visuals.GameObjects);
                    microscopeViewComp.AddRange(region.Visuals.Behaviours);
                }
            }

            m_WorldMicroscopeLayer.GameObjects = microscopeViewGO.ToArray();
            m_WorldMicroscopeLayer.Behaviours = microscopeViewComp.ToArray();
            return true;
        }

        #endif // UNITY_EDITOR
    }
}