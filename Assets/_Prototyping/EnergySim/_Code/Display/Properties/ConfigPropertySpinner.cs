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
    public class ConfigPropertySpinner : ConfigPropertyControl
    {
        #region Types

        public struct Configuration
        {
            public string Name;

            public float Min;
            public float Max;
            public float Increment;
            public bool WholeNumbers;
            public bool SnapToIncrement;

            public string Suffix;
            public string SingularText;

            public Func<float> Get;
            public Action<float> Set;
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

        [NonSerialized] private float m_CurrentValue = 0;
        [NonSerialized] private Configuration m_Configuration;

        private Routine m_ButtonRoutine;

        #region Unity Events

        private void Awake()
        {
            m_Input.onEndEdit.AddListener(OnTextEndEdit);

            PointerListener decrementListener = m_DecrementButton.gameObject.AddComponent<PointerListener>();
            decrementListener.onPointerDown.AddListener(OnDecrementDown);
            decrementListener.onPointerUp.AddListener(OnRepeatButtonUp);

            PointerListener incrementListener = m_IncrementButton.gameObject.AddComponent<PointerListener>();
            incrementListener.onPointerDown.AddListener(OnIncrementDown);
            incrementListener.onPointerUp.AddListener(OnRepeatButtonUp);
        }

        #endregion // Unity Events

        #region Configuration

        public void Configure(in Configuration inConfiguration)
        {
            m_Configuration = inConfiguration;

            m_Label.SetText(m_Configuration.Name);

            if (m_Configuration.WholeNumbers)
            {
                m_Configuration.Min = (float) Math.Ceiling(m_Configuration.Min);
                m_Configuration.Max = (float) Math.Floor(m_Configuration.Max);
            }

            m_Input.contentType = m_Configuration.WholeNumbers ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;

            m_CurrentValue = m_Configuration.Get();
            UpdateText();
            UpdateButtons();
        }

        #endregion // Configuration

        public float Value
        {
            get { return m_CurrentValue; }
            set
            {
                TrySetValue(value);
            }
        }

        #region Updates

        private void TrySetValue(float inNewValue)
        {
            float newVal = inNewValue;
            if (CheckValueChange(ref newVal))
            {
                m_CurrentValue = newVal;
                UpdateText();
                UpdateButtons();
                NotifyChanged();
            }
        }

        private bool CheckValueChange(ref float ioNewValue)
        {
            float oldValue = m_CurrentValue;
            if (ioNewValue < m_Configuration.Min)
            {
                ioNewValue = m_Configuration.Min;
            }
            if (ioNewValue > m_Configuration.Max)
            {
                ioNewValue = m_Configuration.Max;
            }

            if (m_Configuration.SnapToIncrement && m_Configuration.Increment > 0)
            {
                ioNewValue = (float) Math.Round(ioNewValue / m_Configuration.Increment) * m_Configuration.Increment;
            }
            else if (m_Configuration.WholeNumbers)
            {
                ioNewValue = (float) Math.Round(ioNewValue);
            }

            return ioNewValue != oldValue;
        }

        private void UpdateText()
        {
            string val;
            if (m_Configuration.WholeNumbers)
            {
                val = ((int) m_CurrentValue).ToStringLookup();
            }
            else
            {
                val = m_CurrentValue.ToString();
            }
            if (val == "1" && !string.IsNullOrEmpty(m_Configuration.SingularText))
            {
                val = m_Configuration.SingularText;
            }
            else if (!string.IsNullOrEmpty(m_Configuration.Suffix))
            {
                val += m_Configuration.Suffix;
            }

            m_Input.SetTextWithoutNotify(val);
        }

        private void UpdateButtons()
        {
            bool bEnableDecrement = m_CurrentValue > m_Configuration.Min;
            bool bEnableIncrement = m_CurrentValue < m_Configuration.Max;

            m_DecrementButton.targetGraphic.raycastTarget = bEnableDecrement;
            m_DecrementButton.interactable = bEnableDecrement;

            m_IncrementButton.targetGraphic.raycastTarget = bEnableIncrement;
            m_IncrementButton.interactable = bEnableIncrement;
        }

        private void NotifyChanged()
        {
            if (m_Configuration.Set != null)
            {
                m_Configuration.Set(m_CurrentValue);
            }
        }

        #endregion // Updates

        #region Repeating Buttons

        private void OnDecrementRepeat()
        {
            TrySetValue(m_CurrentValue - m_Configuration.Increment);
        }

        private void OnIncrementRepeat()
        {
            TrySetValue(m_CurrentValue + m_Configuration.Increment);
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
            if (!string.IsNullOrEmpty(m_Configuration.SingularText))
            {
                if (inText.Equals(m_Configuration.SingularText, StringComparison.OrdinalIgnoreCase))
                {
                    TrySetValue(1);
                    return;
                }
            }
            if (!string.IsNullOrEmpty(m_Configuration.Suffix))
            {
                if (inText.EndsWith(m_Configuration.Suffix, StringComparison.OrdinalIgnoreCase))
                {
                    inText = inText.Substring(0, inText.Length - m_Configuration.Suffix.Length);
                }
            }
            
            if (m_Configuration.WholeNumbers)
            {
                int newValI;
                if (!int.TryParse(inText, out newValI))
                {
                    UpdateText();
                    return;
                }

                TrySetValue(newValI);
            }
            else
            {
                float newValF;
                if (!float.TryParse(inText, out newValF))
                {
                    UpdateText();
                    return;
                }

                TrySetValue(newValF);
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

            float val = m_Configuration.Get();
            m_CurrentValue = val;
            UpdateText();
            UpdateButtons();
        }

        protected override void OnFree()
        {
            base.OnFree();

            m_Label.SetText(string.Empty);
            m_Configuration = default(Configuration);
        }
    }
}