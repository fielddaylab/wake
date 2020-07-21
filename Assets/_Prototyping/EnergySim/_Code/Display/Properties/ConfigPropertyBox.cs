using System;
using System.Collections;
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

        #endregion // Types

        #region Inspector

        [SerializeField] private HeaderPool m_HeaderPool = null;
        [SerializeField] private SpinnerPool m_SpinnerPool = null;
        [SerializeField] private TextPool m_TextPool = null;

        #endregion // Inspector

        [NonSerialized] private int m_CurrentIndent;

        private void Awake()
        {
            if (!m_HeaderPool.IsInitialized())
                m_HeaderPool.Initialize();

            if (!m_SpinnerPool.IsInitialized())
                m_SpinnerPool.Initialize();

            if (!m_TextPool.IsInitialized())
                m_TextPool.Initialize();
        }

        #region Builder

        public void Clear()
        {
            m_HeaderPool.Reset();
            m_SpinnerPool.Reset();
            m_TextPool.Reset();
        }

        public ConfigPropertyBox BeginGroup(string inHeader)
        {
            var header = m_HeaderPool.Alloc();
            header.Configure(inHeader, m_CurrentIndent++);
            return this;
        }

        public ConfigPropertyBox EndGroup()
        {
            --m_CurrentIndent;
            return this;
        }

        public ConfigPropertyBox Spinner(in ConfigPropertySpinner.Configuration inConfiguration)
        {
            var spinner = m_SpinnerPool.Alloc();
            spinner.Configure(inConfiguration);
            spinner.IndentGroup.SetIndent(m_CurrentIndent);
            return this;
        }

        public ConfigPropertyBox Text(in ConfigPropertyText.Configuration inConfiguration)
        {
            var text = m_TextPool.Alloc();
            text.Configure(inConfiguration);
            text.IndentGroup.SetIndent(m_CurrentIndent);
            return this;
        }

        #endregion // Builder

        public void SyncAll()
        {
            foreach(var spinner in m_SpinnerPool.ActiveObjects())
            {
                spinner.SyncValue();
            }

            foreach(var text in m_TextPool.ActiveObjects())
            {
                text.SyncValue();
            }
        }
    }
}