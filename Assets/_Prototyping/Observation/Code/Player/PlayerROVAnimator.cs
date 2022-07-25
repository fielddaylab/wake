using UnityEngine;
using Aqua;
using Aqua.Character;
using System;
using BeauRoutine;
using BeauUtil;

namespace ProtoAqua.Observation
{
    public class PlayerROVAnimator : MonoBehaviour {

        #region Inspector

        [SerializeField] private Transform m_Root = null;
        [SerializeField] private ParticleSystem m_AmbientParticles = null;
        [SerializeField] private float m_PitchMultiplier = 0.5f;
        [SerializeField] private float m_NormalRotationLerp = 4;
        [SerializeField] private Vector3 m_RotationAdjust = new Vector3(0, -90, 0);

        [Header("Propeller")]
        [SerializeField] private Transform m_Propeller = null;
        [SerializeField] private ParticleSystem m_MovementParticles = null;
        [SerializeField] private ParticleSystem m_BoostParticles = null;
        [SerializeField] private float m_AmbientPropellerSpeed = 0;
        [SerializeField] private float m_FullPropellerSpeedBoost = 0;
        [SerializeField] private float m_EnginePropellerSpeedBoost = 0;

        #endregion // Inspector

        [NonSerialized] private InputState m_LastInputState;
        [NonSerialized] private FacingId m_Facing = FacingId.Right;
        [NonSerialized] private float m_Pitch = 0;
        [NonSerialized] private Vector3? m_LookTarget = null;
        [NonSerialized] private float m_PropellerSpeed;
        [NonSerialized] private float m_TargetPropellerSpeed;

        public struct InputState {
            public PlayerBodyStatus Status;
            public PlayerROVAnimationFlags AnimFlags;
            public bool Moving;
            public bool UsingTool;

            public Vector3 Position;
            public Vector2 NormalizedMove;
            public Vector2 NormalizedLook;
            public Vector3? LookTarget;
        }

        private void Awake() {
            m_PropellerSpeed = m_AmbientPropellerSpeed;
        }

        public void Process(InputState state) {
            m_LastInputState = state;

            if (state.Moving) {
                if (!Bits.ContainsAny(state.AnimFlags, PlayerROVAnimationFlags.DoNotTurn)) {
                    if (state.NormalizedMove.x < 0) {
                        m_Facing = FacingId.Left;
                    } else if (state.NormalizedMove.x > 0) {
                        m_Facing = FacingId.Right;
                    }
                }

                m_Pitch = state.NormalizedMove.y;
                m_LookTarget = null;
            } else if (state.UsingTool) {
                if (!Bits.ContainsAny(state.AnimFlags, PlayerROVAnimationFlags.DoNotTurn)) {
                    if (state.NormalizedLook.x < 0) {
                        m_Facing = FacingId.Left;
                    } else if (state.NormalizedLook.x > 0) {
                        m_Facing = FacingId.Right;
                    }
                }

                m_Pitch = state.NormalizedLook.y;
                m_LookTarget = state.LookTarget;
            } else {
                m_Pitch = 0;
                m_LookTarget = null;
            }

            m_TargetPropellerSpeed = m_AmbientPropellerSpeed;

            if ((state.Status & PlayerBodyStatus.PowerEngineEngaged) != 0) {
                m_TargetPropellerSpeed += m_EnginePropellerSpeedBoost;
            }

            if ((state.Status & PlayerBodyStatus.Slowed) != 0) {
                m_TargetPropellerSpeed *= 0.6f;
            }

            if (state.Moving) {
                m_TargetPropellerSpeed += state.NormalizedMove.magnitude * m_FullPropellerSpeedBoost;
            }
        }

        public void HandleTeleport(FacingId inFacing) {
            m_AmbientParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            m_AmbientParticles.Play();

            m_MovementParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            m_MovementParticles.Play();

            m_BoostParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

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

            if (now != target) {
                Quaternion lerp = Quaternion.Slerp(now, target, TweenUtil.Lerp(m_NormalRotationLerp, 1, Time.deltaTime));
                if (lerp == target) { // quaternion equals operator is fuzzy, so this is an "approximately equals"
                    lerp = target;
                }
                m_Root.localRotation = lerp;
            }

            if ((m_LastInputState.Status & PlayerBodyStatus.PowerEngineEngaged) != 0) {
                m_BoostParticles.Play();
            } else {
                m_BoostParticles.Stop();
            }

            m_PropellerSpeed = Mathf.Lerp(m_PropellerSpeed, m_TargetPropellerSpeed, TweenUtil.Lerp(m_NormalRotationLerp, 1, Time.deltaTime));
            m_Propeller.Rotate(m_PropellerSpeed * Time.deltaTime, 0, 0, Space.Self);
        }

        private Quaternion CalculateDesiredRotation() {
            Quaternion fix = Quaternion.Euler(m_RotationAdjust);
            Vector3 targetLookVec = default;
            if (m_LookTarget.HasValue) {
                targetLookVec = Vector3.Normalize(m_LookTarget.Value - m_LastInputState.Position);
                float down = Mathf.Abs(Vector3.Dot(targetLookVec, Vector3.down));
                targetLookVec = Vector3.Lerp(targetLookVec, targetLookVec.z > 0 ? Vector3.forward : Vector3.back, down * 0.5f);
                targetLookVec.y = Mathf.Clamp(targetLookVec.y, -m_PitchMultiplier, m_PitchMultiplier);
            } else {
                targetLookVec.x = Facing.X(m_Facing);
                targetLookVec.y = m_Pitch * m_PitchMultiplier;
            }

            return Quaternion.LookRotation(targetLookVec, Vector3.up) * fix;
        }
    }

    public enum PlayerROVAnimationFlags {
        DoNotTurn = 0x01
    }
}