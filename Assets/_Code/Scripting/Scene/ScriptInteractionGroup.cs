using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Aqua.Scripting {
    [AddComponentMenu("Aqualab/Scripting/Script Interaction Group")]
    public class ScriptInteractionGroup : ScriptComponent {
        static private readonly Dictionary<StringHash32, ScriptInteractionGroup> s_Groups = new Dictionary<StringHash32, ScriptInteractionGroup>();

        [SerializeField] private bool m_Interactable = true;
        
        [NonSerialized] private int m_LockCount = 0;
        private readonly RingBuffer<ScriptInspectable> m_Children = new RingBuffer<ScriptInspectable>();

        public bool Interactable {
            get { return m_Interactable; }
            set {
                if (m_Interactable != value) {
                    m_Interactable = value;
                    DispatchUpdate();
                }
            }
        }

        public bool CanInteract() {
            return m_Interactable && m_LockCount == 0;
        }

        [LeafMember("LockGroup"), Preserve]
        public void Lock() {
            m_LockCount++;
            if (m_LockCount == 1) {
                DispatchUpdate();
            }
        }

        [LeafMember("UnlockGroup"), Preserve]
        public void Unlock() {
            if (m_LockCount > 0) {
                m_LockCount--;
                if (m_LockCount == 0) {
                    DispatchUpdate();
                }
            }
        }

        public void RegisterInspectable(ScriptInspectable inspectable) {
            if (!m_Children.Contains(inspectable)) {
                m_Children.PushBack(inspectable);
                inspectable.UpdateGroupState(CanInteract());
            }
        }

        public void DeregisterInspectable(ScriptInspectable inspectable) {
            m_Children.FastRemove(inspectable);
        }

        private void DispatchUpdate() {
            bool canInteract = CanInteract();
            Debug.LogFormat("Dispatching group '{0}' update for state {1} to {2} children", name, canInteract, m_Children.Count);
            foreach(var child in m_Children) {
                child.UpdateGroupState(canInteract);
            }
        }

        static public ScriptInteractionGroup Find(StringHash32 id) {
            if (!s_Groups.TryGetValue(id, out ScriptInteractionGroup group)) {
                Debug.LogErrorFormat("[ScriptInteractionGroup] Unable to find group with id '{0}'", id);
            }
            return group;
        }

        #region Events

        public override void OnRegister(ScriptObject inObject) {
            base.OnRegister(inObject);

            if (!inObject.Id().IsEmpty) {
                s_Groups[inObject.Id()] = this;
            }
        }

        public override void OnDeregister(ScriptObject inObject) {
            base.OnDeregister(inObject);

            if (!inObject.Id().IsEmpty) {
                s_Groups.Remove(inObject.Id());
            }
        }
    
        #endregion // Events
    }
}