using System;
using System.Collections;
using Aqua;
using Aqua.Character;
using Aqua.Entity;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using UnityEngine;

namespace ProtoAqua.Observation {
    public sealed class PlayerROVBreaker : MonoBehaviour, PlayerROV.ITool {
        static public readonly TableKeyPair Var_TotalBroken = TableKeyPair.Parse("player:stats.brokenCount");

        #region Inspector

        [SerializeField] private GameObject m_BreakerRoot = null;
        [SerializeField] private Collider2D m_BreakCollider = null;

        #endregion // Inspector

        [NonSerialized] private int m_BrokenThisRun;
        [NonSerialized] private Routine m_ExecuteRoutine;

        [NonSerialized] private TriggerListener2D m_BreakListener;

        private void Awake() {
            m_BreakListener = WorldUtils.TrackLayerMask(m_BreakCollider, GameLayers.Breakable_Mask, HandleBreak, null);
        }

        private void Start() {
            m_BreakListener.enabled = false;
            m_BreakerRoot.gameObject.SetActive(false);
        }

        #region ITool

        public bool IsEnabled() {
            return m_BreakerRoot.activeSelf;
        }

        public void Disable() {
            if (!Services.Valid) {
                return;
            }

            if (m_BreakerRoot.activeSelf) {
                if (m_ExecuteRoutine) {
                    Services.Audio.PostEvent("ROV.IceBreaker.Cancel");
                } else {
                    Services.Audio.PostEvent("ROV.IceBreaker.Off");
                }
                m_ExecuteRoutine.Stop();
                FlushBreakCount();
                m_BreakListener.enabled = false;
                m_BreakerRoot.SetActive(false);
            }
        }

        public void Enable(PlayerBody inBody) {
            if (!m_BreakerRoot.activeSelf) {
                m_BreakerRoot.SetActive(true);
                Services.Audio?.PostEvent("ROV.IceBreaker.On");
                m_BreakListener.enabled = false;
            }
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

        private IEnumerator ExecuteRoutine() {
            yield break;
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