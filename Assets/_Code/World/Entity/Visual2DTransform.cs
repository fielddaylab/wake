using System;
using Aqua.Cameras;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua.Entity {
    public class Visual2DTransform : MonoBehaviour, IActiveEntity, IBaked {

        #region Inspector

        [Required] public Transform Source;
        public Collider2D Collider;
        public LayerMask UpdateMask;
        public float Radius;

        #endregion // Inspector

        // State

        [NonSerialized] public EntityActiveStatus Status = EntityActiveStatus.Sleeping;

        [NonSerialized] public ushort LastUpdatedFrame = Frame.InvalidIndex;
        [NonSerialized] public ushort LastWrittenFrame = Frame.InvalidIndex;
        
        [NonSerialized] public Vector3 LastKnownPosition;
        [NonSerialized] public float LastKnownScale;
        [NonSerialized] public int OffscreenTickDelay;

        [NonSerialized] private Transform m_CachedTransform;

        // Callbacks

        public Visual2DActivateDeactivateDelegate OnActivate;
        public Visual2DActivateDeactivateDelegate OnDeactivate;
        public Visual2DPositionDelegate CustomPosition;

        private void OnEnable() {
            Services.State.FindManager<Visual2DSystem>().Track(this);
        }

        private void OnDisable() {
            if (!Services.Valid) {
                return;
            }
            
            Services.State?.FindManager<Visual2DSystem>()?.Untrack(this);
        }

        #region Write

        public void OverwritePosition(ushort frameIndex, Vector3 position, float scale) {
            LastUpdatedFrame = frameIndex;
            LastKnownPosition = position;
            LastKnownScale = scale;
            LastWrittenFrame = frameIndex;
        }

        public void WritePosition(ushort frameIndex, Vector3 position, float scale) {
            if (frameIndex != LastUpdatedFrame) {
                LastUpdatedFrame = frameIndex;
                LastKnownPosition = position;
                LastKnownScale = scale;
            }
        }

        public void CalculatePosition(ushort frameIndex, in CameraService.PlanePositionHelper positionHelper) {
            if (frameIndex != LastUpdatedFrame) {
                LastUpdatedFrame = frameIndex;
                LastKnownPosition = CameraService.CastToPlane(positionHelper, Source, out LastKnownScale);
            }
        }

        public void Wipe() {
            LastUpdatedFrame = Frame.InvalidIndex;
            LastWrittenFrame = Frame.InvalidIndex;
        }

        #endregion // Write

        public void Apply() {
            if (LastWrittenFrame != LastUpdatedFrame) {
                LastWrittenFrame = LastUpdatedFrame;
                this.CacheComponent(ref m_CachedTransform).position = LastKnownPosition;
            }
        }

        #region Entity

        int IActiveEntity.UpdateMask { get { return UpdateMask; } }

        int IBatchId.BatchId { get { return 0; } }

        EntityActiveStatus IActiveEntity.ActiveStatus { get { return Status; } }

        #endregion // Entity

        #region Bake

        #if UNITY_EDITOR

        private void Reset() {
            Source = transform.parent;
            Collider = GetComponent<Collider2D>();
        }

        int IBaked.Order { get { return -15; } }

        bool IBaked.Bake(BakeFlags flags) {
            if (Collider != null) {
                return Ref.Replace(ref Radius, PhysicsUtils.GetRadius(Collider));
            }

            if (Ref.Replace(ref Collider, GetComponent<Collider2D>())) {
                Radius = Collider != null ? PhysicsUtils.GetRadius(Collider) : 0;
                return true;
            }

            return false;
        }

        #endif // UNITY_EDITOR

        #endregion // Bake
    }

    public delegate void Visual2DActivateDeactivateDelegate(Visual2DTransform transform);
    public delegate Vector3 Visual2DPositionDelegate(Visual2DTransform transform, Vector3 position, in CameraService.PlanePositionHelper positionHelper, out float scale);
}