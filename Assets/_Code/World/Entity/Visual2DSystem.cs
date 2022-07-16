using System;
using System.Collections.Generic;
using Aqua.Cameras;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua.Entity {
    [DefaultExecutionOrder(-102)]
    public sealed class Visual2DSystem : SharedManager {

        #region Types

        private struct UpdateArgs {
            public ushort FrameIndex;
            public CameraService.PlanePositionHelper PositionCast;
            public Vector2 CenterPos;
            public float RadiusSq;
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private float m_CameraRadius = 8;

        #endregion // Inspector

        [NonSerialized] private int m_UpdateMask;

        private readonly EntityActivationSet<Visual2DTransform, UpdateArgs> m_UpdateSet;

        private Visual2DSystem() {
            m_UpdateSet = new EntityActivationSet<Visual2DTransform, UpdateArgs>();
            m_UpdateSet.SetStatus = SetStatus;
            m_UpdateSet.UpdateAwake = UpdateTransform;
            m_UpdateSet.UpdateActive = UpdateActive;
        }

        static private bool UpdateTransform(Visual2DTransform transform, in UpdateArgs updateArgs) {
            Vector3 position;
            float scale;
            if (transform.CustomPosition != null) {
                position = transform.CustomPosition(transform, transform.LastKnownPosition, updateArgs.PositionCast, out scale);
            } else {
                position = updateArgs.PositionCast.CastToPlane(transform.Source, out scale);
            }
            
            transform.WritePosition(updateArgs.FrameIndex, position, scale);

            Vector2 dist = (Vector2) position - updateArgs.CenterPos;
            return dist.sqrMagnitude < (updateArgs.RadiusSq + (transform.Radius * transform.Radius));
        }

        static private void UpdateActive(Visual2DTransform transform, in UpdateArgs updateArgs) {
            transform.Apply();
        }

        static private bool SetStatus(Visual2DTransform transform, EntityActiveStatus status, bool force) {
            EntityActiveStatus prevState = transform.Status;
            if (!force && prevState == status) {
                return false;
            }

            transform.Status = status;
            if (prevState == EntityActiveStatus.Active) {
                transform.OnDeactivate?.Invoke(transform);
            } else if (status == EntityActiveStatus.Active) {
                transform.OnActivate?.Invoke(transform);
            }

            if (transform.Collider) {
                transform.Collider.enabled = (status & EntityActiveStatus.Active) != 0;
            }
            
            return true;
        }

        private void LateUpdate() {
            if (Script.IsPausedOrLoading) {
                return;
            }

            m_UpdateSet.Update(m_UpdateMask, new UpdateArgs() {
                FrameIndex = Frame.Index,
                PositionCast = Services.Camera.GetPositionHelper(),
                CenterPos = Services.State.Player?.transform.position ?? Services.Camera.Position,
                RadiusSq = m_CameraRadius * m_CameraRadius
            });
        }

        public void Track(Visual2DTransform transform) {
            m_UpdateSet.Track(transform);
        }

        public void Untrack(Visual2DTransform transform) {
            m_UpdateSet.Untrack(transform);
        }

        public void AddMask(int mask) {
            m_UpdateMask |= mask;
        }

        public void RemoveMask(int mask) {
            m_UpdateMask &= ~mask;
        }

        static public void Activate(int mask) {
            Services.State.FindManager<Visual2DSystem>().AddMask(mask);
        }

        static public void Deactivate(int mask) {
            if (!Services.Valid) {
                return;
            }

            Services.State?.FindManager<Visual2DSystem>()?.RemoveMask(mask);
        }
    }
}