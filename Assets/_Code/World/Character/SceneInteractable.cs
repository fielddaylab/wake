using System;
using System.Collections;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using Leaf.Runtime;
using UnityEngine;

namespace Aqua.Character {
    public class SceneInteractable : ScriptComponent {

        public delegate IEnumerator ExecuteDelegate(SceneInteractable interactable, PlayerBody player, ScriptThreadHandle thread);

        public enum InteractionMode {
            Inspect,
            GoToMap,
            GoToPreviousScene,
            Talk = ScriptInteractAction.Talk
        }

        #region Inspector

        [Header("Basic")]
        [SerializeField, Required] private Collider2D m_Collider = null;
        [SerializeField, ItemId(InvItemCategory.Upgrade)] private StringHash32[] m_RequiredUpgrades = null;
        [SerializeField, AutoEnum] private ScriptInteractLockMode m_LockMode = ScriptInteractLockMode.DisableInteract;

        [Header("Behavior")]
        [SerializeField] private InteractionMode m_Mode = InteractionMode.GoToMap;
        [SerializeField, MapId] private StringHash32 m_TargetMap = null;
        [SerializeField] private SerializedHash32 m_TargetEntrance = null;
        [SerializeField, ScriptCharacterId] private StringHash32 m_TargetCharacter = null;
        [SerializeField] private SceneLoadFlags m_MapLoadFlags = SceneLoadFlags.Cutscene;
        [SerializeField, ShowIfField("ShowStopMusic")] private bool m_StopMusic = true;
        [SerializeField] private bool m_AutoExecute = false;

        [Header("Display")]
        [SerializeField] private TextId m_LabelOverride = null;
        [SerializeField] private ContextButtonDisplay.LabelMode m_LabelMode = ContextButtonDisplay.LabelMode.MapLabel;
        [SerializeField] private TextId m_ActionLabelOverride = null;
        [SerializeField] private TextId m_LockMessageOverride = null;
        [SerializeField] private ContextButtonDisplay.BadgeMode m_BadgeMode = ContextButtonDisplay.BadgeMode.None;

        [Header("Pin")]
        [SerializeField] private Transform m_PinLocationOverride = null;
        [SerializeField] private bool m_PinToPlayer = false;
        [SerializeField] private TransformOffset m_LocationOffset = default;

        #endregion // Inspector

        private Routine m_Routine;
        [NonSerialized] private PlayerBody m_PlayerInside;
        [NonSerialized] private bool m_Locked;
        [NonSerialized] private bool m_LockOverride;
        private ScriptInteractConfig m_InspectConfig;

        public Predicate<SceneInteractable> CheckInteractable;
        public ExecuteDelegate OnExecute;
        public ExecuteDelegate OnLocked;

        public InteractionMode Mode() { return m_Mode; }
        public StringHash32 TargetMapId() { return m_TargetMap; }
        public StringHash32 TargetMapEntrance() {
            if (!m_TargetEntrance.IsEmpty)
                return m_TargetEntrance;
            return MapDB.LookupCurrentMap();
        }
        public bool CanInteract() { return !Locked() || m_LockMode == ScriptInteractLockMode.AllowInteract; }
        public bool Locked() { return m_LockOverride || m_Locked; }

        public void OverrideTargetMap(StringHash32 newTarget, StringHash32 newEntrance = default) {
            m_TargetMap = newTarget;
            m_TargetEntrance = newEntrance;
        }

        public void OverrideAuto(bool newAuto) {
            m_AutoExecute = newAuto;
        }

        public ContextButtonDisplay.LabelMode LabelMode() {
            return m_LabelMode;
        }

        public TextId Label(TextId defaultLabel) {
            return !m_LabelOverride.IsEmpty ? m_LabelOverride : defaultLabel;
        }

        public TextId ActionLabel(TextId defaultLabel) {
            return !m_ActionLabelOverride.IsEmpty ? m_ActionLabelOverride : defaultLabel;
        }

        public TextId LockedActionLabel(TextId defaultLabel) {
            return !m_LockMessageOverride.IsEmpty ? m_LockMessageOverride : defaultLabel;
        }

        public ContextButtonDisplay.BadgeMode BadgeMode() {
            return m_BadgeMode;
        }

        #region Unity Events

