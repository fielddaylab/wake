using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using TMPro;
using System.Collections;
using System;
using BeauUtil.Tags;
using BeauUtil;
using BeauUtil.Variants;
using Aqua.Scripting;
using Leaf.Runtime;

namespace Aqua
{
    public class UnlockableUI : ScriptComponent
    {
        #region Inspector

        [SerializeField] private BasePanel m_Panel = null;
        [SerializeField] private ActiveGroup m_Group = null;

        #endregion // Inspector

        private TableKeyPair m_VarPair;
        private bool m_CachedLocked = false;
        private bool m_Initialized = false;

        #region Handlers

        public override void OnRegister(ScriptObject inObject) {
            base.OnRegister(inObject);

            m_VarPair = new TableKeyPair(Script.WorldTableId, ScriptObject.PersistenceId(inObject, "locked"));

            Services.Events.Register<TableKeyPair>(GameEvents.VariableSet, OnVariableUpdated, this)
                .Register(GameEvents.ProfileLoaded, RefreshState, this);

            m_Group.ForceActive(false);

            if (Services.Data.IsProfileLoaded()) {
                RefreshState();
            }
        }

        public override void OnDeregister(ScriptObject inObject) {
            Services.Events?.DeregisterAll(this);

            base.OnDeregister(inObject);
        }

        private void OnVariableUpdated(TableKeyPair keyPair) {
            if (keyPair == m_VarPair) {
                RefreshState();
            }
        }

        private void RefreshState() {
            bool locked = Script.ReadVariable(m_VarPair).AsBool();
            
            if (m_Initialized && m_CachedLocked == locked) {
                return;
            }
            m_CachedLocked = locked;
            m_Initialized = true;

            m_Group.SetActive(!locked);
            if (m_Panel) {
                if (locked) {
                    m_Panel.Hide();
                } else {
                    m_Panel.Show();
                }
            }
        }

        #endregion // Handlers
    
        #region Leaf

        [LeafMember("SetLocked")]
        public void SetLocked(bool locked) {
            Script.WriteVariable(m_VarPair, locked);
        }

        [LeafMember("Lock")]
        public void Lock() {
            Script.WriteVariable(m_VarPair, true);
        }

        [LeafMember("Unlock")]
        public void Unlock() {
            Script.WriteVariable(m_VarPair, false);
        }

        #endregion // Leaf
    }
}