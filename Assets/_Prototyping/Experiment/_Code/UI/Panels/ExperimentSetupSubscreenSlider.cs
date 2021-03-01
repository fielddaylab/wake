using System;
using Aqua;
using BeauPools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using System.Collections.Generic;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenSlider : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private Button m_EndButton = null;
        [SerializeField] private LocText m_Label = null;
        [SerializeField] private TMP_Text m_Value = null;
        [SerializeField] private Sprite m_EmptyIcon = null;
        [SerializeField] private Transform m_SliderGroup = null;

        #endregion // Inspector

        private ExperimentSetupData m_CachedData;

        private PropertySlider[] m_CachedSliders;

        public Action OnSelectEnd;

        protected override void Awake()
        {
            m_EndButton.onClick.AddListener(() => OnSelectEnd?.Invoke());
            m_CachedSliders = m_SliderGroup.GetComponentsInChildren<PropertySlider>();
            for(int i = 0; i < m_CachedSliders.Length; ++i)
            {
                PropertySlider slider = m_CachedSliders[i];
                slider.Slider.onValueChanged.AddListener((b) => UpdateFromSlider(slider.Id, b));            
            }

            UpdateSliders();


        }

        public override void Refresh()
        {
            base.Refresh();
            UpdateSliders();
        }

        private void UpdateSliders() {
            var tankType = Services.Tweaks.Get<ExperimentSettings>().GetTank(m_CachedData.Tank);
            if(tankType == null) return;

            var allProperties = Services.Assets.WaterProp.Objects;

            int sliderIdx = 0;
            foreach(var prop in allProperties) {
                if(sliderIdx >= m_CachedSliders.Length) break;

                var slider = m_CachedSliders[sliderIdx];

                if(!prop.HasFlags(WaterPropertyFlags.IsMeasureable)) {
                    continue;
                }

                slider.Load(prop, prop.Icon(), true);

                ++sliderIdx;

            }

            for(; sliderIdx < m_CachedSliders.Length; ++sliderIdx)
            {
                m_CachedSliders[sliderIdx].Load(null, m_EmptyIcon, false);
            }
        }

        private void UpdateFromSlider(WaterPropertyId m_Id, float value) {
            var def = Services.Assets.WaterProp.Property(m_Id);
            if(m_Id != WaterPropertyId.MAX) {
                m_CachedData.SliderValues[(int)m_Id] = value;
            }
            var displayValue = value * def.MaxValue();
            m_Label.SetText(def?.LabelId() ?? null);
            m_Value.SetText(def == null ? "" : def.FormatValue(displayValue));
            var color = def.Color();
            color.a = value;

            Services.Events.Dispatch(ExperimentEvents.OnMeasurementChange, color);
        }

        private void UpdateDisplay(WaterPropertyId inWaterId, float value)
        {
            var def = Services.Assets.WaterProp.Property(inWaterId);
            var displayValue = value * def?.MaxValue() ?? 1;
            m_Label.SetText(def?.LabelId() ?? StringHash32.Null);
            m_Value.SetText(value < 0 ? " " : def.FormatValue(displayValue));
            m_EndButton.interactable = true;
        }

        public override void SetData(ExperimentSetupData inData)
        {
            base.SetData(inData);
            m_CachedData = inData;
        }

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);
            UpdateDisplay(WaterPropertyId.MAX, -1);
        }
    }
}