        private void Awake() {
            WorldUtils.ListenForPlayer(m_Collider, OnPlayerEnter, OnPlayerExit);

            if (m_Mode == InteractionMode.GoToMap) {
                var spawn = GetComponent<SpawnLocation>();
                if (spawn && !spawn.HasEntranceOverride()) {
                    spawn.OverrideEntrance(m_TargetMap);
                }
            }

            m_InspectConfig.Action = (ScriptInteractAction) m_Mode;
            m_InspectConfig.OnLocked = (p, t) => OnLocked?.Invoke(this, m_PlayerInside, t);
            m_InspectConfig.OnPerform = (p, t) => OnExecute?.Invoke(this, m_PlayerInside, t);
        }

        private void OnEnable() {
            if (Script.IsLoading || m_AutoExecute) {
                m_Routine.Replace(this, WaitToActivate()).Tick();
            } else {
                UpdateLocked();
            }
        }

        private void OnDisable() {
            m_Routine.Stop();
            ContextButtonDisplay.Clear(this);
        }

        #endregion // Unity Events

        #region Player Callbacks

        private void OnPlayerEnter(Collider2D collider) {
            m_PlayerInside = collider.GetComponentInParent<PlayerBody>();

            if (Script.IsLoading || m_Routine || !enabled || !m_Collider.isActiveAndEnabled)
                return;

            if (m_AutoExecute) {
                Interact();
                return;
            }

            if (!Locked() || m_LockMode != ScriptInteractLockMode.DisableContextPopup) {
                ContextButtonDisplay.Display(this);
            }
        }

        private void OnPlayerExit(Collider2D collider) {
            m_PlayerInside = null;
            ContextButtonDisplay.Clear(this);
        }

        #endregion // Player Callbacks

        public void Interact() {
            if (m_Routine || m_LockOverride || !enabled) {
                return;
            }

            if (Locked() && m_LockMode != ScriptInteractLockMode.AllowInteract) {
                return;
            }

            ScriptInteractParams interact;
            m_InspectConfig.LoadFlags = m_MapLoadFlags;
            if (m_StopMusic) {
                m_InspectConfig.LoadFlags |= SceneLoadFlags.StopMusic;
            }
            if (m_Mode == InteractionMode.Talk) {
                m_InspectConfig.TargetId = m_TargetCharacter;
                m_InspectConfig.TargetEntranceId = default;
            } else {
                m_InspectConfig.TargetId = TargetMapId();
                m_InspectConfig.TargetEntranceId = TargetMapEntrance();
            }
            interact.Config = m_InspectConfig;
            interact.Available = !Locked();
            interact.Source = this;
            interact.Invoker = m_PlayerInside;

            m_Routine = Script.Interact(interact);
        }

        private IEnumerator WaitToActivate() {
            m_Collider.enabled = false;
            
            while (Script.IsLoading) {
                yield return null;
            }

            UpdateLocked();
            m_Collider.enabled = true;

            if (m_AutoExecute) {
                while(m_PlayerInside) {
                    yield return null;
                }
            }
        }

        [LeafMember("Lock")]
        public void Lock() {
            if (!m_LockOverride) {
                m_LockOverride = true;
                UpdateLocked();
            }
        }

        [LeafMember("Unlock")]
        public void Unlock() {
            if (m_LockOverride) {
                m_LockOverride = false;
                UpdateLocked();
            }
        }

        private void UpdateLocked() {
            m_Locked = !CheckAvailable();
            if (Locked()) {
                if (m_LockMode == ScriptInteractLockMode.DisableObject) {
                    gameObject.SetActive(false);
                }
            } else {
                if (m_LockMode == ScriptInteractLockMode.DisableObject) {
                    gameObject.SetActive(true);
                }
            }
        }

        private bool CheckAvailable() {
            if (CheckInteractable != null && !CheckInteractable(this)) {
                return false;
            }

            foreach(var upgradeId in m_RequiredUpgrades) {
                if (!Save.Inventory.HasUpgrade(upgradeId)) {
                    return false;
                }
            }

            return true;
        }

        #region Configuration

        public void ConfigurePin(RectTransformPinned pinned) {
            Transform pin = null;
            if (m_PinToPlayer && m_PlayerInside)
                pin = m_PlayerInside.transform;
            if (!pin)
                pin = m_PinLocationOverride;
            if (!pin) {
                pin = transform;
            }
            pinned.Pin(pin, m_LocationOffset);
        }

        #endregion // Configuration

        #if UNITY_EDITOR

        private bool ShowTarget() {
            return m_Mode == InteractionMode.GoToMap;
        }

        private bool ShowStopMusic() {
            return m_Mode == InteractionMode.GoToMap || m_Mode == InteractionMode.GoToPreviousScene;
        }

        #endif // UNITY_EDITOR
    }
}