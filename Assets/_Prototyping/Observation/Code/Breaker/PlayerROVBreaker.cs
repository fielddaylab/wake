using System;
using System.Collections;
using Aqua;
using Aqua.Character;
using Aqua.Entity;
using Aqua.Scripting;
using AquaAudio;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using UnityEngine;

namespace ProtoAqua.Observation {
    public sealed class PlayerROVBreaker : MonoBehaviour, PlayerROV.ITool {
        static public readonly TableKeyPair Var_TotalBroken = TableKeyPair.Parse("player:stats.brokenCount");

        static public readonly StringHash32 Trigger_OnCharging = "IceBreakerCharging";
        static public readonly StringHash32 Trigger_OnCancel = "IceBreakerCanceled";
        static public readonly StringHash32 Trigger_OnFire = "IceBreakerFired";
        static public readonly StringHash32 Trigger_OnRecharge = "IceBreakerRecharged";
        static public readonly StringHash32 Trigger_OnFireFinished = "IceBreakerFireFinished";

        public enum Phase {
            Ready,
            Charging,
            Bursting,
            Recharging
        }

        #region Inspector

        [SerializeField] private Collider2D m_BreakCollider = null;
        [SerializeField] private ColorGroup m_RechargingBlink = null;
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem m_ChargeParticles = null;
        [SerializeField] private ParticleSystem m_BurstParticles = null;

        [Header("Timing")]
        [SerializeField] private float m_ChargeTime = 2;
        [SerializeField] private float m_ReleaseTime = 1;
        [SerializeField] private float m_RechargeTime = 2;

        #endregion // Inspector

        [NonSerialized] private int m_BrokenThisRun;
        [NonSerialized] private Routine m_ExecuteRoutine;
        [NonSerialized] private Routine m_RechargeRoutine;
        [NonSerialized] private Phase m_Phase;

        [NonSerialized] private TriggerListener2D m_BreakListener;
        [NonSerialized] private AudioHandle m_ChargeAudio;
        [NonSerialized] private bool m_Active;
        [NonSerialized] private BreakerUI m_UI;

        private void Awake() {
            m_BreakListener = WorldUtils.TrackLayerMask(m_BreakCollider, GameLayers.Breakable_Mask, HandleBreak, null);
        }

        private void Start() {
            m_BreakListener.enabled = false;
            m_UI = Services.UI.FindPanel<BreakerUI>();
        }

        #region ITool

        public bool IsEnabled() {
            return m_Active;
        }

        public void Disable() {
            if (!Services.Valid) {
                return;
            }

            if (m_Active) {
                if (m_Phase != Phase.Charging) {
                    Services.Audio.PostEvent("ROV.Breaker.Off");
                }
                m_ExecuteRoutine.Stop();
                Cancel();
                FlushBreakCount();
                m_BreakListener.enabled = false;
                m_Active = false;

                m_UI.Disable();
            }
        }

        public void Enable(PlayerBody inBody) {
            if (!Services.Valid) {
                return;
            }
            
            if (!m_Active) {
                m_Active = true;
                Services.Audio.PostEvent("ROV.Breaker.On");
                m_BreakListener.enabled = false;
                m_UI.Enable(inBody.transform);
            }
        }

        public void GetTargetPosition(bool inbOnGamePlane, out Vector3? outWorld, out Vector3? outCursor) {
            outWorld = outCursor = null;
        }

        public bool HasTarget() {
            return false;
        }

        public PlayerROVAnimationFlags AnimFlags() {
            switch(m_Phase) {
                case Phase.Bursting: {
                    return PlayerROVAnimationFlags.DoNotTurn;
                }

                default: {
                    return 0;
                }
            }
        }

        public float MoveSpeedMultiplier() {
            switch(m_Phase) {
                case Phase.Bursting:
                case Phase.Charging: {
                    return 0.5f;
                }

                default: {
                    return 1.0f;
                }
            }
        }

        public bool UpdateTool(in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody) {
            return false;
        }

        public void UpdateActive(in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody) {
            switch(m_Phase) {
                case Phase.Ready: {
                    if (inInput.UseAltHold || m_UI.IsHeld()) {
                        BeginCharge();
                    }
                    break;
                }

                case Phase.Charging: {
                    if (!inInput.UseAltHold && !m_UI.IsHeld()) {
                        CancelCharge();
                    }
                    break;
                }
            }
        }

        #endregion // ITool
        
        public void Cancel() {
            CancelCharge();
            EndBurst();
        }

