using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Aqua.Scripting {
    [AddComponentMenu("Aqualab/Scripting/Script Inspectable")]
    public class ScriptInspectable : ScriptComponent, IPointerClickHandler {
        public delegate IEnumerator ExecuteDelegate(ScriptInspectable inspectable, ScriptThreadHandle thread);

        [SerializeField] private PointerListener m_Proxy = null;
        [SerializeField] private Selectable m_Selectable = null;
        [SerializeField] private SerializedHash32 m_GroupId = null;
        [SerializeField] private ActiveGroup m_VisibleWhenInteractive = null;
        private Routine m_Execute;

        [NonSerialized] public CursorInteractionHint Hint;
        [NonSerialized] public bool Locked = false;
        [NonSerialized] private ScriptInteractionGroup m_Group;

        public ScriptInteractConfig Config;
        public ExecuteDelegate OnInspect;

        private readonly ScriptInteractCallback m_DefaultCallback;

        private ScriptInspectable() {
            m_DefaultCallback = (p, t) => {
                return OnInspect?.Invoke(this, t);
            };
        }

        [LeafMember, Preserve]
        public void Inspect() {
            if (m_Execute)
                return;

            ScriptInteractParams interact;
            interact.Source = this;
            interact.Available = !Locked;
            interact.Invoker = null;
            interact.Config = Config;
            interact.Config.OnPerform = interact.Config.OnPerform ?? m_DefaultCallback;
            m_Execute = Script.Interact(interact);
        }

        public StringHash32 GroupId {
            get { return m_GroupId; }
            set {
                if (m_GroupId != value) {
                    Group = ScriptInteractionGroup.Find(value);
                }
            }
        }

        public ScriptInteractionGroup Group {
            get { return m_Group; }
            set {
                if (m_Group != value) {
                    if (m_Group != null) {
                        m_Group.DeregisterInspectable(this);
                        m_GroupId = null;
                    }
                    m_Group = value;
                    if (m_Group) {
                        m_Group.RegisterInspectable(this);
                        m_GroupId = m_Group.Parent.Id();
                    }
                }
            }
        }

        [LeafMember("LockInteract"), Preserve]
        private void LeafLock() {
            if (!Locked) {
                Locked = true;
                RefreshState();
            }
        }

        [LeafMember("UnlockInteract"), Preserve]
        private void LeafUnlock() {
            if (Locked) {
                Locked = false;
                RefreshState();
            }
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            if (!m_Selectable || (m_Selectable.interactable && CanvasHelper.IsPointerInteractable((RectTransform) m_Selectable.transform))) {
                Inspect();
            }
        }

        private void OnDisable() {
            m_Execute.Stop();
        }

        internal void UpdateGroupState(bool parentState) {
            bool interactable = parentState && !Locked && isActiveAndEnabled;

            if (m_Proxy) {
                m_Proxy.enabled = interactable;
            }
            if (Hint) {
                Hint.enabled = interactable;
            }
            if (m_Selectable) {
                m_Selectable.interactable = interactable;
            }
            m_VisibleWhenInteractive.SetActive(interactable);
        }

        public void RefreshState() {
            bool interactable = !Locked;
            if (m_Group) {
                interactable &= m_Group.CanInteract();
            }

            if (m_Proxy) {
                m_Proxy.enabled = interactable;
            }
            if (Hint) {
                Hint.enabled = interactable;
            }
            if (m_Selectable) {
                m_Selectable.interactable = interactable;
            }
            m_VisibleWhenInteractive.SetActive(interactable);
        }

        #region IScriptComponent

        public override void OnRegister(ScriptObject inObject) {
            base.OnRegister(inObject);

            if (m_Selectable) {
                Hint = m_Selectable.EnsureComponent<CursorInteractionHint>();
                if (!m_Proxy && m_Selectable.gameObject != gameObject) {
                    m_Proxy = m_Selectable.EnsureComponent<PointerListener>();
                }
            } else if (!m_Proxy) {
                var collider = GetComponent<Collider>();
                if (collider) {
                    m_Proxy = collider.EnsureComponent<PointerListener>();
                } else {
                    var collider2d = GetComponent<Collider2D>();
                    if (collider2d) {
                        m_Proxy = collider2d.EnsureComponent<PointerListener>();
                    }
                }
            }
            
            if (m_Proxy && m_Proxy.gameObject != gameObject) {
                Hint = m_Proxy.EnsureComponent<CursorInteractionHint>();
                m_Proxy.onClick.AddListener(((IPointerClickHandler) this).OnPointerClick);
            } else {
                Hint = this.EnsureComponent<CursorInteractionHint>();
            }

            m_VisibleWhenInteractive.ForceActive(true);
            RefreshState();
        }

        public override void PostRegister() {
            base.PostRegister();

            if (!m_GroupId.IsEmpty) {
                Group = ScriptInteractionGroup.Find(m_GroupId);
            }
        }

        public override void OnDeregister(ScriptObject inObject) {
            Group = null;
            base.OnDeregister(inObject);
        }

        #endregion // IScriptComponent
    }
}