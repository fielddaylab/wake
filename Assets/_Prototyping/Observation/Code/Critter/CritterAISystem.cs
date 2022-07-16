using System;
using System.Collections.Generic;
using Aqua;
using Aqua.Cameras;
using Aqua.Entity;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.Observation {
    [DefaultExecutionOrder(-102)]
    public sealed class CritterAISystem : SharedManager {

        private struct UpdateArgs {
            public CameraService.PlanePositionHelper PositionCast;
            public Vector2 CenterPos;
            public float RadiusSq;
        }

        #region Inspector

        [SerializeField] private float m_CameraRadius = 8;

        #endregion // Inspector

        private readonly EntityActivationSet<CritterAI, UpdateArgs> m_UpdateSet;

        private CritterAISystem() {
            m_UpdateSet = new EntityActivationSet<CritterAI, UpdateArgs>();
            m_UpdateSet.SetStatus = SetStatus;
            m_UpdateSet.UpdateAwake = UpdateAI;
            m_UpdateSet.UpdateActiveBatch = UpdateActiveBatch;
        }

        static private bool UpdateAI(CritterAI obj, in UpdateArgs updateArgs) {
            Vector3 position;
            float scale;
            position = updateArgs.PositionCast.CastToPlane(obj.transform, out scale);
            Vector2 dist = (Vector2) position - updateArgs.CenterPos;
            return dist.sqrMagnitude < updateArgs.RadiusSq;
        }

        static private void UpdateActiveBatch(ListSlice<CritterAI> objs, int batchId, in UpdateArgs updateArgs) {
            
        }

        static private bool SetStatus(CritterAI obj, EntityActiveStatus status, bool force) {
            EntityActiveStatus prevState = obj.Status;
            if (!force && prevState == status) {
                return false;
            }

            obj.Status = status;
            return true;
        }

        private void LateUpdate() {
            if (Script.IsPausedOrLoading) {
                return;
            }

            // every third frame, update the set
            if ((Frame.Index % 3) == 0) {
                m_UpdateSet.Update(1, new UpdateArgs() {
                    PositionCast = Services.Camera.GetPositionHelper(),
                    CenterPos = Services.State.Player?.transform.position ?? Services.Camera.Position,
                    RadiusSq = m_CameraRadius * m_CameraRadius
                });
            }
        }

        public void Track(CritterAI ai) {
            m_UpdateSet.Track(ai);
        }

        public void Untrack(CritterAI ai) {
            m_UpdateSet.Untrack(ai);
        }
    }

    /// <summary>
    /// AI Stimulus
    /// </summary>
    public struct CritterAIStimulus {
        public uint Id;
        public StimulusType Type;
        public float Strength;
        public float Radius;
        public Vector3 Offset;
        
        public StimulusFlags Flags;
        public float Start;
        public float End;

        public StringHash32 Name;
        public RuntimeObjectHandle<Transform> Anchor;
    }

    [Flags]
    public enum StimulusType : ushort {
        [Hidden]
        None = 0,

        Light = 0x01,
        Sound = 0x02,
        Pulse = 0x04,
        Explosion = 0x08,
        RedLight = 0x10
    }

    [Flags]
    public enum StimulusFlags : ushort {
        TaperStrength = 0x01,
        TaperRadius = 0x02
    }
}