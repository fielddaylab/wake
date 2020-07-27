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
    public class ConfigPropertyToggle : ConfigPropertyControl
    {
        #region Types

        public struct Configuration
        {
            public string Name;

            public Func<bool> Get;
            public Action<bool> Set;
        }

        #endregion // Types

        #region Inspector

        [Header("Controls")]
        [SerializeField] private TMP_Text m_Label = null;
        [SerializeField] private Toggle m_Toggle = null;

        #endregion // Inspector

        [NonSerialized] private bool m_CurrentValue = false;
        [NonSerialized] private Configuration m_Configuration;

        #region Unity Events

        private void Awake()
        {
            m_Toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        #endregion // Unity Events

        #region Configuration

        public void Configure(in Configuration inConfiguration)
        {
            m_Configuration = inConfiguration;

            m_Label.SetText(m_Configuration.Name);

            m_CurrentValue = m_Configuration.Get();
            UpdateToggle();
        }

        #endregion // Configuration

        public bool Value
        {
            get { return m_CurrentValue; }
            set
            {
                TrySetValue(value);
            }
        }

        #region Updates

        private void TrySetValue(bool inbNewValue)
        {
            if (inbNewValue != m_CurrentValue)
            {
                m_CurrentValue = inbNewValue;
                UpdateToggle();
                NotifyChanged();
            }
        }

        private void UpdateToggle()
        {
            m_Toggle.SetIsOnWithoutNotify(m_CurrentValue);
        }

        private void NotifyChanged()
        {
            if (m_Configuration.Set != null)
            {
                m_Configuration.Set(m_CurrentValue);
            }
        }

        #endregion // Updates

        #region Listeners

        private void OnToggleChanged(bool inbValue)
        {
            TrySetValue(inbValue);
        }

        #endregion // Listeners

        public override void Sync()
        {
            base.Sync();

            bool val = m_Configuration.Get();
            m_CurrentValue = val;
            UpdateToggle();
        }

        protected override void OnFree()
        {
            base.OnFree();

            m_Label.SetText(string.Empty);
            m_Configuration = default(Configuration);
        }
    }
}