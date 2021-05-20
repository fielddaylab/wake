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

        private WaterPropertyBlockF32 m_Block;

        private PropertySlider[] m_CachedSliders;

        private class PropButton : IKeyValuePair<WaterPropertyId, PropButton>
        {
            public WaterPropertyId PropId;
            public PropertySlider Slider;
            public BFWaterProperty EnvProperty;
            public WaterPropertyDesc PropSettings;

            WaterPropertyId IKeyValuePair<WaterPropertyId, PropButton>.Key { get { return PropId; } }

            PropButton IKeyValuePair<WaterPropertyId, PropButton>.Value { get { return this; } }
        }

        private Dictionary<WaterPropertyId, PropButton> buttonDict;

        public Action OnSelectEnd;

        protected override void Awake()
        {
            m_Block = new WaterPropertyBlockF32();
            m_EndButton.onClick.AddListener(() => OnSelectEnd?.Invoke());
            m_CachedSliders = m_SliderGroup.GetComponentsInChildren<PropertySlider>();

            buttonDict = new Dictionary<WaterPropertyId, PropButton>();

            for(int i = 0; i < m_CachedSliders.Length; ++i)
            {
                PropertySlider slider = m_CachedSliders[i];
                slider.Slider.onValueChanged.AddListener((b) => UpdateFromSlider(slider.Id, b));            
            }

            UpdateSliders();

            ResetSliders();


        }

        public override void Refresh()
        {
            base.Refresh();
            m_Block = new WaterPropertyBlockF32();
            UpdateSliders();
            ResetSliders();
            buttonDict?.Clear();
        }

        private void UpdateSliders() {
            var tankType = Services.Tweaks.Get<ExperimentSettings>().GetTank(m_CachedData.Tank);
            if(tankType == null) return;

            var allProperties = Services.Assets.WaterProp.Sorted();

            int sliderIdx = 0;
            foreach(var prop in allProperties) {
                if(sliderIdx >= m_CachedSliders.Length) break;

                var slider = m_CachedSliders[sliderIdx];

                if(!prop.HasFlags(WaterPropertyFlags.IsMeasureable)) {
                    continue;
                }

                slider.Load(prop, prop.Icon(), true);

                PropButton acb = new PropButton();
                acb.PropId = prop.Index();
                acb.Slider = m_CachedSliders[sliderIdx];
                acb.EnvProperty = null;
                acb.PropSettings = prop;
                if(!buttonDict.ContainsKey(acb.PropId)) buttonDict.Add(acb.PropId, acb);

                ++sliderIdx;
            }

            for(; sliderIdx < m_CachedSliders.Length; ++sliderIdx)
            {
                m_CachedSliders[sliderIdx].Load(null, m_EmptyIcon, false);
            }
        }

        private void ResetSliders() {
            if(m_CachedSliders == null || m_CachedSliders.Length < 1) return;
            var facts = Services.Assets.Bestiary.Get(m_CachedData.EcosystemId)?.Facts ?? null;
            if(facts != null) {
                foreach(var fact in facts) {
                    if(fact.GetType().Equals(typeof(BFWaterProperty))) {
                        BFWaterProperty envProp = (BFWaterProperty)fact;
                        buttonDict.TryGetValue(envProp.PropertyId(), out PropButton slider);
                        slider.EnvProperty = envProp;
                    }
                }
            }
            
            foreach(var slider in m_CachedSliders) {
                buttonDict.TryGetValue(slider.Id, out PropButton propSlider);
                if(buttonDict == null || propSlider?.EnvProperty == null) {
                    slider.Slider.SetValueWithoutNotify(0f);
                }
                else {
                    var propDesc = propSlider.PropSettings;
                    float initValue = Rescale(propSlider.EnvProperty.Value(), 0f, 1f, propDesc.MinValue(), propDesc.MaxValue());
                    slider.Slider.SetValueWithoutNotify(initValue);
                }
                
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
            if (def != null)
            {
                var scaledValue = Rescale(value, 0, 1, def.MinValue(), def.MaxValue());
                UpdateBlock(m_Id, scaledValue);
            }

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

        private float Rescale(float value, float min, float max, float minScale, float maxScale) {
            return minScale + (float)((value - min)/((max-min) * (maxScale - minScale)));
        }

        private void UpdateBlock(WaterPropertyId m_Id, float value) {
            switch(m_Id) {
                case WaterPropertyId.CarbonDioxide:
                    m_Block.CarbonDioxide = value;
                    m_CachedData.Values.CarbonDioxide = value;
                    break;
                case WaterPropertyId.Light:
                    m_Block.Light = value;
                    m_CachedData.Values.Light = value;
                    break;
                case WaterPropertyId.Oxygen:
                    m_Block.Oxygen = value;
                    m_CachedData.Values.Oxygen = value;
                    break;
                case WaterPropertyId.PH:
                    m_Block.PH = value;
                    m_CachedData.Values.PH = value;
                    break;
                case WaterPropertyId.Salinity:
                    m_Block.Salinity = value;
                    m_CachedData.Values.Salinity = value;
                    break;
                case WaterPropertyId.Temperature:
                    m_Block.Temperature = value;
                    m_CachedData.Values.Temperature = value;
                    break;
                default:
                    break;
            }
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