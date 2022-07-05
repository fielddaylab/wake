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
    public class ScriptInspectable : ScriptComponent, IPointerClickHandler {
        public delegate IEnumerator ExecuteDelegate(ScriptInspectable inspectable, ScriptThreadHandle thread);

        [SerializeField] private PointerListener m_Proxy = null;
        [SerializeField] private Selectable m_Selectable = null;
        private Routine m_Execute;

        [NonSerialized] public CursorInteractionHint Hint;
        [NonSerialized] public bool Locked = false;

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

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            if (!m_Selectable || (m_Selectable.interactable && CanvasHelper.IsPointerInteractable((RectTransform) m_Selectable.transform))) {
                Inspect();
            }
        }

        private void OnDisable() {
            m_Execute.Stop();
        }

        #region IScriptComponent

        public override void OnRegister(ScriptObject inObject) {
            base.OnRegister(inObject);

            if (m_Selectable) {
                Hint = m_Selectable.EnsureComponent<CursorInteractionHint>();
                if (!m_Proxy && m_Selectable.gameObject != gameObject) {
                    m_Proxy = m_Selectable.EnsureComponent<PointerListener>();
                }
            }
            
            if (m_Proxy && m_Proxy.gameObject != gameObject) {
                Hint = m_Proxy.EnsureComponent<CursorInteractionHint>();
                m_Proxy.onClick.AddListener(((IPointerClickHandler) this).OnPointerClick);
            } else {
                Hint = this.EnsureComponent<CursorInteractionHint>();
            }

            Hint.enabled = !Locked;
        }

        #endregion // IScriptComponent
    }
}