using System;
using System.Collections;
using System.Runtime.InteropServices;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoCP
{
    public class CPEnumSpinner : CPControl
    {
        #region Types

        public struct Configuration
        {
            public string Name;

            public CPLabeledValue[] Values;
            public object DefaultValue;

            public Type ValueType;

            public Func<object> Get;
            public Action<object> Set;
        }

        #endregion // Types

        #region Inspector

        [Header("Controls")]
        [SerializeField] private TMP_Text m_Label = null;
        [SerializeField] private TMP_InputField m_Input = null;
        [SerializeField] private Button m_DecrementButton = null;
        [SerializeField] private Button m_IncrementButton = null;

        [Header("Repeat Settings")]
        [SerializeField] private float m_InitialButtonRepeatDelay = 0.3f;
        [SerializeField] private float m_ButtonRepeatDelay = 0.1f;

        #endregion // Inspector

        [NonSerialized] private object m_CurrentValue = 0;
        [NonSerialized] private int m_CurrentIndex = 0;
        [NonSerialized] private Configuration m_Configuration;
        [NonSerialized] private int m_DefaultIndex;

        private Routine m_ButtonRoutine;

        #region CPControl

        public override FourCC Type() { return CPControlType.Enum; }

        protected override void OnConstruct()
        {
            m_Input.onEndEdit.AddListener(OnTextEndEdit);

            PointerListener decrementListener = m_DecrementButton.gameObject.AddComponent<PointerListener>();
            decrementListener.onPointerDown.AddListener(OnDecrementDown);
            decrementListener.onPointerUp.AddListener(OnRepeatButtonUp);

            PointerListener incrementListener = m_IncrementButton.gameObject.AddComponent<PointerListener>();
            incrementListener.onPointerDown.AddListener(OnIncrementDown);
            incrementListener.onPointerUp.AddListener(OnRepeatButtonUp);
        }

        #endregion // CPControl

        #region Configuration

        public void Configure(in Configuration inConfiguration)
        {
            m_Configuration = inConfiguration;
            if (m_Configuration.ValueType != null)
            {
                for(int i = 0; i < m_Configuration.Values.Length; ++i)
                {
                    m_Configuration.Values[i].Value = Convert.ChangeType(m_Configuration.Values[i].Value, m_Configuration.ValueType);
                }
            }

            m_DefaultIndex = GetIndexFromValue(m_Configuration.DefaultValue);
            if (m_DefaultIndex < 0)
                m_DefaultIndex = 0;

            if (m_Label)
                m_Label.SetText(m_Configuration.Name);

            SafeRetrieveValue();
        }

        #endregion // Configuration

        public object Value
        {
            get { return m_CurrentValue; }
            set
            {
                TrySetValue(m_CurrentValue);
            }
        }

        #region Updates

        private void SafeRetrieveValue()
        {
            object val = m_Configuration.Get();
            int index = GetIndexFromValue(val);
            if (index < 0)
            {
                index = m_DefaultIndex;
                val = m_Configuration.Values[index].Value;
            }

            m_CurrentIndex = index;
            m_CurrentValue = val;

            UpdateText();
            UpdateButtons();
        }

        private int GetIndexFromValue(object inValue)
        {
            if (m_Configuration.ValueType != null)
            {
                inValue = Convert.ChangeType(inValue, m_Configuration.ValueType);
            }
            
            for(int i = 0; i < m_Configuration.Values.Length; ++i)
            {
                ref CPLabeledValue value = ref m_Configuration.Values[i];
                if (value.Value == null)
                {
                    if (inValue == null)
                        return i;
                }
                else
                {
                    if (value.Value.Equals(inValue))
                        return i;
                }
            }

            return -1;
        }

        private int GetIndexFromLabel(string inLabel)
        {
            for(int i = 0; i < m_Configuration.Values.Length; ++i)
            {
                ref CPLabeledValue value = ref m_Configuration.Values[i];
                if (value.Label.Equals(inLabel, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private void TrySetValue(object inNewValue)
        {
            int index = GetIndexFromValue(inNewValue);
            TrySetValueFromIndex(index);
        }

        private void TrySetValueFromIndex(int inIndex)
        {
            if (inIndex < 0 || inIndex >= m_Configuration.Values.Length)
                return;

            if (m_CurrentIndex == inIndex)
                return;

            m_CurrentIndex = inIndex;
            m_CurrentValue = m_Configuration.Values[inIndex].Value;

            UpdateText();
            UpdateButtons();
            NotifyChanged();
        }

        private void UpdateText()
        {
            string value = m_Configuration.Values[m_CurrentIndex].Label;
            m_Input.SetTextWithoutNotify(value);
        }

        private void UpdateButtons()
        {
            bool bEnableDecrement = m_CurrentIndex > 0;
            bool bEnableIncrement = m_CurrentIndex < m_Configuration.Values.Length - 1;

            m_DecrementButton.targetGraphic.raycastTarget = bEnableDecrement;
            m_DecrementButton.interactable = bEnableDecrement;

            m_IncrementButton.targetGraphic.raycastTarget = bEnableIncrement;
            m_IncrementButton.interactable = bEnableIncrement;
        }

        private void NotifyChanged()
        {
            if (m_Configuration.Set != null)
            {
                object val = m_CurrentValue;
                if (m_Configuration.ValueType != null)
                    val = Convert.ChangeType(val, m_Configuration.ValueType);
                m_Configuration.Set(val);
            }

            InvokeUpdate(); 
        }

        #endregion // Updates

        #region Repeating Buttons

        private void OnDecrementRepeat()
        {
            TrySetValueFromIndex(m_CurrentIndex - 1);
        }

        private void OnIncrementRepeat()
        {
            TrySetValueFromIndex(m_CurrentIndex + 1);
        }

        private IEnumerator RepeatButton(Action inAction)
        {
            inAction();

            yield return m_InitialButtonRepeatDelay;

            while(true)
            {
                inAction();
                yield return m_ButtonRepeatDelay;
            }
        }

        #endregion // Repeating Buttons

        #region Listeners

        private void OnTextEndEdit(string inText)
        {
            int index = GetIndexFromLabel(inText);
            if (index >= 0)
            {
                TrySetValueFromIndex(index);
            }
            else
            {
                UpdateText();
            }
        }

        private void OnDecrementDown(PointerEventData inEventData)
        {
            m_ButtonRoutine.Replace(this, RepeatButton(OnDecrementRepeat));
        }

        private void OnIncrementDown(PointerEventData inEventData)
        {
            m_ButtonRoutine.Replace(this, RepeatButton(OnIncrementRepeat));
        }

        private void OnRepeatButtonUp(PointerEventData inEventData)
        {
            m_ButtonRoutine.Stop();
        }

        #endregion // Listeners

        public override void Sync()
        {
            base.Sync();

            SafeRetrieveValue();
            InvokeUpdate();
        }

        protected override void OnFree()
        {
            base.OnFree();

            if (m_Label)
                m_Label.SetText(string.Empty);
            m_Configuration = default(Configuration);
        }
    }
}