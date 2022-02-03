using System;
using System.Collections;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using Leaf.Runtime;
using UnityEngine;

namespace Aqua.Character {
    public class SceneInteractable : ScriptComponent {

        public delegate IEnumerator ExecuteDelegate(SceneInteractable interactable, ScriptThreadHandle thread);

        public enum InteractionMode {
            Inspect,
            GoToMap,
            GoToPreviousScene
        }

        public enum LockMode {
            DisableObject,
            DisableInteract,
            AllowInteract
        }

        #region Inspector

        [Header("Basic")]
        [SerializeField, Required] private Collider2D m_Collider = null;
        [SerializeField, ItemId(InvItemCategory.Upgrade)] private StringHash32[] m_RequiredUpgrades = null;
        [SerializeField] private LockMode m_LockMode = LockMode.DisableInteract;

        [Header("Behavior")]
        [SerializeField] private InteractionMode m_Mode = InteractionMode.GoToMap;
        [SerializeField, MapId] private StringHash32 m_TargetMap = null;
        [SerializeField] private SerializedHash32 m_TargetEntrance = null;
        [SerializeField] private SceneLoadFlags m_MapLoadFlags = SceneLoadFlags.Cutscene;
        [SerializeField, ShowIfField("ShowStopMusic")] private bool m_StopMusic = true;
        [SerializeField] private bool m_AutoExecute = false;

        [Header("Display")]
        [SerializeField] private Sprite m_IconOverride = null;
        [SerializeField] private TextId m_LabelOverride = null;
        [SerializeField] private Transform m_PinLocationOverride = null;
        [SerializeField] private bool m_PinToPlayer = false;
        [SerializeField] private TransformOffset m_LocationOffset = default;

        #endregion // Inspector

        [NonSerialized] private Routine m_Routine;
        [NonSerialized] private PlayerBody m_PlayerInside;
        [NonSerialized] private bool m_Locked;
        [NonSerialized] private bool m_CancelQueued;

        public Predicate<SceneInteractable> CheckInteractable;
        public ExecuteDelegate OnExecute;
        public ExecuteDelegate OnLocked;

        public InteractionMode Mode() { return m_Mode; }
        public StringHash32 TargetMapId() { return m_TargetMap; }
        public StringHash32 TargetMapEntrance() {
            if (!m_TargetEntrance.IsEmpty)
                return m_TargetEntrance;
            var spawn = GetComponent<SpawnLocation>();
            if (spawn && !spawn.Id.IsEmpty)
                return spawn.Id;
            return MapDB.LookupCurrentMap();
        }
        public bool CanInteract() { return !m_Locked || m_LockMode == LockMode.AllowInteract; }

        public void OverrideTargetMap(StringHash32 newTarget, StringHash32 newEntrance = default) {
            m_TargetMap = newTarget;
            m_TargetEntrance = newEntrance;
        }

        public Sprite Icon(Sprite defaultIcon) {
            return m_IconOverride != null ? m_IconOverride : defaultIcon;
        }

        public TextId Label(TextId defaultLabel) {
            return !m_LabelOverride.IsEmpty ? m_LabelOverride : defaultLabel;
        }

        #region Unity Events

        private void Awake() {
            WorldUtils.ListenForPlayer(m_Collider, OnPlayerEnter, OnPlayerExit);
        }

        private void OnEnable() {
            if (Script.IsLoading || m_AutoExecute) {
                m_Routine.Replace(this, WaitToActivate()).TryManuallyUpdate(0);
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

            if (m_Routine)
                return;

            if (m_AutoExecute) {
                Interact();
                return;
            }

            ContextButtonDisplay.Display(this);
        }

        private void OnPlayerExit(Collider2D collider) {
            m_PlayerInside = null;
            ContextButtonDisplay.Clear(this);
        }

        #endregion // Player Callbacks

        public void Interact() {
            if (m_Routine) {
                return;
            }

            if (m_Locked && m_LockMode != LockMode.AllowInteract) {
                return;
            }

            m_CancelQueued = false;
            m_Routine = Routine.Start(this, InteractRoutine());
            m_Routine.TryManuallyUpdate(0);
        }

        private IEnumerator InteractRoutine() {
            ScriptThreadHandle thread = ScriptObject.Interact(Parent, m_Locked, m_TargetMap);

            if (m_Locked) {
                IEnumerator locked = OnLocked?.Invoke(this, thread);
                if (locked != null)
                    yield return null;
                
                if (thread.IsRunning())
                    yield return thread.Wait();
                yield break;
            }

            IEnumerator execute = OnExecute?.Invoke(this, thread);
            if (execute != null)
                yield return execute;
            if (thread.IsRunning())
                yield return thread.Wait();

            if (m_CancelQueued) {
                m_CancelQueued = false;
                yield break;
            }

            switch(m_Mode) {
                case InteractionMode.Inspect: {
                    thread = ScriptObject.Inspect(Parent);
                    yield return thread.Wait();
                    break;
                }

                case InteractionMode.GoToPreviousScene: {
                    if (m_StopMusic) {
                        Services.Audio.StopMusic();
                    }
                    StateUtil.LoadPreviousSceneWithWipe(TargetMapEntrance(), null, m_MapLoadFlags);
                    yield break;
                }

                case InteractionMode.GoToMap: {
                    if (m_StopMusic) {
                        Services.Audio.StopMusic();
                    }
                    StateUtil.LoadMapWithWipe(m_TargetMap, TargetMapEntrance(), null, m_MapLoadFlags);
                    yield break;
                }
            }
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
    
        [LeafMember("CancelInteract")]
        public void Cancel() {
            m_CancelQueued = true;
        }

        private void UpdateLocked() {
            m_Locked = !CheckAvailable();
            if (m_Locked) {
                if (m_LockMode == LockMode.DisableObject) {
                    gameObject.SetActive(false);
                }
            } else {
                if (m_LockMode == LockMode.DisableObject) {
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
            if (m_PinToPlayer)
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