using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Aqua;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoAqua.Energy
{
    public class ConfigPropertyBox : MonoBehaviour
    {
        #region Types

        [Serializable] private class HeaderPool : SerializablePool<ConfigPropertyHeader> { }
        [Serializable] private class SpinnerPool : SerializablePool<ConfigPropertySpinner> { }
        [Serializable] private class TextPool : SerializablePool<ConfigPropertyText> { }
        [Serializable] private class EnumPool : SerializablePool<ConfigPropertyEnum> { }
        [Serializable] private class TogglePool : SerializablePool<ConfigPropertyToggle> { }

        #endregion // Types

        #region Inspector

        [SerializeField] private HeaderPool m_HeaderPool = null;
        [SerializeField] private SpinnerPool m_SpinnerPool = null;
        [SerializeField] private TextPool m_TextPool = null;
        [SerializeField] private EnumPool m_EnumPool = null;
        [SerializeField] private TogglePool m_TogglePool = null;

        #endregion // Inspector

        [NonSerialized] private int m_CurrentIndent;
        [NonSerialized] private readonly List<ConfigPropertyControl> m_AllControls = new List<ConfigPropertyControl>(32);
        [NonSerialized] private readonly Stack<ConfigPropertyHeader> m_HeaderStack = new Stack<ConfigPropertyHeader>(8);
        [NonSerialized] private readonly Dictionary<string, ConfigPropertyExpandable.State> m_PreservedExpansionState = new Dictionary<string, ConfigPropertyExpandable.State>(32);

        #region Builder

        public void Clear()
        {
            m_HeaderPool.Reset();
            m_SpinnerPool.Reset();
            m_TextPool.Reset();
            m_EnumPool.Reset();
            m_TogglePool.Reset();

            m_AllControls.Clear();
            m_HeaderStack.Clear();
        }

        public void BeginControls()
        {
            StoreExpansion();
            Clear();
        }

        public ConfigPropertyBox BeginGroup(string inId, string inHeader)
        {
            var header = m_HeaderPool.Alloc();
            m_AllControls.Add(header);
            PreInitialize(header, inId);
            header.Configure(inHeader);
            m_HeaderStack.Push(header);
            ++m_CurrentIndent;
            return this;
        }

        public ConfigPropertyBox EndGroup()
        {
            --m_CurrentIndent;
            m_HeaderStack.Pop();
            return this;
        }

        public ConfigPropertyBox Spinner(string inId, in ConfigPropertySpinner.Configuration inConfiguration)
        {
            var spinner = m_SpinnerPool.Alloc();
            m_AllControls.Add(spinner);
            PreInitialize(spinner, inId);
            spinner.Configure(inConfiguration);
            return this;
        }

        public ConfigPropertyBox Text(string inId, in ConfigPropertyText.Configuration inConfiguration)
        {
            var text = m_TextPool.Alloc();
            m_AllControls.Add(text);
            PreInitialize(text, inId);
            text.Configure(inConfiguration);
            return this;
        }

        public ConfigPropertyBox Enum(string inId, in ConfigPropertyEnum.Configuration inConfiguration)
        {
            var enumm = m_EnumPool.Alloc();
            m_AllControls.Add(enumm);
            PreInitialize(enumm, inId);
            enumm.Configure(inConfiguration);
            return this;
        }

        public ConfigPropertyBox Toggle(string inId, in ConfigPropertyToggle.Configuration inConfiguration)
        {
            var toggle = m_TogglePool.Alloc();
            m_AllControls.Add(toggle);
            PreInitialize(toggle, inId);
            toggle.Configure(inConfiguration);
            return this;
        }

        public void EndControls()
        {
            while(m_HeaderStack.Count > 0)
            {
                EndGroup();
            }

            RestoreExpansion();

            foreach(var control in m_AllControls)
            {
                control.Sync();
            }
        }

        private void PreInitialize(ConfigPropertyControl inControl, string inId)
        {
            int indentLevel = m_CurrentIndent;
            ConfigPropertyHeader header;
            if (m_HeaderStack.Count > 0)
                header = m_HeaderStack.Peek();
            else
                header = null;
            inControl.PreInitialize(inId, indentLevel, header);
        }

        #endregion // Builder

        #region Expansion State

        private ConfigPropertyExpandable.State GetPreservedState(string inPath)
        {
            ConfigPropertyExpandable.State state;
            if (!m_PreservedExpansionState.TryGetValue(inPath, out state))
                state = ConfigPropertyExpandable.State.Default;
            return state;
        }

        private void RestoreExpansion()
        {
            foreach(var control in m_AllControls)
            {
                string path = control.Id();
                var state = GetPreservedState(path);
                control.Expandable.RestoreState(state);
            }
        }

        private void StoreExpansion()
        {
            m_PreservedExpansionState.Clear();

            foreach(var control in m_AllControls)
            {
                string path = control.Id();
                var state = control.Expandable.RetrieveState();
                m_PreservedExpansionState.Add(path, state);
            }
        }

        #endregion // Expansion State

        public void SyncAll()
        {
            foreach(var control in m_AllControls)
            {
                control.Sync();
            }
        }
    }
}