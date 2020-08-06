using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoCP
{
    public class CPRoot : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private CPStyle m_Style = null;
        [SerializeField] private Transform m_ControlsRoot = null;
        [SerializeField] private bool m_StartExpanded = false;
        [SerializeField] private bool m_ClearEmptyGroups = false;

        #endregion // Inspector

        [NonSerialized] private readonly List<CPControl> m_RootControls = new List<CPControl>(32);
        [NonSerialized] private readonly List<CPControl> m_AllControls = new List<CPControl>(32);
        [NonSerialized] private readonly Stack<CPHeader> m_HeaderStack = new Stack<CPHeader>(8);
        [NonSerialized] private readonly Dictionary<string, CPControlState.State> m_PreservedExpansionState = new Dictionary<string, CPControlState.State>(32);

        #region Builder

        public void Clear()
        {
            foreach(var control in m_AllControls)
            {
                control.Recycle();
            }

            m_RootControls.Clear();
            m_AllControls.Clear();
            m_HeaderStack.Clear();
        }

        public void BeginControls()
        {
            StoreExpansion();
            Clear();
        }

        public CPHeader BeginGroup(string inId, string inHeader)
        {
            return BeginGroup(null, inId, inHeader);
        }

        public CPHeader BeginGroup(string inVariantId, string inId, string inHeader)
        {
            var header = m_Style.Alloc<CPHeader>(CPControlType.Header, inVariantId, m_ControlsRoot);
            PreInitialize(header, inId);
            header.Configure(inHeader);
            m_HeaderStack.Push(header);
            return header;
        }

        public int CurrentGroupChildCount()
        {
            if (m_HeaderStack.Count > 0)
            {
                return m_HeaderStack.Peek().Children.Length;
            }
            else
            {
                return 0;
            }
        }

        public string CurrentPath()
        {
            if (m_HeaderStack.Count > 0)
            {
                return m_HeaderStack.Peek().Id();
            }
            else
            {
                return string.Empty;
            }
        }

        public void EndGroup()
        {
            CPHeader header = m_HeaderStack.Pop();
            if (m_ClearEmptyGroups && header.Children.Length == 0)
            {
                m_AllControls.Remove(header);
                m_RootControls.Remove(header);
                header.Recycle();
            }
        }

        public CPSpinner NumberSpinner(string inId, in CPSpinner.Configuration inConfiguration)
        {
            return NumberSpinner(null, inId, inConfiguration);
        }

        public CPSpinner NumberSpinner(string inVariantId, string inId, in CPSpinner.Configuration inConfiguration)
        {
            var spinner = m_Style.Alloc<CPSpinner>(CPControlType.Spinner, inVariantId, m_ControlsRoot);
            PreInitialize(spinner, inId);
            spinner.Configure(inConfiguration);
            return spinner;
        }

        public CPTextField TextField(string inId, in CPTextField.Configuration inConfiguration)
        {
            return TextField(null, inId, inConfiguration);
        }

        public CPTextField TextField(string inVariantId, string inId, in CPTextField.Configuration inConfiguration)
        {
            var textField = m_Style.Alloc<CPTextField>(CPControlType.TextField, inVariantId, m_ControlsRoot);
            PreInitialize(textField, inId);
            textField.Configure(inConfiguration);
            return textField;
        }

        public CPEnumSpinner EnumField(string inId, in CPEnumSpinner.Configuration inConfiguration)
        {
            return EnumField(null, inId, inConfiguration);
        }

        public CPEnumSpinner EnumField(string inVariantId, string inId, in CPEnumSpinner.Configuration inConfiguration)
        {
            var enumm = m_Style.Alloc<CPEnumSpinner>(CPControlType.Enum, inVariantId, m_ControlsRoot);
            PreInitialize(enumm, inId);
            enumm.Configure(inConfiguration);
            return enumm;
        }

        public CPToggle Toggle(string inId, in CPToggle.Configuration inConfiguration)
        {
            return Toggle(null, inId, inConfiguration);
        }

        public CPToggle Toggle(string inVariantId, string inId, in CPToggle.Configuration inConfiguration)
        {
            var toggle = m_Style.Alloc<CPToggle>(CPControlType.Toggle, inVariantId, m_ControlsRoot);
            PreInitialize(toggle, inId);
            toggle.Configure(inConfiguration);
            return toggle;
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

        private void PreInitialize(CPControl inControl, string inId)
        {
            m_AllControls.Add(inControl);
            
            CPHeader header;
            if (m_HeaderStack.Count > 0)
            {
                header = m_HeaderStack.Peek();
            }
            else
            {
                header = null;
                m_RootControls.Add(inControl);
            }
            
            inControl.PreInitialize(inId, header);
        }

        public TControl Custom<TControl>(FourCC inControlType, string inId) where TControl : CPControl
        {
            return Custom<TControl>(inControlType, null, inId);
        }

        public TControl Custom<TControl>(FourCC inControlType, string inVariantId, string inId) where TControl : CPControl
        {
            var control = m_Style.Alloc<TControl>(inControlType, inVariantId, m_ControlsRoot);
            PreInitialize(control, inId);
            return control;
        }

        #endregion // Builder

        #region Expansion State

        private CPControlState.State GetPreservedState(string inPath)
        {
            CPControlState.State state;
            if (!m_PreservedExpansionState.TryGetValue(inPath, out state))
            {
                if (m_StartExpanded)
                {
                    state = CPControlState.State.AllExpanded;
                }
                else
                {
                    state = CPControlState.State.DefaultExpanded;
                }
            }
            return state;
        }

        private void RestoreExpansion()
        {
            foreach(var control in m_AllControls)
            {
                string path = control.Id();
                var state = GetPreservedState(path);
                control.State.RestoreState(state);
            }
        }

        private void StoreExpansion()
        {
            m_PreservedExpansionState.Clear();

            foreach(var control in m_AllControls)
            {
                string path = control.Id();
                var state = control.State.RetrieveState();
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