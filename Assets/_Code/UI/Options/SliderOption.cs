using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

namespace Aqua.Option
{
    public class SliderOption : MonoBehaviour 
    {
        #region Inspector

        [SerializeField] private LocText m_Label = null;
        [SerializeField] private Slider m_Slider = null;
        [SerializeField] private CursorInteractionHint m_Hint = null;
        [SerializeField] private TMP_Text m_Value = null;
        [SerializeField] private RectTransform m_Default = null;
        
        #endregion // Inspector

        [NonSerialized] private float m_MinValue = 0;
        [NonSerialized] private float m_MaxValue = 0;

        public Action<float> OnChanged;
        public Func<float, string> GenerateString;

        private void Awake()
        {
            m_Slider.onValueChanged.AddListener(OnSliderUpdated);
        }

        public void Initialize(TextId inLabel, TextId inDescription, Action<float> inSetter, float inMinValue, float inMaxValue, float inDefault, float inIncrement = 0)
        {
            m_Label.SetText(inLabel);
            m_Hint.TooltipId = inDescription;
            OnChanged = inSetter;

            m_MinValue = inMinValue;
            m_MaxValue = inMaxValue;

            m_Default.SetAnchorX(Mathf.InverseLerp(inMinValue, inMaxValue, inDefault));

            if (inIncrement > 0)
            {
                m_Slider.wholeNumbers = true;
                m_Slider.minValue = 0;
                m_Slider.maxValue = (inMaxValue - m_MinValue) / inIncrement;
            }
            else
            {
                m_Slider.wholeNumbers = false;
                m_Slider.minValue = 0;
                m_Slider.maxValue = 1;
            }
        }

        public void Sync(float inValue)
        {
            float sliderValue = Mathf.InverseLerp(m_MinValue, m_MaxValue, inValue) * m_Slider.maxValue;
            m_Slider.SetValueWithoutNotify(sliderValue);

            string valueString;
            if (GenerateString != null)
            {
                valueString = GenerateString(inValue);
            }
            else
            {
                valueString = inValue.ToString();
            }
            m_Value.SetText(valueString);
        }

        private void OnSliderUpdated(float inValue) 
        {
            float actualValue = Mathf.Lerp(m_MinValue, m_MaxValue, inValue / m_Slider.maxValue);

            OnChanged?.Invoke(actualValue);

            OptionsData options = Services.Data.Options;
            options.SetDirty();

            string valueString;
            if (GenerateString != null)
            {
                valueString = GenerateString(actualValue);
            }
            else
            {
                valueString = actualValue.ToString();
            }
            m_Value.SetText(valueString);
            
            Services.Events.QueueForDispatch(GameEvents.OptionsUpdated, options);
        }
    }
}