        private void BeginCharge() {
            if (m_Phase == Phase.Ready) {
                m_Phase = Phase.Charging;
                m_ChargeAudio = Services.Audio.PostEvent("ROV.Breaker.Charging");
                m_ExecuteRoutine.Replace(this, ExecuteRoutine());
                m_ChargeParticles.Play(true);
                m_UI.SetPhase(m_Phase, 0);

                if (!Script.ShouldBlock()) {
                    Services.Script.TriggerResponse(Trigger_OnCharging);
                }
            }
        }

        private void CancelCharge() {
            if (m_Phase == Phase.Charging) {
                m_Phase = Phase.Ready;
                // TODO: Broadcast breaker cancel charge
                m_ExecuteRoutine.Stop();
                m_ChargeParticles.Stop();
                m_ChargeAudio.Stop();
                Services.Audio.PostEvent("ROV.Breaker.Cancel");
                m_UI.SetPhase(m_Phase, 0);

                if (!Script.ShouldBlock()) {
                    Services.Script.TriggerResponse(Trigger_OnCancel);
                }
            }
        }

        private void BeginBurst() {
            if (m_Phase == Phase.Charging) {
                m_Phase = Phase.Bursting;
                // TODO: Broadcast breaker burst
                m_BreakListener.enabled = true;
                m_UI.SetPhase(m_Phase, 0);
                m_ChargeAudio.Stop();
                Services.Audio.PostEvent("ROV.Breaker.Discharge");
                m_BurstParticles.Play(true);
                m_ChargeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Services.UI.WorldFaders.Flash(Color.white.WithAlpha(0.5f), 0.1f);
                Services.Camera.AddShake(0.1f, 0.1f, 0.4f);

                if (!Script.ShouldBlock()) {
                    Services.Script.TriggerResponse(Trigger_OnFire);
                }
            }
        }

        private void EndBurst() {
            if (m_Phase == Phase.Bursting) {
                m_Phase = Phase.Recharging;
                // TODO: Broadcast breaker finished
                m_UI.SetPhase(m_Phase, 0);
                m_BurstParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                m_BreakListener.enabled = false;
                int totalBroken = m_BrokenThisRun;
                FlushBreakCount();
                m_ExecuteRoutine.Stop();
                m_RechargeRoutine.Replace(this, RechargeRoutine());
                m_RechargingBlink.SetColor(AQColors.Red.WithAlpha(0.5f));

                if (!Script.ShouldBlock()) {
                    using(var table = TempVarTable.Alloc()) {
                        table.Set("objectsBroken", totalBroken);
                        Services.Script.TriggerResponse(Trigger_OnFireFinished, table);
                    }
                }
            }
        }

        private IEnumerator ExecuteRoutine() {
            float fill = 0, delta = 1 / m_ChargeTime;
            while(fill < 1) {
                if (Script.IsPausedOrLoading) {
                    CancelCharge();
                    yield break;
                }

                fill += Routine.DeltaTime * delta;
                m_UI.SetProgress(fill);
                yield return null;
            }
            
            BeginBurst();
            yield return m_ReleaseTime;
            EndBurst();
        }

        private IEnumerator RechargeRoutine() {
            try {
                float recharge = 0, delta = 1f / m_RechargeTime;
                while(recharge < 1) {
                    if (!Script.IsPausedOrLoading) {
                        recharge += Routine.DeltaTime * delta;
                        // TODO: Broadcast breaker recharge progress
                        m_UI.SetProgress(1 - recharge);
                    }
                    yield return null;
                }
            }
            finally {
                if (!Script.ShouldBlock()) {
                    Services.Script.TriggerResponse(Trigger_OnRecharge);
                }
                Services.Audio.PostEvent("ROV.Breaker.Recharged");
                m_RechargingBlink.SetColor(AQColors.BrightBlue);
                // TODO: Broadcast breaker recharge completed
                m_Phase = Phase.Ready;
                m_UI.SetPhase(m_Phase);
            }
        }

        private void FlushBreakCount() {
            if (m_BrokenThisRun > 0) {
                Services.Data.AddVariable(Var_TotalBroken, m_BrokenThisRun);
                m_BrokenThisRun = 0;
            }
        }

        private void HandleBreak(Collider2D inCollider) {
            ScriptDestructible destructible = inCollider.GetComponentInParent<ScriptDestructible>();
            if (destructible != null) {
                destructible.TriggerDestroy();
                m_BrokenThisRun++;
            }
        }
    }
}