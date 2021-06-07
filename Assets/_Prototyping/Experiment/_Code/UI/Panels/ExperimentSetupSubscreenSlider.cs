using System;
using Aqua;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using BeauUtil.Debugger;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenSlider : ExperimentSetupSubscreen
    {        
        #region Inspector

        [SerializeField] private Button m_EndButton = null;
        [SerializeField] private LocText m_Label = null;
        [SerializeField] private TMP_Text m_Value = null;
        [SerializeField] private Transform m_SliderGroup = null;

        #endregion // Inspector

        [NonSerialized] private PropertySlider[] m_CachedSliders;
        [NonSerialized] private int m_SliderCount; 

        public Action OnSelectEnd;

        protected override void Awake()
        {
            m_EndButton.onClick.AddListener(() => OnSelectEnd?.Invoke());
            m_CachedSliders = m_SliderGroup.GetComponentsInChildren<PropertySlider>();

            for(int i = 0; i < m_CachedSliders.Length; i++)
            {
                PropertySlider slider = m_CachedSliders[i];
                slider.Slider.onValueChanged.AddListener((b) => UpdateFromSlider(slider.Id, b));
            }
        }

        protected override void RestoreState()
        {
            base.RestoreState();

            int sliderIdx = 0;
            foreach(var property in Services.Assets.WaterProp.Measurable())
            {
                Assert.True(sliderIdx < m_CachedSliders.Length);

                if (!Services.Data.Profile.Inventory.IsPropertyUnlocked(property.Index()))
                    continue;

                var slider = m_CachedSliders[sliderIdx];
                slider.gameObject.SetActive(true);
                slider.Load(property, property.Icon(), true);
                slider.Slider.SetValueWithoutNotify(Setup.EnvironmentProperties[property.Index()]);

                sliderIdx++;
            }
            
            m_SliderCount = sliderIdx;

            for(; sliderIdx < m_CachedSliders.Length; ++sliderIdx)
            {
                m_CachedSliders[sliderIdx].gameObject.SetActive(false);
            }

            UpdateDisplay(WaterPropertyId.NONE, 0);
        }

        private void UpdateFromSlider(WaterPropertyId id, float value)
        {
            var def = Services.Assets.WaterProp.Property(id);
            Setup.EnvironmentProperties[id] = value;

            UpdateDisplay(id, value);
            
            var color = def.Color();
            color.a = value;
            Services.Events.Dispatch(ExperimentEvents.OnMeasurementChange, color);
        }

        private void UpdateDisplay(WaterPropertyId inWaterId, float value)
        {
            if (inWaterId == WaterPropertyId.NONE)
            {
                m_Label.SetText(null);
                m_Value.SetText(string.Empty);
            }
            else
            {
                var def = Services.Assets.WaterProp.Property(inWaterId);
                m_Label.SetText(def.LabelId());
                m_Value.SetText(def.FormatValue(value));
            }
        }
    }
}