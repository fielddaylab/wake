using UnityEngine;
using Aqua;
using Aqua.Character;
using System;
using BeauRoutine;

namespace ProtoAqua.Observation
{
    public class PlayerROVAnimator : MonoBehaviour {

        #region Inspector

        [SerializeField] private Transform m_Root = null;
        [SerializeField] private ParticleSystem m_MovementParticles = null;
        [SerializeField] private float m_PitchMultiplier = 0.5f;
        [SerializeField] private float m_NormalRotationLerp = 4;
        [SerializeField] private Vector3 m_RotationAdjust = new Vector3(0, -90, 0);

        #endregion // Inspector

        [NonSerialized] private InputState m_LastInputState;
        [NonSerialized] private FacingId m_Facing = FacingId.Right;
        [NonSerialized] private float m_Pitch = 0;
        [NonSerialized] private Vector3? m_LookTarget = null;

        public struct InputState {
            public PlayerBodyStatus Status;
            public bool Moving;
            public bool UsingTool;

            public Vector3 Position;
            public Vector2 NormalizedMove;
            public Vector2 NormalizedLook;
            public Vector3? LookTarget;
        }

        public void Process(InputState state) {
            m_LastInputState = state;

            if (state.Moving) {
                if (state.NormalizedMove.x < 0) {
                    m_Facing = FacingId.Left;
                } else if (state.NormalizedMove.x > 0) {
                    m_Facing = FacingId.Right;
                }

                m_Pitch = state.NormalizedMove.y;
                m_LookTarget = null;
            } else if (state.UsingTool) {
                if (state.NormalizedLook.x < 0) {
                    m_Facing = FacingId.Left;
                } else if (state.NormalizedLook.x > 0) {
                    m_Facing = FacingId.Right;
                }

                m_Pitch = state.NormalizedLook.y;
                m_LookTarget = state.LookTarget;
            } else {
                m_Pitch = 0;
                m_LookTarget = null;
            }
        }

        public void HandleTeleport(FacingId inFacing) {
            m_MovementParticles.Stop();
            m_MovementParticles.Play();

            if (Facing.X(inFacing) != 0) {
                m_Facing = inFacing;
            }

            m_Pitch = 0;

            m_Root.localRotation = CalculateDesiredRotation();
        }

        private void LateUpdate() {
            if (Script.IsPaused) {
                return;
            }

            Quaternion target = CalculateDesiredRotation();
            Quaternion now = m_Root.localRotation;

            Quaternion lerp = Quaternion.Slerp(now, target, TweenUtil.Lerp(m_NormalRotationLerp, 1, Routine.DeltaTime));
            m_Root.localRotation = lerp;
        }

        private Quaternion CalculateDesiredRotation() {
            Quaternion fix = Quaternion.Euler(m_RotationAdjust);
            Vector3 targetLookVec = default;
            if (m_LookTarget.HasValue) {
                targetLookVec = Vector3.Normalize(m_LookTarget.Value - m_LastInputState.Position);
                targetLookVec.y = Mathf.Clamp(targetLookVec.y, -m_PitchMultiplier, m_PitchMultiplier);
            } else {
                targetLookVec.x = Facing.X(m_Facing);
                targetLookVec.y = m_Pitch * m_PitchMultiplier;
            }

            return Quaternion.LookRotation(targetLookVec, Vector3.up) * fix;
        }
    }
}